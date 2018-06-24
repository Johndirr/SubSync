# SubSync
SubSync can automatically resync subtitles to a video-file by using the audio-track of a reference video and the corresponding subtitle-file.

# Usage
1. Select the video you want to resync your subtitles to by pressing 'Open video...'
2. Select the reference video by pressing 'Open reference video...'
3. Select the reference subtitle by pressing 'Open reference subtitle...'
4. Press 'Synchronize' to start the fingerprinting and matching process.
5. After snychronizing the resulting subtitles will be saved into the same folder as the reference subtitle-file with the text '_sync' added to the name. E.g. 'The_Godfather_sync.srt'

IMPORTANT: Of course the reference audio and reference subtitles must be in sync to each other.

# Dependencies / References
SubSync uses the following programs and libraries:
- https://github.com/protyposis/Aurio for audio fingerprinting and matching
- https://github.com/SubtitleEdit/subtitleedit libse for loading and editing subtitles

# Known bugs / To Do
- Copy lines for which more than one match was found even though the other matches aren't in the original video
- Start processing the subtitles in an extra process
