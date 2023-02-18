using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
public static class VLCPlayer{
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
    private const string PluginName = "libvlc";
    // private const string PluginName = "libvlc5";
    [DllImport(PluginName)] private extern static UIntPtr libvlc_new(
        int argc, string[] args);// return instance
    [DllImport(PluginName)] private extern static UIntPtr libvlc_media_new_path(
        UIntPtr instance, string path);// return media
    // [DllImport(PluginName)] private extern static UIntPtr libvlc_media_new_location(
    //     UIntPtr instance, string path);// return media
    [DllImport(PluginName)] private extern static void libvlc_media_parse(
        UIntPtr media);// parse media
    public static UIntPtr MediaNew(UIntPtr instance, string path){
        UIntPtr media = UIntPtr.Zero;
        media = libvlc_media_new_path(instance, path);
        // media = libvlc_media_new_location(instance, path);
        if(media != UIntPtr.Zero) libvlc_media_parse(media);
        return media;
    }
    [DllImport(PluginName)] private extern static UIntPtr libvlc_media_player_new_from_media(
        UIntPtr media);// return player
    // [DllImport(PluginName)] public extern static int libvlc_video_get_size(
    //     UIntPtr player, uint offset, out uint width, out uint height);
    [DllImport(PluginName)] private extern static void libvlc_video_set_format(
        UIntPtr player, string chroma, uint width, uint height, uint pitch);
    [DllImport(PluginName)] private extern static void libvlc_media_player_set_time(
        UIntPtr player, long ms);
    [DllImport(PluginName)] private extern static int libvlc_media_player_play(
        UIntPtr player);
    #region libvlc_video_set_callbacks
    private unsafe delegate UIntPtr Lock_cb(void* opaque, void** planes);
    private unsafe delegate void Unlock_cb(void* opaque, void* picture, void** planes);
    private unsafe delegate void Display_cb(void* opaque, void* picture);
    [DllImport(PluginName, CallingConvention = CallingConvention.StdCall)]
    private extern static unsafe void libvlc_video_set_callbacks(
        UIntPtr player, Lock_cb lock_cb, Unlock_cb unlock_cb, Display_cb display_cb, void* data);
    public static unsafe UIntPtr PlayerNew(UIntPtr media, string chroma, uint width,
    uint height, uint pitch, void* data, long ms = 0){
        if(media == UIntPtr.Zero || width < 1 || height < 1 || pitch < 1) return UIntPtr.Zero;
        UIntPtr player = libvlc_media_player_new_from_media(media);
        if(player != UIntPtr.Zero){
            libvlc_video_set_format(player, chroma, width, height, pitch);
            libvlc_video_set_callbacks(player, (opaque, planes)=>{
                *planes = opaque;
                return UIntPtr.Zero;
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
    [DllImport(PluginName)] private extern static libvlc_state_t libvlc_media_player_get_state(UIntPtr player);
    public static bool PlayerPlaying(UIntPtr player){
        libvlc_state_t t = libvlc_media_player_get_state(player);
        // Debug.Log(t);
        // return t < libvlc_state_t.libvlc_Paused;
        return t == libvlc_state_t.libvlc_Playing;
    }
    #endregion
    [DllImport(PluginName)] private extern static void libvlc_media_release(
        UIntPtr media);
    private static void MediaFree(UIntPtr media){
        if(media != UIntPtr.Zero){
            libvlc_media_release(media);
        }
    }
    [DllImport(PluginName)] private extern static void libvlc_media_player_release(
        UIntPtr player);
    [DllImport(PluginName)] private extern static void libvlc_media_player_stop(
        UIntPtr player);
    public static void PlayerFree(ref UIntPtr player){
        if(player != UIntPtr.Zero){
            libvlc_media_player_stop(player);
            libvlc_media_player_release(player);
            player = UIntPtr.Zero;
        }
    }
    [DllImport(PluginName)] private extern static void libvlc_release(
        UIntPtr instance);
    private static void InstFree(ref UIntPtr instance){
        if(instance != UIntPtr.Zero){
            libvlc_release(instance);
            instance = UIntPtr.Zero;
        }
    }
#else
    private const string PluginName = "VLCPlugin";
    [DllImport(PluginName, EntryPoint = "InstNew")] private extern static UIntPtr libvlc_new(
        int argc, string[] args);
    [DllImport(PluginName)] public extern static UIntPtr MediaNew(UIntPtr instance, string path);
    [DllImport(PluginName)] public extern static unsafe UIntPtr PlayerNew(UIntPtr media, string chroma,
        uint width, uint height, uint pitch, void* data, long ms = 0);
    [DllImport(PluginName)] public extern static void PlayerFree(ref UIntPtr player);
    [DllImport(PluginName)] private extern static void MediaFree(UIntPtr media);
    [DllImport(PluginName)] private extern static void InstFree(ref UIntPtr instance);
    [DllImport(PluginName)] public extern static bool PlayerPlaying(UIntPtr player);
#endif
    public static UIntPtr InstNew(string[] args){
        return args == null ? libvlc_new(0, null) : libvlc_new(args.Length, args);
    }
    public static UIntPtr instance;
    public static Dictionary<ushort, UIntPtr> medias = new Dictionary<ushort, UIntPtr>();
    [StructLayout(LayoutKind.Explicit)]
    public struct VideoSize{
        [FieldOffset(0)] public int width;
        [FieldOffset(sizeof(int))] public int height;
    }
    public static Dictionary<ushort, VideoSize> media_sizes = new Dictionary<ushort, VideoSize>();
    public static Texture2D[] media_textures = new Texture2D[4];
    // public static Dictionary<ushort, Texture2D> media_textures = new Dictionary<ushort, Texture2D>();
    public static Color32[][] color32s = new Color32[4][];
    public static UIntPtr[] players = new UIntPtr[4];
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
