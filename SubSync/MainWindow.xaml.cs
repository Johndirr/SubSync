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

namespace SubSync
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FingerprintStore store;
        private Profile profile;
        private string ffmpegpath = System.AppDomain.CurrentDomain.BaseDirectory + "Tools\\ffmpeg.exe";
        private string ffprobepath = System.AppDomain.CurrentDomain.BaseDirectory + "Tools\\ffprobe.exe";

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
                File1.Text = extractAudio(dlg.FileName);
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
                File2.Text = extractAudio(dlg.FileName);
            }
        }

        private void OpenSubtitle_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".srt";
            dlg.Multiselect = false;
            dlg.Filter = "srt|*.srt|ass|*.ass";

            if (dlg.ShowDialog() == true)
            {
                subtitlepath.Text = dlg.FileName;

                Subtitle subtitle = new Subtitle();
                Encoding encoding;
                var format = subtitle.LoadSubtitle(dlg.FileName, out encoding, null);
                SubtitleGrid.ItemsSource = subtitle.Paragraphs;
            }
        }

        public string getAudioExtension(string filepath)
        {
            string output;
            string ffprobecommand;

            ffprobecommand = "-v error -select_streams a:0 -show_entries stream=codec_name -of default=nokey=1:noprint_wrappers=1" + " \"" + filepath + "\"";

            ProcessStartInfo processffmpegStartInfo = new ProcessStartInfo(ffprobepath, ffprobecommand);
            processffmpegStartInfo.RedirectStandardOutput = true;
            processffmpegStartInfo.RedirectStandardError = true;
            processffmpegStartInfo.RedirectStandardInput = true;
            processffmpegStartInfo.UseShellExecute = false;
            processffmpegStartInfo.CreateNoWindow = true;

            Process process = Process.Start(processffmpegStartInfo);
            using (StreamReader streamReader = process.StandardOutput)
            {
                output = streamReader.ReadToEnd();
            }
            return output = Regex.Replace(output, @"\t|\n|\r", ""); ;
        }

        public string extractAudio(string filepath)
        {
            string ffmpegcommand;
            string AudioExtension = getAudioExtension(filepath);

            if (AudioExtension == "mp3")
            {
                //just copy the audio
                ffmpegcommand = "-i " + " \"" + filepath + "\"" + " -vn -acodec copy " + "\"" + filepath.Substring(0, filepath.Length - 3) + AudioExtension + "\"";
            }
            else
            {
                //reencode to mp3
                AudioExtension = "mp3";
                ffmpegcommand = "-i " + " \"" + filepath + "\"" + " -vn " + "\"" + filepath.Substring(0, filepath.Length - 3) + AudioExtension + "\"";
            }

            /*
            if (AudioExtension != "aac")
            {
                ffmpegcommand = "-i " + " \"" + filepath + "\"" + " -vn -acodec copy " + "\"" + filepath.Substring(0, filepath.Length - 3) + AudioExtension + "\"";
            }
            else
            {
                //reencode to mp3
                AudioExtension = "mp3";
                ffmpegcommand = "-i " + " \"" + filepath + "\"" + " -vn " + "\"" + filepath.Substring(0, filepath.Length - 3) + AudioExtension + "\"";
            }
            */
            Process AudioExtraction = Process.Start(ffmpegpath, ffmpegcommand);
            AudioExtraction.WaitForExit();
            return filepath.Substring(0, filepath.Length - 3) + AudioExtension;
        }

        public void findmatches()
        {
            int matchcount = store.FindAllMatches().Count;
            if (matchcount > 0)
            {

                //MessageBox.Show(matchcount.ToString() + " matches found.", "Fingerprinting and Matching", MessageBoxButton.OK, MessageBoxImage.Information);

                List<Aurio.Matching.Match> matches = store.FindAllMatches();

                //--
                //TimeSpan lastoffset = new TimeSpan();
                List<String> listoff = new List<String>();
                List<int> listnooff = new List<int>();
                Subtitle subtitle = new Subtitle();
                Subtitle subtitleNoMatches = new Subtitle();
                Encoding encoding;
                var format = subtitle.LoadSubtitle(subtitlepath.Text, out encoding, null);
                var Filename2 = System.IO.Path.GetFileName(File2.Text);
                //look for a match for each subtitle line and add the offset to a list
                for (int i = 0; i < subtitle.Paragraphs.Count; i++)
                {
                    //fill a list for every line/offset and insert an offset later on if a match is found
                    listoff.Add("Line " + i.ToString() + " No offset");
                    for (int y = 0; y < matches.Count; y++)
                    {
                        if ((matches[y].Track1.Name == Filename2 && matches[y].Track1Time > subtitle.Paragraphs[i].StartTime.TimeSpan && matches[y].Track1Time < subtitle.Paragraphs[i].EndTime.TimeSpan) ||
                           (matches[y].Track2.Name == Filename2 && matches[y].Track2Time > subtitle.Paragraphs[i].StartTime.TimeSpan && matches[y].Track2Time < subtitle.Paragraphs[i].EndTime.TimeSpan))
                        {
                            //(make the offset negative if the files in the matches are in another order than choosen under File1 and File2)
                            if (Filename2.Contains(matches[y].Track1.Name))
                            {
                                listoff[i] = "Line " + i.ToString() + " Offset: " + matches[y].Offset.Negate().ToString();
                                //shift subtitleline by offset
                                subtitle.Paragraphs[i].Adjust(1, matches[y].Offset.Negate().TotalSeconds);
                            }
                            else
                            {
                                listoff[i] = "Line " + i.ToString() + " Offset: " + matches[y].Offset.ToString();
                                //shift subtitleline by offset
                                subtitle.Paragraphs[i].Adjust(1, matches[y].Offset.TotalSeconds);
                            }
                            //adjust time to 0:00:00.000 if adjusting resulted in a negative timestamp
                            if (subtitle.Paragraphs[i].StartTime.TotalSeconds < 0)
                            {
                                subtitle.Paragraphs[i].StartTime.TotalSeconds = 0;
                                subtitle.Paragraphs[i].EndTime = subtitle.Paragraphs[i].Duration; //only the time from 0 to the endtime will is counted as the duration
                            }
                            break;
                        }
                    }
                    //make a list with the lines for which no match was found 
                    if (listoff[i] == ("Line " + i.ToString() + " No offset"))
                    {
                        listnooff.Add(i);
                    }
                }

                if (listnooff.Count == 0)
                {
                    MessageBox.Show(listoff.Count.ToString() + " lines were adjusted.\n" + listnooff.Count.ToString() + " lines had no matching audiofingerprint.", "Fingerprinting and Matching", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    if (MessageBox.Show(listoff.Count.ToString() + " lines were adjusted.\n" + listnooff.Count.ToString() + " lines had no matching audiofingerprint.\n\nDo you want to delete the missing lines?", "Delete missing lines?", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    {
                        //no...
                        //delete all lines with no match from the listoff-list only
                        for (int i = listnooff.Count - 1; i >= 0; i--)
                        {
                            subtitleNoMatches.InsertParagraphInCorrectTimeOrder(subtitle.Paragraphs[listnooff[i]]);
                            listoff.RemoveAt(listnooff[i]);
                        }
                    }
                    else
                    {
                        //yes...
                        //delete all lines with no match from the subtitle and from the listoff-list
                        for (int i = listnooff.Count - 1; i >= 0; i--)
                        {
                            //save the deleted lines to a seperate subtitle file
                            subtitleNoMatches.InsertParagraphInCorrectTimeOrder(subtitle.Paragraphs[listnooff[i]]);
                            subtitle.Paragraphs.RemoveAt(listnooff[i]);
                            listoff.RemoveAt(listnooff[i]);
                        }
                    }
                    //save the nomatches subs
                    string allText2 = subtitleNoMatches.ToText(format);
                    TextWriter file2 = new StreamWriter(subtitlepath.Text.Insert(subtitlepath.Text.Length - 4, "_no_match"), false, encoding);
                    file2.Write(allText2);
                    file2.Close();
                }

                //save the corrected subs
                subtitle.Renumber(1);
                string allText = subtitle.ToText(format);
                TextWriter file = new StreamWriter(subtitlepath.Text.Insert(subtitlepath.Text.Length - 4, "_sync"), false, encoding);
                file.Write(allText);
                file.Close();

                //reload the subtitle for displaying
                SubtitleGrid.ItemsSource = subtitle.Paragraphs;  
            }
            else
            {
                MessageBox.Show("No matches found.", "Fingerprinting and Matching", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Synchronize_Click(object sender, RoutedEventArgs e)
        {
            if (File1.Text.Length != 0 && File2.Text.Length != 0 && subtitlepath.Text.Length != 0)
            {
                //dissable button while processing
                Synchronize.IsEnabled = false;

                string[] FileNames = new string[] { File1.Text, File2.Text };

                profile = FingerprintGenerator.GetProfiles()[0];
                store = new FingerprintStore(profile);
                //store.FingerprintSize = ;
                store.Threshold = 0.45f;

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
                    progressReporter.Finish();
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
            });
        }

        void GlobalInstance_ProcessingFinished(object sender, EventArgs e)
        {
            progressBar1.Dispatcher.BeginInvoke((Action)delegate
            {
                progressBar1.Value = 0;
                //enable button after processing
                Synchronize.IsEnabled = true;
                //find matches
                findmatches();
            });
        }
    }
}
