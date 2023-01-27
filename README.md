# CueFileGen

Utility to generate `.cue` (https://en.wikipedia.org/wiki/Cue_sheet_(computing)) files from metadata available on files.

Uses `ffprobe` (https://ffmpeg.org/ffprobe.html) to get the relevant metadata.

## Usage
> __:warning: This will override any existing `.cue` file__
```
CueFileGen <File_Path>
```

## Impl details
This utility expects data in the following format, if `ffprobe` format changes please make an issue to update this utility.

```
[CHAPTER]
id=0
time_base=1/44100 
start=0
start_time=0:00:00.000000
end=632790
end_time=0:00:14.348980
TAG:title=Opening Credits
[/CHAPTER]
```