using System;
using System.Linq;
using System.Runtime.InteropServices;
// #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
// #endif
public unsafe static class VLCPlayer{
    private enum State{
        [Obsolete("Deprecated value. Check the libvlc_MediaPlayerBuffering" +
        " event to know the buffering state of a libvlc_media_player", false)]
        Buffering = 2, Playing, Paused, Stopped,
        Ended, Error, NothingSpecial = 0, Opening,
    };
    private enum libvlc_event_e : int{
        libvlc_MediaPlayerNothingSpecial = 257,
        libvlc_MediaPlayerOpening,
        libvlc_MediaPlayerBuffering,
        libvlc_MediaPlayerPlaying,
        libvlc_MediaPlayerPaused,
        libvlc_MediaPlayerStopped,
        libvlc_MediaPlayerEndReached = 265,
        libvlc_MediaPlayerEncounteredError,
        libvlc_MediaPlayerTimeChanged,
        libvlc_MediaPlayerPositionChanged,
        libvlc_MediaPlayerSeekableChanged,
        libvlc_MediaPlayerPausableChanged,
        libvlc_MediaPlayerLengthChanged = 273,
    };
    [StructLayout(LayoutKind.Explicit)] public struct VideoSize{
        [FieldOffset(0)] public int width;
        [FieldOffset(0)] public uint uwidth;
        [FieldOffset(sizeof(int))] public int height;
        [FieldOffset(sizeof(int))] public uint uheight;
    }
    private delegate void* Lock_cb(void* opaque, void** planes);
    private delegate void Unlock_cb(void* opaque, void* picture, void** planes);
    private delegate void Display_cb(void* opaque, void* picture);
    private delegate void libvlc_callback_t(UIntPtr @event, void* p_data);
    private const string PluginName = "libvlc";
    public static UIntPtr instance;
    private static readonly UIntPtr[] medias = Enumerable.Repeat(UIntPtr.Zero, 36*36).ToArray();
    public static readonly VideoSize[] media_sizes = new VideoSize[36*36];
    public static readonly UIntPtr[] players = Enumerable.Repeat(UIntPtr.Zero, 36*36).ToArray();
    public static readonly UIntPtr[] ev_mgs = Enumerable.Repeat(UIntPtr.Zero, 36*36).ToArray();
    public static readonly bool[] playing = Enumerable.Repeat(false, 36*36).ToArray();
    public static readonly bool[] toStop = Enumerable.Repeat(false, 36*36).ToArray();
    private static readonly libvlc_callback_t endFunc = (e, data)=>{
        *(bool*)data = true; Debug.Log("to end"); };
    private static readonly libvlc_callback_t playFunc = (e, data)=>{
        *(bool*)data = true; };
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    [DllImport("ucrtbase")]
#else
    public static readonly uint[] offsetYs = Enumerable.Repeat<uint>(0, 36*36).ToArray();
    public static readonly byte[][] tex_pixels = Enumerable.Repeat<byte[]>(null, 36*36).ToArray();
    public static readonly byte*[] addrs = new byte*[36*36];
    [DllImport("libavcodec")]
#endif
    private extern static void* memset(void* src, int val, UIntPtr count);
    public static void ClearPixels(ushort num){
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        NativeArray<byte> arr = BMSInfo.textures[num].GetRawTextureData<byte>();
        memset(arr.GetUnsafePtr(), 0, (UIntPtr)arr.Length);
#else
        fixed(void* p = tex_pixels[num])
            memset(p, 0, (UIntPtr)tex_pixels[num].LongLength);
#endif
    }
    [DllImport(PluginName)] private extern static UIntPtr libvlc_new(
        int argc, string[] args);// return instance
    [DllImport(PluginName)] private extern static void libvlc_release(
        UIntPtr instance);
    [DllImport(PluginName)] private extern static UIntPtr libvlc_media_new_path(
        UIntPtr instance, string path);// return media
    // [DllImport(PluginName)] private extern static UIntPtr libvlc_media_new_location(
    //     UIntPtr instance, string path);// return media
    [DllImport(PluginName)] private extern static void libvlc_media_parse(UIntPtr media);
    [DllImport(PluginName)] private extern static void libvlc_media_release(UIntPtr media);
    [DllImport(PluginName)] private extern static UIntPtr libvlc_media_player_new_from_media(
        UIntPtr media);// return player
    // [DllImport(PluginName)] public extern static int libvlc_video_get_size(
    //     UIntPtr player, uint offset, out uint width, out uint height);
    [DllImport(PluginName)] private extern static void libvlc_video_set_format(
        UIntPtr player, string chroma, uint width, uint height, uint pitch);
    [DllImport(PluginName, CallingConvention = CallingConvention.StdCall)]
    private extern static void libvlc_video_set_callbacks(UIntPtr player,
        Lock_cb lock_cb, Unlock_cb unlock_cb, Display_cb display_cb, void* data);
    [DllImport(PluginName)] private extern static void libvlc_media_player_set_time(
        UIntPtr player, long ms);
    [DllImport(PluginName)] private extern static int libvlc_media_player_play(
        UIntPtr player);
    [DllImport(PluginName)] private extern static State libvlc_media_player_get_state(
        UIntPtr player);
    [DllImport(PluginName)] private extern static void libvlc_media_player_set_pause(
        UIntPtr player, int do_pause);// play/resume if zero, pause if non-zero
    // [DllImport(PluginName)] private extern static void libvlc_media_player_pause(
    //     UIntPtr player);
    [DllImport(PluginName)] private extern static void libvlc_media_player_stop(
        UIntPtr player);
    [DllImport(PluginName)] private extern static void libvlc_media_player_release(
        UIntPtr player);
    [DllImport(PluginName)] private extern static UIntPtr libvlc_media_player_event_manager(
        UIntPtr player);// return libvlc_event_manager_t*
    [DllImport(PluginName, CallingConvention = CallingConvention.StdCall)]
    private extern static int libvlc_event_attach(UIntPtr evmn,
        int eventType, libvlc_callback_t c, void* userData);
    [DllImport(PluginName, CallingConvention = CallingConvention.StdCall)]
    private extern static void libvlc_event_detach(UIntPtr evmn,
        int eventType, libvlc_callback_t c, void* userData);
    public static bool PlayerNew(string path, ushort num){
        if(instance == UIntPtr.Zero || !StaticClass.GetVideoSize(
            path, out media_sizes[num].width, out media_sizes[num].height))
            return false;
        if(medias[num] == UIntPtr.Zero) medias[num] = libvlc_media_new_path(instance, path);
        if(medias[num] == UIntPtr.Zero) goto cleanup;
        libvlc_media_parse(medias[num]);
        if(players[num] == UIntPtr.Zero) players[num] = libvlc_media_player_new_from_media(medias[num]);
        if(players[num] == UIntPtr.Zero) goto cleanup;
        if(ev_mgs[num] == UIntPtr.Zero) ev_mgs[num] = libvlc_media_player_event_manager(players[num]);
        if(ev_mgs[num] == UIntPtr.Zero) goto cleanup;
        fixed(bool* p = toStop)
            if(libvlc_event_attach(ev_mgs[num],
                (int)libvlc_event_e.libvlc_MediaPlayerEndReached,
                endFunc, p + num) != 0) goto cleanup;
        libvlc_video_set_format(players[num], "RV24", media_sizes[num].uwidth,
            media_sizes[num].uheight, media_sizes[num].uwidth * 3);
#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        if(media_sizes[num].uwidth <= media_sizes[num].uheight){
            tex_pixels[num] = new byte[3 * media_sizes[num].uwidth
                * media_sizes[num].uheight];
            offsetYs[num] = 0;
            fixed(byte* ptr = tex_pixels[num]) addrs[num] = ptr;
        }else{// if(media_sizes[num].uwidth > media_sizes[num].uheight)
            tex_pixels[num] = new byte[3 * media_sizes[num].uwidth
                * media_sizes[num].uwidth];
            offsetYs[num] = (media_sizes[num].uwidth -
                media_sizes[num].uheight) / 2;
            fixed(byte* ptr = tex_pixels[num])
                addrs[num] = ptr + offsetYs[num] * media_sizes[num].uwidth * 3;
        }
        libvlc_video_set_callbacks(players[num], (opaque, planes)=>{
            *planes = opaque; return null; }, null, null, addrs[num]);
        libvlc_media_player_play(players[num]);
        while(libvlc_media_player_get_state(players[num]) < State.Playing);
        libvlc_media_player_set_pause(players[num], 1);
        fixed(bool* p = playing)
            if(libvlc_event_attach(ev_mgs[num],
                (int)libvlc_event_e.libvlc_MediaPlayerPlaying,
                playFunc, p + num) != 0) goto cleanup;
#endif
        return true;
        cleanup:
        VideoFree(num);
        return false;
    }
    public static void NewVideoTex(ushort num){
        if(players[num] == UIntPtr.Zero) return;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        byte* ptr = null;
        NativeArray<byte> arr;
        if(media_sizes[num].uwidth <= media_sizes[num].uheight){
            BMSInfo.textures[num] = new Texture2D(media_sizes[num].width,
                media_sizes[num].height, TextureFormat.RGB24, false){
                filterMode = FilterMode.Point };
            arr = BMSInfo.textures[num].GetRawTextureData<byte>();
            ptr = (byte*)arr.GetUnsafePtr();// GetUnsafeReadOnlyPtr());
        }else{// if(media_sizes[num].uwidth > media_sizes[num].uheight)
            BMSInfo.textures[num] = new Texture2D(media_sizes[num].width,
                media_sizes[num].width, TextureFormat.RGB24, false){
                filterMode = FilterMode.Point };
            arr = BMSInfo.textures[num].GetRawTextureData<byte>();
            ptr = (byte*)arr.GetUnsafePtr();// GetUnsafeReadOnlyPtr());
            memset(ptr, 0, (UIntPtr)arr.Length);
            ptr += (media_sizes[num].uwidth - media_sizes[num].uheight)
                / 2 * media_sizes[num].uwidth * 3;
            // media_sizes[num].uheight = media_sizes[num].uwidth;
        }
        libvlc_video_set_callbacks(players[num], (opaque, planes)=>{
            *planes = opaque; return null; }, null, null, ptr);
        libvlc_media_player_play(players[num]);
        while(libvlc_media_player_get_state(players[num]) < State.Playing);
        libvlc_media_player_set_pause(players[num], 1);
#else
        if(media_sizes[num].uwidth <= media_sizes[num].uheight){
            BMSInfo.textures[num] = GL_libs.NewRGBTex(tex_pixels[num],
                media_sizes[num].width, media_sizes[num].height, ref BMSInfo.texture_names[num]);
        }else{
            BMSInfo.textures[num] = GL_libs.NewRGBTex(tex_pixels[num],
                media_sizes[num].width, media_sizes[num].width, ref BMSInfo.texture_names[num]);
        }
#endif
    }
    // public static bool PlayerPlaying(ushort num) =>
    //     libvlc_media_player_get_state(players[num]) == State.Playing;
        // num >= players.Length ? false : PlayerPlaying(players[num]);
    // public static bool PlayerPlaying(UIntPtr player){
    //     if(player == UIntPtr.Zero) return false;
    //     return libvlc_media_player_get_state(player) == State.Playing;
    // }
    // public static int PlayerPlay(ushort num) => players[num] == UIntPtr.Zero
    //     ? -1 : libvlc_media_player_play(players[num]);
    public static void PlayerPlay(ushort num){
        if(players[num] != UIntPtr.Zero){
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            libvlc_media_player_set_time(players[num], 0);
#else
            libvlc_media_player_set_time(players[num], 320);
#endif
            libvlc_media_player_set_pause(players[num], 0);
            // libvlc_media_player_play(players[num]);
        }
    }
