using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Aurio;
using Aurio.Matching;
using Aurio.Matching.HaitsmaKalker2002;
using Aurio.Project;
using Aurio.Streams;
using Aurio.TaskMonitor;
using NAudio.Wave;
using Nikse.SubtitleEdit.Core;
using System.IO;
using System.ComponentModel;

using System.Runtime.Serialization.Formatters.Binary;

namespace SubSync
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public FingerprintStore store;
        public Profile profile;
        public Subtitle subtitle = new Subtitle();


        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenFile1_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = "*.*";
            dlg.Multiselect = false;
            dlg.Filter = "Video| *.avi; *.mp4; *.mkv|All| *.*";

            if (dlg.ShowDialog() == true)
            {
                VideoToSyncFilePath.Text = dlg.FileName;
            }
        }

        private void OpenFile2_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = "*.*";
            dlg.Multiselect = false;
            dlg.Filter = "Video| *.avi; *.mp4; *.mkv|All| *.*";

            if (dlg.ShowDialog() == true)
            {
                ReferenceFilePath.Text = dlg.FileName;
            }
        }

        private void OpenSubtitle_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".srt";
            dlg.Multiselect = false;
            dlg.Filter = "srt| *.srt; *.ass|All| *.*";

            if (dlg.ShowDialog() == true)
            {
                SubtitlePath.Text = dlg.FileName;

                Subtitle subtitle = new Subtitle();
                Encoding encoding;
                var format = subtitle.LoadSubtitle(dlg.FileName, out encoding, null);
                SubtitleGrid.ItemsSource = subtitle.Paragraphs;
            }
        }

        public TimeSpan TruncateTimespan(TimeSpan Timespan)
        {
            TimeSpan t1 = Timespan;

            int precision = 2; // Specify how many digits past the decimal point
            const int TIMESPAN_SIZE = 7; // it always has seven digits
                                         // convert the digitsToShow into a rounding/truncating mask
            int factor = (int)Math.Pow(10, (TIMESPAN_SIZE - precision));

            TimeSpan truncatedTimeSpan = new TimeSpan(t1.Ticks - (t1.Ticks % factor));
            return truncatedTimeSpan;
        }

        public TimeSpan RoundTimespan(TimeSpan Timespan)
        {
            TimeSpan t1 = Timespan;

            int precision = 2; // Specify how many digits past the decimal point
            const int TIMESPAN_SIZE = 7; // it always has seven digits
                                         // convert the digitsToShow into a rounding/truncating mask
            int factor = (int)Math.Pow(10, (TIMESPAN_SIZE - precision));

            TimeSpan roundedTimeSpan = new TimeSpan(((long)Math.Round((1.0 * t1.Ticks / factor)) * factor));
            return roundedTimeSpan;
        }

        private static readonly TimeSpan GapSize = TimeSpan.FromSeconds(3);

        public static IEnumerable<IEnumerable<TimeSpan>> GetGroups(IEnumerable<TimeSpan> timespans)
        {
            var timespansList = timespans.ToList();
            while (timespansList.Count > 0)
            {
                TimeSpan min = timespansList.Min();
                var closeList = timespansList.Where(x => x - min <= GapSize).ToList();
                yield return closeList;
                foreach (var timeSpan in closeList)
                {
                    timespansList.Remove(timeSpan);
                }
            }
        }

        public class SimpleMatch
        {
            public TimeSpan Offset { get; set;  }
            public AudioTrack Track1 { get; set; }
            public TimeSpan Track1Time { get; set; }
            public AudioTrack Track2 { get; set; }
            public TimeSpan Track2Time { get; set; }
        }

        public class MultipleMatch
        {
            public TimeSpan Starttime { get; set; }
            public int Linenumber { get; set; }
        }

        public void findmatches(string VideoToSyncFileName, string ReferenceFileName, string SubtitlePath) //for testing
        {
            //var VideoToSyncFileName = System.IO.Path.GetFileName(VideoToSyncFilePath.Text);
            //var ReferenceFileName = System.IO.Path.GetFileName(ReferenceFilePath.Text);

            List<Aurio.Matching.Match> matches = store.FindAllMatches();
            List<SimpleMatch> simplematches = new List<SimpleMatch>();
            List<TimeSpan> adjustmentlog = new List<TimeSpan>();
            //sort matches so that Track1.Name is always ReferenceFileName. save to SimpleMatchList
            foreach (var match in matches)
            {
                if(match.Track1.Name != ReferenceFileName)
                {
                    //swap Track1 and Track2
                    simplematches.Add(new SimpleMatch { Offset = match.Offset, Track1 = match.Track2, Track1Time = match.Track2Time, Track2 = match.Track1, Track2Time = match.Track1Time });
                    //simplematches.Add(new SimpleMatch { Offset = match.Offset.Negate(), Track1 = match.Track2, Track1Time = match.Track2Time, Track2 = match.Track1, Track2Time = match.Track1Time });
                }
                else
                {
                    //just copy to simplematches list
                    simplematches.Add(new SimpleMatch { Offset = match.Offset.Negate(), Track1 = match.Track1, Track1Time = match.Track1Time, Track2 = match.Track2, Track2Time = match.Track2Time });
                    //simplematches.Add(new SimpleMatch { Offset = match.Offset, Track1 = match.Track1, Track1Time = match.Track1Time, Track2 = match.Track2, Track2Time = match.Track2Time });
                }
            }

            //Subtitle subtitle = new Subtitle();
            Encoding encoding;
            var format = subtitle.LoadSubtitle(SubtitlePath, out encoding, null);

            List<int> nomatch = new List<int>();
            List<MultipleMatch> multiplematch = new List<MultipleMatch>();

            for (int i = 0; i < subtitle.Paragraphs.Count; i++)
            {
                //find all matches for a subtitleparagraph
                var submatches = simplematches.Where(y => y.Track1Time > subtitle.Paragraphs[i].StartTime.TimeSpan && y.Track1Time < subtitle.Paragraphs[i].EndTime.TimeSpan);
                
                //if matches were found
                if (submatches.Count() > 0)
                {
                    //round timespans of the found submatches
                    foreach (var offset in submatches)
                    {
                        offset.Offset = RoundTimespan(offset.Offset);
                    }
                    //group submatches by offset
                    var groupedsubmatches = GetGroups(submatches.Select(x => x.Offset).ToList());
                    List<TimeSpan> groupedsubmatches_avarage = new List<TimeSpan>();
                    foreach (var group in groupedsubmatches)
                    {
                        groupedsubmatches_avarage.Add(TimeSpan.FromMilliseconds(group.Average(a => a.TotalMilliseconds)));
                    }

                    //adjust subtitle by using the offset that is nearest to the original subtitleparagraph if there is more than one offset
                    if (groupedsubmatches_avarage.Count > 1)
                    {
                        var closestToSubtitleTime = groupedsubmatches_avarage.OrderBy(t => Math.Abs((t - subtitle.Paragraphs[i].StartTime.TimeSpan).Ticks)).First();

                        //save the other(s) offsets to multiplematch-list
                        groupedsubmatches_avarage.Remove(closestToSubtitleTime);
                        foreach (var element in groupedsubmatches_avarage)
                        {
                            multiplematch.Add(new MultipleMatch { Starttime = subtitle.Paragraphs[i].StartTime.TimeSpan + element, Linenumber = i });
                        }
                        subtitle.Paragraphs[i].Adjust(1, closestToSubtitleTime.TotalSeconds);
                        adjustmentlog.Add(closestToSubtitleTime);
                    }
                    else //take first offset otherwise
                    {
                        subtitle.Paragraphs[i].Adjust(1, groupedsubmatches_avarage[0].TotalSeconds);
                        adjustmentlog.Add(groupedsubmatches_avarage[0]);
                    }

                    //adjust time to 0:00:00.000 if adjusting resulted in a negative timestamp
                    if (subtitle.Paragraphs[i].StartTime.TotalSeconds < 0)
                    {
                        var TempDuration = subtitle.Paragraphs[i].Duration.TotalSeconds;
                        subtitle.Paragraphs[i].StartTime.TotalSeconds = 0;
                        subtitle.Paragraphs[i].EndTime.TotalSeconds = TempDuration;
                        //check for negative duration ???
                    }
                }
                else //no match found
                {
                    //add linenumber to nomatch-list
                    nomatch.Add(i);
                    adjustmentlog.Add(TimeSpan.FromSeconds(0));
                }
            }

            //subtitle.Sort(Nikse.SubtitleEdit.Core.Enums.SubtitleSortCriteria.StartTime);

            //TODO: Kopieren von matches, die öfter als einmal gefunden wurden
            /*
            //create new subtitlepargraphs if there are matches in multiplematch that werent used
            foreach (var m in multiplematch)
            {
                //multiplematch contains the linenumber for wich multiple matches exist and the starttimes for the lines
                
                //check if there is no subtitle at the new position
                var addParagraph = new Nikse.SubtitleEdit.Core.Paragraph(subtitle.Paragraphs[m.Linenumber], false);
                var tempspan = addParagraph.Duration.TimeSpan;
                addParagraph.StartTime.TimeSpan = m.Starttime;
                addParagraph.EndTime.TimeSpan = addParagraph.StartTime.TimeSpan + tempspan;

                bool insertsub = true;
                foreach (var sp in subtitle.Paragraphs)
                {
                    //check if there already is a paragraph in the subtitle that has the same timefram as multiplematch paragraph
                    if((addParagraph.StartTime.TotalMilliseconds >= sp.StartTime.TotalMilliseconds && addParagraph.StartTime.TotalMilliseconds < sp.EndTime.TotalMilliseconds) || (addParagraph.EndTime.TotalMilliseconds > sp.StartTime.TotalMilliseconds && addParagraph.EndTime.TotalMilliseconds <= sp.EndTime.TotalMilliseconds) || (addParagraph.StartTime.TotalMilliseconds < sp.StartTime.TotalMilliseconds && addParagraph.EndTime.TotalMilliseconds > sp.EndTime.TotalMilliseconds))
                    {
                        //DONT copy multiplematch paragraph into existing subtitle
                        //There already is a subtitlepagraph at this position
                        insertsub = false;
                    }
                }
                if(insertsub == true)
                {
                    //copy multiplematch paragraph into original subtitle
                    subtitle.InsertParagraphInCorrectTimeOrder(addParagraph);

                    //increase linenumber of nomatch list since a new line was added
                    //if the new line was added before the linenumber of nomatch, increase nomatchlinenumber
                    for (int i = 0; i < nomatch.Count; i++)
                    {
                        int nomatchlinenumber = nomatch[i];
                        if (nomatchlinenumber > m.Linenumber)
                        {
                            nomatch[i]++;
                        }
                    }
                }
            }
            */
            
            //remove subtitle paragraphs for which no match was found
            Subtitle nomatchsubtitle = new Subtitle();
            if (nomatch.Count > 0)
            {
                for (int i = nomatch.Count - 1; i >= 0; i--)
                {
                    //save the deleted lines to a seperate subtitle file
                    nomatchsubtitle.InsertParagraphInCorrectTimeOrder(subtitle.Paragraphs[nomatch[i]]);
                    //remove the line from the original subtitle
                    subtitle.Paragraphs.RemoveAt(nomatch[i]);
                }
                //save the nomatches subs to a file
                string allText2 = nomatchsubtitle.ToText(format);
                TextWriter file2 = new StreamWriter(SubtitlePath.Insert(SubtitlePath.Length - 4, "_no_match"), false, encoding);
                file2.Write(allText2);
                file2.Close();
            }


            //save the corrected subs
            subtitle.Renumber(1);
            string allText = subtitle.ToText(format);
            TextWriter file = new StreamWriter(SubtitlePath.Insert(SubtitlePath.Length - 4, "_sync"), false, encoding);
            file.Write(allText);
            file.Close();

            //save adjustmentlog to log
            string outdir = System.IO.Path.GetDirectoryName(SubtitlePath);
            TextWriter fileal = new StreamWriter(outdir + "\\" + "adjustment.log");
            foreach (TimeSpan ts in adjustmentlog)
                fileal.WriteLine(ts.ToString());
            fileal.Close();
        }

        private void Synchronize_Click(object sender, RoutedEventArgs e)
        {
            if (VideoToSyncFilePath.Text.Length != 0 && ReferenceFilePath.Text.Length != 0 && SubtitlePath.Text.Length != 0)
            {
                //dissable button while processing
                Synchronize.IsEnabled = false;

                string[] FileNames = new string[] { VideoToSyncFilePath.Text, ReferenceFilePath.Text };

                profile = FingerprintGenerator.GetProfiles()[0];
                store = new FingerprintStore(profile);
                //store.FingerprintSize = ;
                store.Threshold = 0.45f;

                var VideoToSyncFileName = System.IO.Path.GetFileName(VideoToSyncFilePath.Text);
                var ReferenceFileName = System.IO.Path.GetFileName(ReferenceFilePath.Text);
                var SubtitlePathname = SubtitlePath.Text;
                bool firsttaskfinished = false;
                Task.Factory.StartNew(() => Parallel.ForEach<string>(FileNames, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, fileName =>
                {
                    AudioTrack audioTrack = new AudioTrack(new FileInfo(fileName));
                    IProgressReporter progressReporter = ProgressMonitor.GlobalInstance.BeginTask("Generating sub-fingerprints for " + audioTrack.FileInfo.Name, true);
                    FingerprintGenerator fpg = new FingerprintGenerator(profile, audioTrack);
                    int subFingerprintsCalculated = 0;
                    fpg.SubFingerprintsGenerated += new EventHandler<SubFingerprintsGeneratedEventArgs>(delegate (object s2, SubFingerprintsGeneratedEventArgs e2)
                    {
                        subFingerprintsCalculated++;
                        progressReporter.ReportProgress((double)e2.Index / e2.Indices * 100);
                        store.Add(e2);
                    });

                    fpg.Generate();

                    //use second task to find matches if first task is finished
                    if (firsttaskfinished)
                    {
                        findmatches(VideoToSyncFileName, ReferenceFileName, SubtitlePathname);
                    }
                    progressReporter.Finish();
                    firsttaskfinished = true;

                }))
                .ContinueWith(task =>
                { }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                MessageBox.Show("Choose video and subtitle files first.", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ProgressMonitor.GlobalInstance.ProcessingProgressChanged += GlobalInstance_ProcessingProgressChanged;
            ProgressMonitor.GlobalInstance.ProcessingFinished += GlobalInstance_ProcessingFinished;
        }

        void GlobalInstance_ProcessingProgressChanged(object sender, ValueEventArgs<float> e)
        {
            progressBar1.Dispatcher.BeginInvoke((Action)delegate
            {
                progressBar1.Value = e.Value;
                if(progressBar1.Value == 100)
                {
                    progressBar1.IsIndeterminate = true;
                }
            });
        }

        void GlobalInstance_ProcessingFinished(object sender, EventArgs e)
        {
            progressBar1.Dispatcher.BeginInvoke((Action)delegate
            {
                //reload the subtitle for displaying
                SubtitleGrid.ItemsSource = subtitle.Paragraphs;
                //enable button after processing
                Synchronize.IsEnabled = true;
                progressBar1.Value = 0;
                progressBar1.IsIndeterminate = false;
                //delete wav file
                if (File.Exists(VideoToSyncFilePath.Text + ".ffproxy.wav"))
                {
                    File.Delete(VideoToSyncFilePath.Text + ".ffproxy.wav");
                }
                if (File.Exists(ReferenceFilePath.Text + ".ffproxy.wav"))
                {
                    File.Delete(ReferenceFilePath.Text + ".ffproxy.wav");
                }
                MessageBox.Show("Synchronizing done!", "", MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }
    }
}
