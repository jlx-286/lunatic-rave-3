using System;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
public unsafe static class VLCPlayer{
    private enum State{
        [Obsolete("Deprecated value. Check the libvlc_MediaPlayerBuffering" +
        " event to know the buffering state of a libvlc_media_player", false)]
        Buffering = 2, Playing, Paused, Stopped,
        Ended, Error, NothingSpecial = 0, Opening,
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
    private const string PluginName = "libvlc";
    public static UIntPtr instance;
    private static readonly UIntPtr[] medias = Enumerable.Repeat(UIntPtr.Zero, 36*36).ToArray();
    public static readonly VideoSize[] media_sizes = new VideoSize[36*36];
    public static readonly Color32[][] color32s = Enumerable.Repeat<Color32[]>(null, 36*36).ToArray();
    public static readonly UIntPtr[] players = Enumerable.Repeat(UIntPtr.Zero, 36*36).ToArray();
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    public static readonly Texture2D[] media_textures = Enumerable.Repeat<Texture2D>(null, 4).ToArray();
#else
    public static readonly uint[] texture_names = new uint[]{0,0,0,0};
#endif
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
    public static bool PlayerNew(string path, ushort num){
        if(instance == UIntPtr.Zero || !StaticClass.GetVideoSize(
            path, out media_sizes[num].width, out media_sizes[num].height))
            return false;
        medias[num] = libvlc_media_new_path(instance, path);
        if(medias[num] == UIntPtr.Zero) return false;
        libvlc_media_parse(medias[num]);
        players[num] = libvlc_media_player_new_from_media(medias[num]);
        if(players[num] == UIntPtr.Zero){
            if(medias[num] != UIntPtr.Zero){
                libvlc_media_release(medias[num]);
                medias[num] = UIntPtr.Zero;
            }
            return false;
        }
        libvlc_video_set_format(players[num], "RGBA", media_sizes[num].uwidth,
            media_sizes[num].uheight, media_sizes[num].uwidth * 4);
        if(media_sizes[num].uwidth <= media_sizes[num].uheight){
            color32s[num] = new Color32[media_sizes[num].uwidth * media_sizes[num].uheight];
            fixed(Color32* data = color32s[num])
                libvlc_video_set_callbacks(players[num], (opaque, planes)=>{
                    *planes = opaque;
                    return null;
                }, null, null, data);
        }else{// if(media_sizes[num].uwidth > media_sizes[num].uheight)
            color32s[num] = new Color32[media_sizes[num].uwidth * media_sizes[num].uwidth];
            fixed(Color32* data = color32s[num])
                libvlc_video_set_callbacks(players[num], (opaque, planes)=>{
                    *planes = opaque;
                    return null;
                }, null, null, data + (media_sizes[num].uwidth -
                    media_sizes[num].uheight) / 2 * media_sizes[num].uwidth);
            media_sizes[num].uheight = media_sizes[num].uwidth;
        }
        libvlc_media_player_play(players[num]);
        while(libvlc_media_player_get_state(players[num]) < State.Playing);
        libvlc_media_player_set_pause(players[num], 1);
        return true;
    }
    public static bool PlayerPlaying(ushort num) =>
        libvlc_media_player_get_state(players[num]) == State.Playing;
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
        if(players[num] != UIntPtr.Zero){
            // libvlc_media_player_stop(players[num]);
            libvlc_media_player_set_pause(players[num], 1);
        }
    }
    public static void VideoFree(ushort num){
        // if(num >= players.Length) return;
        if(players[num] != UIntPtr.Zero){
            libvlc_media_player_stop(players[num]);
            libvlc_media_player_release(players[num]);
            players[num] = UIntPtr.Zero;
        }
        if(medias[num] != UIntPtr.Zero){
            libvlc_media_release(medias[num]);
            medias[num] = UIntPtr.Zero;
        }
        color32s[num] = null;
    }
    public static UIntPtr InstNew(string[] args) => args == null
        ? libvlc_new(0, null) : libvlc_new(args.Length, args);
    // public static void InstNew(string[] args){
    //     instance = (args == null ?
    //     libvlc_new(0, null) :
    //     libvlc_new(args.Length, args));
    // }
    public static void VLCRelease(){
#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
        // GL_libs.BindTexture(0);
        fixed(uint* p = texture_names)
            GL_libs.glDeleteTextures(texture_names.Length, p);
#endif
        for(byte p = 0; p < 4; p++)
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            media_textures[p] = null;
#else
            texture_names[p] = 0;
#endif
        for(ushort num = 0; num < medias.Length; num++)
            VideoFree(num);
        if(instance != UIntPtr.Zero){
            libvlc_release(instance);
            instance = UIntPtr.Zero;
        }
    }
}
