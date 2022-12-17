using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
public static class VLCPlayer{
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
    private const string PluginName = "libvlc";
    // private const string PluginName = "libvlc5";
    [DllImport(PluginName)] private extern static IntPtr libvlc_new(
        int argc, string[] args);// return instance
    [DllImport(PluginName)] private extern static IntPtr libvlc_media_new_path(
        IntPtr instance, string path);// return media
    // [DllImport(PluginName)] private extern static IntPtr libvlc_media_new_location(
    //     IntPtr instance, string path);// return media
    [DllImport(PluginName)] private extern static void libvlc_media_parse(
        IntPtr media);// parse media
    public static IntPtr MediaNew(IntPtr instance, string path){
        IntPtr media = IntPtr.Zero;
        media = libvlc_media_new_path(instance, path);
        // media = libvlc_media_new_location(instance, path);
        if(media != IntPtr.Zero) libvlc_media_parse(media);
        return media;
    }
    [DllImport(PluginName)] private extern static IntPtr libvlc_media_player_new_from_media(
        IntPtr media);// return player
    // [DllImport(PluginName)] public extern static int libvlc_video_get_size(
    //     IntPtr player, uint offset, out uint width, out uint height);
    [DllImport(PluginName)] private extern static void libvlc_video_set_format(
        IntPtr player, string chroma, uint width, uint height, uint pitch);
    [DllImport(PluginName)] private extern static void libvlc_media_player_set_time(
        IntPtr player, long ms);
    [DllImport(PluginName)] private extern static int libvlc_media_player_play(
        IntPtr player);
    #region libvlc_video_set_callbacks
    private unsafe delegate IntPtr Lock_cb(void* opaque, void** planes);
    private unsafe delegate void Unlock_cb(void* opaque, void* picture, void** planes);
    private unsafe delegate void Display_cb(void* opaque, void* picture);
    [DllImport(PluginName, CallingConvention = CallingConvention.StdCall)]
    private extern static void libvlc_video_set_callbacks(
        IntPtr player, Lock_cb lock_cb, Unlock_cb unlock_cb, Display_cb display_cb, IntPtr data);
    public static unsafe IntPtr PlayerNew(IntPtr media, string chroma, uint width,
    uint height, uint pitch, IntPtr data, long ms = 0){
        if(media == IntPtr.Zero || width < 1 || height < 1 || pitch < 1) return IntPtr.Zero;
        IntPtr player = libvlc_media_player_new_from_media(media);
        if(player != IntPtr.Zero){
            libvlc_video_set_format(player, chroma, width, height, pitch);
            libvlc_video_set_callbacks(player, (opaque, planes)=>{
                *planes = opaque;
                return IntPtr.Zero;
            },(opaque, picture, planes)=>{
                opaque = *planes;
            },null,data);
            if(ms > 0) libvlc_media_player_set_time(player, ms);
            libvlc_media_player_play(player);
        }
        return player;
    }
    #endregion
    #region libvlc_media_player_get_state
    private enum libvlc_state_t{
        libvlc_NothingSpecial = 0,
        libvlc_Opening,
        libvlc_Buffering, /* XXX: Deprecated value. Check the
                        * libvlc_MediaPlayerBuffering event to know the
                        * buffering state of a libvlc_media_player */
        libvlc_Playing,
        libvlc_Paused,
        libvlc_Stopped,
        libvlc_Ended,
        libvlc_Error
    };
    [DllImport(PluginName)] private extern static libvlc_state_t libvlc_media_player_get_state(IntPtr player);
    public static bool PlayerPlaying(IntPtr player){
        libvlc_state_t t = libvlc_media_player_get_state(player);
        // Debug.Log(t);
        // return t < libvlc_state_t.libvlc_Paused;
        return t == libvlc_state_t.libvlc_Playing;
    }
    #endregion
    [DllImport(PluginName)] private extern static void libvlc_media_release(
        IntPtr media);
    private static void MediaFree(IntPtr media){
        if(media != IntPtr.Zero){
            libvlc_media_release(media);
        }
    }
    [DllImport(PluginName)] private extern static void libvlc_media_player_release(
        IntPtr player);
    [DllImport(PluginName)] private extern static void libvlc_media_player_stop(
        IntPtr player);
    public static void PlayerFree(ref IntPtr player){
        if(player != IntPtr.Zero){
            libvlc_media_player_stop(player);
            libvlc_media_player_release(player);
            player = IntPtr.Zero;
        }
    }
    [DllImport(PluginName)] private extern static void libvlc_release(
        IntPtr instance);
    private static void InstFree(ref IntPtr instance){
        if(instance != IntPtr.Zero){
            libvlc_release(instance);
            instance = IntPtr.Zero;
        }
    }
#else
    private const string PluginName = "VLCPlugin";
    [DllImport(PluginName, EntryPoint = "InstNew")] private extern static IntPtr libvlc_new(
        int argc, string[] args);
    [DllImport(PluginName)] public extern static IntPtr MediaNew(IntPtr instance, string path);
    [DllImport(PluginName)] public extern static IntPtr PlayerNew(IntPtr media, string chroma,
        uint width, uint height, uint pitch, IntPtr data, long ms = 0);
    [DllImport(PluginName)] public extern static void PlayerFree(ref IntPtr player);
    [DllImport(PluginName)] private extern static void MediaFree(IntPtr media);
    [DllImport(PluginName)] private extern static void InstFree(ref IntPtr instance);
    [DllImport(PluginName)] public extern static bool PlayerPlaying(IntPtr player);
#endif
    public static IntPtr InstNew(string[] args){
        return args == null ? libvlc_new(0, null) : libvlc_new(args.Length, args);
    }
    public static IntPtr instance;
    public static Dictionary<ushort, IntPtr> medias = new Dictionary<ushort, IntPtr>();
    public struct VideoSize{
        public int width;
        public int height;
    }
    public static Dictionary<ushort, VideoSize> media_sizes = new Dictionary<ushort, VideoSize>();
    public static Texture2D[] media_textures = new Texture2D[4];
    // public static Dictionary<ushort, Texture2D> media_textures = new Dictionary<ushort, Texture2D>();
    public static Color32[][] color32s = new Color32[4][];
    public static IntPtr[] players = new IntPtr[4];
    public static void VLCRelease(){
        for(int p = 0; p < players.Length; p++){
            PlayerFree(ref players[p]);
            media_textures[p] = null;
            color32s[p] = null;
        }
        foreach(ushort num in medias.Keys){
            MediaFree(medias[num]);
        }
        medias.Clear();
        InstFree(ref instance);
        media_sizes.Clear();
        // media_textures.Clear();
    }
}
