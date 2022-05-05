using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class VLCPlayer{
    [DllImport("libvlc")] public extern static IntPtr libvlc_new(
        int argc, string[] args);// return instance
    [DllImport("libvlc")] public extern static IntPtr libvlc_media_new_path(
        IntPtr instance, string path);// return media
    [DllImport("libvlc")] public extern static IntPtr libvlc_media_new_location(
        IntPtr instance, string path);// return media
    [DllImport("libvlc")] public extern static void libvlc_media_parse(
        IntPtr media);// parse media
    [DllImport("libvlc")] public extern static IntPtr libvlc_media_player_new_from_media(
        IntPtr media);// return player
    [DllImport("libvlc")] public extern static int libvlc_video_get_size(
        IntPtr player, uint offset, out uint width, out uint height);
    [DllImport("libvlc")] public extern static void libvlc_video_set_format(
        IntPtr player, string chroma, uint width, uint height, uint pitch);
    [DllImport("TestPlugin")] public extern static void VLC_callback(
        IntPtr player, IntPtr data);
    [DllImport("libvlc")] public extern static int libvlc_media_player_play(
        IntPtr player);
    [DllImport("TestPlugin")] public extern static bool LibVLC_IsPlaying(
        IntPtr player);
    [DllImport("libvlc")] public extern static void libvlc_media_player_release(
        IntPtr player);
    [DllImport("libvlc")] public extern static void libvlc_media_release(
        IntPtr media);
    [DllImport("libvlc")] public extern static void libvlc_release(
        IntPtr instance);
    [DllImport("libvlc")] public extern static void libvlc_media_player_stop(
        IntPtr player);
    [DllImport("libvlc")] public extern static void libvlc_media_player_set_time(IntPtr player, long ms);
    public static IntPtr instance;
    public static Dictionary<ushort, IntPtr> medias = new Dictionary<ushort, IntPtr>();
    public static Dictionary<ushort, string> media_sizes = new Dictionary<ushort, string>();
    public static Texture2D[] media_textures = new Texture2D[4];
    public static Color32[][] color32s = new Color32[4][];
    public static IntPtr[] players = new IntPtr[4];
    public static void VLCRelease(){
        for(int p = 0; p < players.Length; p++){
            if(players[p] != IntPtr.Zero){
                libvlc_media_player_stop(players[p]);
                libvlc_media_player_release(players[p]);
                players[p] = IntPtr.Zero;
            }
            media_textures[p] = null;
            color32s[p] = null;
        }
        //Debug.Log(medias.Count);
        foreach(ushort num in medias.Keys){
            if(medias[num] != IntPtr.Zero){
                libvlc_media_release(medias[num]);
                //medias[num] = IntPtr.Zero;
                //medias.Remove(num);
            }
        }
        medias.Clear();
        if (instance != IntPtr.Zero){
            libvlc_release(instance);
            instance = IntPtr.Zero;
        }
        media_sizes.Clear();
        GC.Collect();
    }
}
