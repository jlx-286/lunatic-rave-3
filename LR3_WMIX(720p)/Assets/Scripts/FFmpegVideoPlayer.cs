using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
public unsafe static class FFmpegVideoPlayer{
    public enum VideoState : byte{
        stopped = 0,
        [Obsolete("not enabled", true)]
        paused = 1,
        playing = 2,
    };
    [StructLayout(LayoutKind.Explicit)] public struct VideoSize{
        [FieldOffset(0)] public int width;
        [FieldOffset(sizeof(int))] public int height;
    }
    public static double speed = 1;
    public static readonly Thread[] threads = Enumerable.Repeat<Thread>(null, 4).ToArray();
    public static readonly Texture2D[] textures = Enumerable.Repeat<Texture2D>(null, 4).ToArray();
    public static readonly byte*[] addrs = new byte*[4];
    public static readonly bool[] playing = Enumerable.Repeat(false, 4).ToArray();
    public static readonly bool[] toStop = Enumerable.Repeat(false, 4).ToArray();
    private const ushort ZZ = 36*36;
    public static readonly string[] paths = Enumerable.Repeat<string>(null, ZZ).ToArray();
    public static readonly VideoSize[] media_sizes = Enumerable.Repeat(
        new VideoSize(){width = 0, height = 0}, ZZ).ToArray();
    private static readonly int[] offsets = Enumerable.Repeat(0, ZZ).ToArray();
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || PLATFORM_STANDALONE_WIN
    private const byte pixelBytes = 4;
    private const TextureFormat textureFormat = TextureFormat.BGRA32;
#else
    private const byte pixelBytes = 3;
    private const TextureFormat textureFormat = TextureFormat.RGB24;
#endif
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
    private delegate void InitFunc();
    private delegate void CleanUpFunc();
    private delegate bool GetVideoSizeFunc(string path,
        ushort num, out int width, out int height);
    public delegate void SetSpeedFunc(double speed = 1);
    public delegate void SetVideoStateFunc(
        byte layer, VideoState _ = VideoState.playing);
    private delegate void PlayVideoFunc(string path,
        byte layer, ushort num, byte* pixels);
    private static InitFunc Init = null;
    private static CleanUpFunc CleanUp = null;
    private static GetVideoSizeFunc GetVideoSize = null;
    public static SetSpeedFunc SetSpeed = null;
    public static SetVideoStateFunc SetVideoState = null;
    private static PlayVideoFunc PlayVideo = null;
    private static class V4{
        private const string PluginName = "FFmpegPlayer";
        [DllImport(PluginName)] public extern static void Init();
        [DllImport(PluginName)] public extern static void CleanUp();
        [DllImport(PluginName)] public extern static bool GetVideoSize(
            string path, ushort num, out int width, out int height);
        [DllImport(PluginName)] public extern static void SetSpeed(
            double speed = 1);
        [DllImport(PluginName)] public extern static void SetVideoState(
            byte layer, VideoState _ = VideoState.playing);
        [DllImport(PluginName)] public extern static void PlayVideo(
            string path, byte layer, ushort num, byte* pixels);
    }
    private static class V5{
        private const string PluginName = "FFmpeg5Player";
        [DllImport(PluginName)] public extern static void Init();
        [DllImport(PluginName)] public extern static void CleanUp();
        [DllImport(PluginName)] public extern static bool GetVideoSize(
            string path, ushort num, out int width, out int height);
        [DllImport(PluginName)] public extern static void SetSpeed(
            double speed = 1);
        [DllImport(PluginName)] public extern static void SetVideoState(
            byte layer, VideoState _ = VideoState.playing);
        [DllImport(PluginName)] public extern static void PlayVideo(
            string path, byte layer, ushort num, byte* pixels);
    }
    private static class V6{
        private const string PluginName = "FFmpeg6Player";
        [DllImport(PluginName)] public extern static void Init();
        [DllImport(PluginName)] public extern static void CleanUp();
        [DllImport(PluginName)] public extern static bool GetVideoSize(
            string path, ushort num, out int width, out int height);
        [DllImport(PluginName)] public extern static void SetSpeed(
            double speed = 1);
        [DllImport(PluginName)] public extern static void SetVideoState(
            byte layer, VideoState _ = VideoState.playing);
        [DllImport(PluginName)] public extern static void PlayVideo(
            string path, byte layer, ushort num, byte* pixels);
    }
#else
    private const string PluginName = "FFmpegPlayer";
    [DllImport(PluginName)] private extern static void Init();
    [DllImport(PluginName)] private extern static void CleanUp();
    [DllImport(PluginName)] private extern static bool GetVideoSize(
        string path, ushort num, out int width, out int height);
    [DllImport(PluginName)] public extern static void SetSpeed(
        double speed = 1);
    [DllImport(PluginName)] private extern static void SetVideoState(
        byte layer, VideoState _ = VideoState.playing);
    [DllImport(PluginName)] private extern static void PlayVideo(
        string path, byte layer, ushort num, byte* pixels);
#endif
    static FFmpegVideoPlayer(){
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        const string PluginDir = "/lib/x86_64-linux-gnu/";
        if(File.Exists(PluginDir + "libavcodec.so.58")){//FFmpeg 4.x
            Init          = V4.Init;
            CleanUp       = V4.CleanUp;
            GetVideoSize  = V4.GetVideoSize;
            SetSpeed      = V4.SetSpeed;
            SetVideoState = V4.SetVideoState;
            PlayVideo     = V4.PlayVideo;
        }else if(File.Exists(PluginDir + "libavcodec.so.59")){//FFmpeg 5.x
            Init          = V5.Init;
            CleanUp       = V5.CleanUp;
            GetVideoSize  = V5.GetVideoSize;
            SetSpeed      = V5.SetSpeed;
            SetVideoState = V5.SetVideoState;
            PlayVideo     = V5.PlayVideo;
        }else if(File.Exists(PluginDir + "libavcodec.so.60")){//FFmpeg 6.x
            Init          = V6.Init;
            CleanUp       = V6.CleanUp;
            GetVideoSize  = V6.GetVideoSize;
            SetSpeed      = V6.SetSpeed;
            SetVideoState = V6.SetVideoState;
            PlayVideo     = V6.PlayVideo;
        }else throw new DllNotFoundException("unknown libavcodec version");
#endif
        Init();
    }
    public static void PlayerStop(byte layer){
        if(layer >= threads.Length) return;
        if(threads[layer] != null){
            SetVideoState(layer, VideoState.stopped);
            while(threads[layer].IsAlive);
            threads[layer] = null;
            textures[layer] = null;
            addrs[layer] = null;
        }
    }
#if UNITY_EDITOR
    [Obsolete("not enabled", false)]
    public static void PlayerSetPause(byte layer, UnityEditor.PauseState _){
        // if(layer >= threads.Length || threads[layer] == null) return;
        // switch(_){
        //     case UnityEditor.PauseState.Paused:
        //         SetVideoState(layer, VideoState.paused);
        //         break;
        //     case UnityEditor.PauseState.Unpaused:
        //         SetVideoState(layer, VideoState.playing);
        //         break;
        // }
    }
#endif
    [Obsolete("not enabled", false)]
    public static void PlayerSetPause(byte layer, bool pause){
        // if(layer >= threads.Length || threads[layer] == null) return;
        // if(pause) SetVideoState(layer, VideoState.paused);
        // else SetVideoState(layer, VideoState.playing);
    }
    private static void VideoFree(ushort num){
        if(num >= ZZ) return;
        paths[num] = null;
        media_sizes[num] = new VideoSize(){width = 0, height = 0};
        offsets[num] = 0;
    }
    private static void NewVideoTex(byte layer, ushort num){
        if(layer >= threads.Length || num >= ZZ ||
            media_sizes[num].width < 1 || media_sizes[num].height < 1) return;
        NativeArray<byte> arr;
        if(media_sizes[num].width <= media_sizes[num].height)
            textures[layer] = new Texture2D(media_sizes[num].width,
                media_sizes[num].height, textureFormat, false){
                filterMode = FilterMode.Point };
        else// if(media_sizes[num].width > media_sizes[num].height)
            textures[layer] = new Texture2D(media_sizes[num].width,
                media_sizes[num].width, textureFormat, false){
                filterMode = FilterMode.Point };
        arr = textures[layer].GetRawTextureData<byte>();
        addrs[layer] = (byte*)arr.GetUnsafePtr();// GetUnsafeReadOnlyPtr());
        StaticClass.memset(addrs[layer], 0, (IntPtr)arr.Length);
    }
    public static void PlayerPlay(byte layer, ushort num){
        if(layer >= threads.Length) return;
        if(num >= ZZ || media_sizes[num].width < 1) return;
        NewVideoTex(layer, num);
        if(addrs[layer] == null) return;
        threads[layer] = new Thread(()=>{
            SetVideoState(layer, VideoState.playing);
            playing[layer] = true;
            PlayVideo(paths[num], layer, num, addrs[layer] + offsets[num]);
            // playing[layer] = false;
            toStop[layer] = true;
        }){IsBackground = true};
        threads[layer].Start();
    }
    public static bool VideoNew(string path, ushort num){
        if(!GetVideoSize(path, num, out media_sizes[num].width, out media_sizes[num].height)){
            VideoFree(num);
            return false;
        }
        paths[num] = path;
        if(media_sizes[num].width <= media_sizes[num].height)
            offsets[num] = 0;
        else// if(media_sizes[num].width > media_sizes[num].height)
            offsets[num] = (media_sizes[num].width - media_sizes[num].height) / 2 * media_sizes[num].width * pixelBytes;
        return true;
    }
    public static void Release(){
        for(byte i = 0; i < threads.Length; i++)
            PlayerStop(i);
        for(ushort i = 0; i < ZZ; i++)
            VideoFree(i);
        CleanUp();
        fixed(void* p = playing)
            StaticClass.memset(p, 0, (IntPtr)(playing.Length * sizeof(bool)));
        fixed(void* p = toStop)
            StaticClass.memset(p, 0, (IntPtr)(toStop.Length * sizeof(bool)));
    }
    public static void ClearPixels(byte layer, ushort num){
        StaticClass.memset(addrs[layer] + offsets[num], 0, (IntPtr)((long)pixelBytes
            * media_sizes[num].width * media_sizes[num].height));
    }
}