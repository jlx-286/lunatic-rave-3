using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
public unsafe static class VLCPlayer{
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
    private delegate void* Lock_cb(void* opaque, void** planes);
    private delegate void Unlock_cb(void* opaque, void* picture, void** planes);
    private delegate void Display_cb(void* opaque, void* picture);
    [DllImport(PluginName, CallingConvention = CallingConvention.StdCall)]
    private extern static void libvlc_video_set_callbacks(UIntPtr player,
        Lock_cb lock_cb, Unlock_cb unlock_cb, Display_cb display_cb, void* data);
    public static UIntPtr PlayerNew(UIntPtr media, uint width, uint height, void* data, long ms = 0){
        if(media == UIntPtr.Zero || width < 1 || height < 1) return UIntPtr.Zero;
        UIntPtr player = libvlc_media_player_new_from_media(media);
        if(player != UIntPtr.Zero){
            libvlc_video_set_format(player, "RGBA", width, height, width * 4);
            libvlc_video_set_callbacks(player, (opaque, planes)=>{
                *planes = opaque;
                return null;
            },null,null,data);
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
    [DllImport(PluginName)] public extern static UIntPtr PlayerNew(
        UIntPtr media, uint width, uint height, void* data, long ms = 0);
    [DllImport(PluginName)] public extern static void PlayerFree(ref UIntPtr player);
    [DllImport(PluginName)] private extern static void MediaFree(UIntPtr media);
    [DllImport(PluginName)] private extern static void InstFree(ref UIntPtr instance);
    [DllImport(PluginName)] public extern static bool PlayerPlaying(UIntPtr player);
#endif
    public static UIntPtr InstNew(string[] args) => args == null
        ? libvlc_new(0, null) : libvlc_new(args.Length, args);
    public static UIntPtr instance;
    public static readonly UIntPtr[] medias = Enumerable.Repeat(UIntPtr.Zero, 36*36).ToArray();
    [StructLayout(LayoutKind.Explicit)] public struct VideoSize{
        [FieldOffset(0)] public int width;
        [FieldOffset(sizeof(int))] public int height;
        public VideoSize(int w, int h){ width = w; height = h; }
    }
    public static readonly VideoSize[] media_sizes = new VideoSize[36*36];
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    public static readonly Texture2D[] media_textures = Enumerable.Repeat<Texture2D>(null, 4).ToArray();
#else
    public static readonly uint[] texture_names = new uint[]{0,0,0,0};
#endif
    public static readonly Color32[][] color32s = Enumerable.Repeat<Color32[]>(null, 4).ToArray();
    public static readonly UIntPtr[] players = Enumerable.Repeat(UIntPtr.Zero, 4).ToArray();
    public static void VLCRelease(){
#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        // GL_libs.BindTexture(0);
        fixed(uint* p = texture_names)
            GL_libs.glDeleteTextures(texture_names.Length, p);
#endif
        for(byte p = 0; p < players.Length; p++){
            PlayerFree(ref players[p]);
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            media_textures[p] = null;
#else
            texture_names[p] = 0;
#endif
            color32s[p] = null;
        }
        for(ushort num = 0; num < medias.Length; num++){
            MediaFree(medias[num]);
            medias[num] = UIntPtr.Zero;
        }
        InstFree(ref instance);
    }
}