#if UNITY_EDITOR
    public static void PlayerSetPause(ushort num, int do_pause){
        if(players[num] != UIntPtr.Zero){
            libvlc_media_player_set_pause(players[num], do_pause);
        }
    }
#endif
    public static void PlayerStop(ushort num){
        playing[num] = false;
        if(players[num] != UIntPtr.Zero){
            // libvlc_media_player_stop(players[num]);
            libvlc_media_player_set_pause(players[num], 1);
        }
    }
    public static void VideoFree(ushort num){
        // if(num >= players.Length) return;
        if(ev_mgs[num] != UIntPtr.Zero){
            fixed(bool* p = toStop)
                libvlc_event_detach(ev_mgs[num],
                    (int)libvlc_event_e.libvlc_MediaPlayerEndReached,
                    endFunc, p + num);
            fixed(bool* p = playing)
                libvlc_event_detach(ev_mgs[num],
                    (int)libvlc_event_e.libvlc_MediaPlayerPlaying,
                    playFunc, p + num);
            ev_mgs[num] = UIntPtr.Zero;
        }
        if(players[num] != UIntPtr.Zero){
            libvlc_media_player_stop(players[num]);
            libvlc_media_player_release(players[num]);
            players[num] = UIntPtr.Zero;
        }
        if(medias[num] != UIntPtr.Zero){
            libvlc_media_release(medias[num]);
            medias[num] = UIntPtr.Zero;
        }
        playing[num] = toStop[num] = false;
#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        tex_pixels[num] = null;
        offsetYs[num] = 0;
        addrs[num] = null;
#endif
    }
    public static UIntPtr InstNew(string[] args) => args == null
        ? libvlc_new(0, null) : libvlc_new(args.Length, args);
    // public static void InstNew(string[] args){
    //     instance = (args == null ?
    //     libvlc_new(0, null) :
    //     libvlc_new(args.Length, args));
    // }
    public static void VLCRelease(){
        for(ushort num = 0; num < medias.Length; num++)
            VideoFree(num);
        if(instance != UIntPtr.Zero){
            libvlc_release(instance);
            instance = UIntPtr.Zero;
        }
    }
}
