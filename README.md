# SubSync

SubSync can automatically resync subtitles to a video-file by using a reference video and the corresponding subtitle-file.

# Usage
To run SubSync the following files must be present in the 'Tools' directory:
- ffmpeg.exe
- ffprobe.exe

Both files are components of the FFmpeg multimedia framework. Download FFmpeg for windows at: https://ffmpeg.zeranoe.com/builds/

1. Select the video you want to resync your subtitles to by pressing 'Open video...' (A command line window will pop up - this is ffmpeg converting or copying the audio track from the video file. If an audio track already exists you will be asked to overwrite it (enter 'y' or 'n' and press enter)
2. Select the reference video by pressing 'Open reference video...' (A command line window will pop up - this is ffmpeg converting or copying the audio track from the video file. If an audio track already exists you will be asked to overwrite it (enter 'y' or 'n' and press enter)
3. Select the reference subtitle by pressing 'Open reference subtitle...'
4. Press Synchronize to start the fingerprinting and matching process.
5. After snychronizing the resulting subtitles will be saved into the same folder as the reference subtitle-file with the text '_sync' added to the name. E.g. 'The_Godfather_sync.srt'

IMPORTANT: Of course the reference audio and reference subtitles must be in sync to each other.


# Dependencies / References

SubSync uses the following programs and libraries:
- https://github.com/protyposis/Aurio for audio fingerprinting and matching
- https://github.com/SubtitleEdit/subtitleedit libse for loading and editing subtitles


