### Unity Editor Version
+ considering 2019.3 or newer version(s) / known problem(s)
    - Windows: Fatal error in GC "GetThreadContext failed" (due to Unity with System.Threading?)
+ considering 2020.2.0 or newer version(s)
    - <https://docs.unity3d.com/2020.2/Documentation/ScriptReference/Time.html>
	- <a href="https://gamedev.stackexchange.com/questions/141807/what-happens-when-time-time-gets-very-large-in-unity">What happens when Time.time gets very large in Unity?</a>

### Dependencies:
- Windows (<https://learn.microsoft.com/windows-server/administration/windows-commands/path>)
    - <a href="https://www.videolan.org/vlc/">VLC</a>
        + install VLC and add the directory of "libvlc.dll" to "%path%"
    - FFmpeg (avcodec-58 + avformat-58 + avutil-56 + swresample-3 + swscale-5)
        + <a href="https://github.com/BtbN/FFmpeg-Builds/releases/download/autobuild-2022-10-31-12-44/ffmpeg-n4.4.3-win64-lgpl-shared-4.4.zip">download FFmpeg</a> and add the directory of "avcodec-58.dll" to "%path%"
- Linux (Debian or Ubuntu)
    1. VLC
        ``` shell
        # sudo apt install vlc-plugin-base vlc-plugin-video-output libvlc5
        sudo apt install vlc-plugin-base vlc-plugin-video-output libvlc-dev
        # sudo apt install vlc libvlc-dev
        ```
    2. FFmpeg (libavcodec + libavutil + libavformat + libswresample + libswscale)
        ```shell
        sudo apt install libavformat-dev libswscale-dev
        ```
    3. FluidSynth
        ```shell
        sudo apt install libfluidsynth-dev
        # sudo apt install libfluidsynth2
        # apt download timgm6mb-soundfont fluid-soundfont-gm
        ```
    4. OpenGL ES 2
        ```shell
        sudo apt install libgles-dev # libgles2
        ```
    5. Others
        ```shell
        ## You can install the dependencies by Synaptic (in GUI):
        # sudo apt install synaptic
        ## considering plugins from snap store
        # sudo apt install patchelf || sudo snap install patchelf
        # snap download vlc ffmpeg minuet
        ```
- FluidSynth
    + The plugins are from <a href="https://assetstore.unity.com/packages/tools/audio/fluid-midi-player-173680">Unity Asset Store</a>.
        (alternative:<https://github.com/FluidSynth/fluidsynth/releases/>)
    + The soundfont in "<a href="https://docs.unity3d.com/2018.2/Documentation/ScriptReference/Application-streamingAssetsPath.html">Application.streamingAssetsPath</a>" is from timgm6mb-soundfont.
    + <https://github.com/FluidSynth/fluidsynth/wiki/Download#distributions>

### About .gitignore
1. previous .gitignore (by owy787)
    + same as <a href="https://github.com/github/gitignore/blob/main/C.gitignore">C.gitignore</a>
2. current .gitignore (by jlx-286)
    + from <a href="https://github.com/github/gitignore/blob/main/Unity.gitignore">Unity.gitignore</a>
    + from <https://www.it1352.com/1848876.html>
    ``` dockerfile
        /.out/
        /UWP/
    ```
