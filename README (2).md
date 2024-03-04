#### considering .NET 7 (C# 11) or newer version(s)
- <https://learn.microsoft.com/dotnet/core/whats-new/dotnet-7#regular-expressions>
- <https://docs.unity3d.com/2023.3/Documentation/Manual/CSharpCompiler.html>
- <https://docs.godotengine.org/en/4.2/about/list_of_features.html#scripting>

### Dependencies
- Windows (<https://learn.microsoft.com/windows-server/administration/windows-commands/path>)
    + FFmpeg (avcodec-58 + avformat-58 + avutil-56 + swresample-3 + swscale-5)
        1. [download FFmpeg](https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2023-04-30-12-46/ffmpeg-n4.4.4-win64-lgpl-shared-4.4.zip)
        2. add (the absolute path of) the directory "bin" to "%path%"
        3. reboot now or later
- Linux (Debian or Ubuntu)
    + FluidSynth
    ```shell
        sudo apt install libfluidsynth2 || sudo apt install libfluidsynth3
        ## You can install the dependencies by Synaptic (in GUI):
        # sudo apt install synaptic
        ## soundfonts
        # apt download timgm6mb-soundfont fluid-soundfont-gm
        # snap download minuet
    ```
- [FluidSynth](https://www.fluidsynth.org)
    + The plugins are from [Unity Asset Store](https://assetstore.unity.com/packages/tools/audio/fluid-midi-player-173680).
    + alternative plugin(s):<https://github.com/FluidSynth/fluidsynth/releases/>
    + The soundfont in "[Application.streamingAssetsPath](https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Application-streamingAssetsPath.html)" can be from <https://packages.debian.org/trixie/timgm6mb-soundfont>.
    + <https://github.com/FluidSynth/fluidsynth/wiki/Download#distributions>

### About .gitignore
1. previous .gitignore (by owy787)
    + same as [C.gitignore](https://github.com/github/gitignore/blob/main/C.gitignore)
2. current ".gitignore"s (by jlx-286)
    + from [Unity.gitignore](https://github.com/github/gitignore/blob/main/Unity.gitignore) and <https://www.it1352.com/1848876.html>
    ``` dockerfile
        /.out/
        /UWP/
    ```
    + from [Godot.gitignore](https://github.com/github/gitignore/blob/main/Godot.gitignore)