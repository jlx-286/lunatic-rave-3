### known problem(s)
- "sws_scale" crashes (in Windows) when (the video in) "[トルコ行進曲 (Hd-NRG mix)](https://manbow.nothing.sh/event/event.cgi?action=More_def&num=47&event=78)" is stopped
- the second video in "[Grape.exe!](https://anonymous.nekokan.dyndns.info/data/BOFoonXV/grape_exe.zip)" is not shown sometimes
### Unity Editor Version(s)
+ considering 2019.3 or newer version(s)
    - Windows: Fatal error in GC "GetThreadContext failed" (due to Unity with System.Threading?)
+ considering 2020.2.0 or newer version(s)
    - <https://docs.unity3d.com/2020.2/Documentation/ScriptReference/Time.html>
	- [What happens when Time.time gets very large in Unity?](https://gamedev.stackexchange.com/questions/141807/what-happens-when-time-time-gets-very-large-in-unity)
+ considering the version(s) using .NET 7 (C# 11) or later
    - <https://learn.microsoft.com/dotnet/core/whats-new/dotnet-7#regular-expressions>
    - <https://docs.unity3d.com/2023.3/Documentation/Manual/CSharpCompiler.html>

### Dependencies
- Windows (<https://learn.microsoft.com/windows-server/administration/windows-commands/path>)
    + FFmpeg (avcodec-58 + avformat-58 + avutil-56 + swresample-3 + swscale-5)
        * [download FFmpeg](https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2023-04-30-12-46/ffmpeg-n4.4.4-win64-lgpl-shared-4.4.zip) and add the directory of "avcodec-58.dll" to "%path%"
- Linux (Debian or Ubuntu)
    + FluidSynth
        ```shell
            sudo apt install libfluidsynth-dev
            # apt download timgm6mb-soundfont fluid-soundfont-gm
            # sudo apt install libfluidsynth2 || sudo apt install libfluidsynth3
            ## You can install the dependencies by Synaptic (in GUI):
            # sudo apt install synaptic
            ## considering plugins from snap store
            # sudo apt install patchelf || sudo snap install patchelf
            # snap download minuet
        ```
- [FluidSynth](https://www.fluidsynth.org)
    + The plugins are from [Unity Asset Store](https://assetstore.unity.com/packages/tools/audio/fluid-midi-player-173680).
        (alternative:<https://github.com/FluidSynth/fluidsynth/releases/>)
    + The soundfont in "[Application.streamingAssetsPath](https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Application-streamingAssetsPath.html)" is from timgm6mb-soundfont.
    + <https://github.com/FluidSynth/fluidsynth/wiki/Download#distributions>

### About .gitignore
1. previous .gitignore (by owy787)
    + same as [C.gitignore](https://github.com/github/gitignore/blob/main/C.gitignore)
2. current .gitignore (by jlx-286)
    + from [Unity.gitignore](https://github.com/github/gitignore/blob/main/Unity.gitignore) and <https://www.it1352.com/1848876.html>
    ``` dockerfile
        /.out/
        /UWP/
    ```
