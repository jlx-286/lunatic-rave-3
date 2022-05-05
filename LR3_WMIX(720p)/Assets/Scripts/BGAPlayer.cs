//using RenderHeads.Media.AVProVideo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class BGAPlayer : MonoBehaviour {
    private BMSReader BMS_Reader;
    public BMSPlayer BMS_Player;
    public RawImage[] rawImages;
    [HideInInspector] public ushort bgi_num;
    //private float video_speed;
	// Use this for initialization
	void Start () {
        BMS_Reader = MainVars.BMSReader;
        //BMS_Player = MainVars.BMSPlayer;
        //video_speed = Mathf.Pow(2f, MainVars.freq / 12f);
        for(int p = 0; p < VLCPlayer.players.Length; p++){
            VLCPlayer.players[p] = IntPtr.Zero;
        }
    }
	
	// Update is called once per frame
	//void FixedUpdate () {}
    private void Update(){
        if (BMS_Player.escaped) { return; }
        if (!BMS_Player.no_bgi){
            while (BMS_Player.bga_table_row < BMS_Reader.bga_table.Rows.Count){
                if((double)BMS_Reader.bga_table.Rows[BMS_Player.bga_table_row][1] - BMS_Player.playing_time < Time.deltaTime){
                    bgi_num = StaticClass.Convert36To10(BMS_Reader.bga_table.Rows[BMS_Player.bga_table_row][2].ToString());
                    switch (BMS_Reader.bga_table.Rows[BMS_Player.bga_table_row][0].ToString()){
                        case "04"://base
                            if (BMS_Reader.isVideo[bgi_num] && VLCPlayer.medias.ContainsKey(bgi_num)){
                                try{
                                    if(VLCPlayer.players[0] != IntPtr.Zero){
                                        VLCPlayer.libvlc_media_player_stop(VLCPlayer.players[0]);
                                        VLCPlayer.libvlc_media_player_release(VLCPlayer.players[0]);
                                        VLCPlayer.players[0] = IntPtr.Zero;
                                    }
                                    VLCPlayer.players[0] = VLCPlayer.libvlc_media_player_new_from_media(
                                        VLCPlayer.medias[bgi_num]);
                                    if(VLCPlayer.players[0] != IntPtr.Zero){
                                        VLCPlayer.media_textures[0] = new Texture2D(
                                            Convert.ToInt32(VLCPlayer.media_sizes[bgi_num].Split()[0]),
                                            Convert.ToInt32(VLCPlayer.media_sizes[bgi_num].Split()[1]),
                                            TextureFormat.RGBA32, false
                                        );
                                        VLCPlayer.color32s[0] = new Color32[
                                            VLCPlayer.media_textures[0].width * VLCPlayer.media_textures[0].height];
                                        VLCPlayer.libvlc_video_set_format(VLCPlayer.players[0],
                                            "RGBA", (uint)VLCPlayer.media_textures[0].width,
                                            (uint)VLCPlayer.media_textures[0].height,
                                            (uint)VLCPlayer.media_textures[0].width * 4);
                                        VLCPlayer.VLC_callback(VLCPlayer.players[0],
                                            Marshal.UnsafeAddrOfPinnedArrayElement(VLCPlayer.color32s[0], 0));
                                        //VLCPlayer.libvlc_media_player_set_time(VLCPlayer.players[0],
                                        //    (long)(1000 * 60 * 2 / BMS_Reader.start_bpm));
                                        VLCPlayer.libvlc_media_player_play(VLCPlayer.players[0]);
                                    }
                                }
                                catch (Exception e){
                                    Debug.LogWarning(e.Message);
                                }
                            }
                            else if (!BMS_Reader.isVideo[bgi_num]){
                                if(VLCPlayer.players[0] != IntPtr.Zero){
                                    VLCPlayer.libvlc_media_player_stop(VLCPlayer.players[0]);
                                    VLCPlayer.libvlc_media_player_release(VLCPlayer.players[0]);
                                    VLCPlayer.players[0] = IntPtr.Zero;
                                }
                                for(int i = 0; i < rawImages.Length; i += 4){
                                    rawImages[i].texture = BMS_Reader.textures[bgi_num];
                                }
                            }
                            break;
                        case "07"://layer
                            if (BMS_Reader.isVideo[bgi_num] && VLCPlayer.medias.ContainsKey(bgi_num)){
                                try{
                                    if (VLCPlayer.players[1] != IntPtr.Zero){
                                        VLCPlayer.libvlc_media_player_stop(VLCPlayer.players[1]);
                                        VLCPlayer.libvlc_media_player_release(VLCPlayer.players[1]);
                                        VLCPlayer.players[1] = IntPtr.Zero;
                                    }
                                    VLCPlayer.players[1] = VLCPlayer.libvlc_media_player_new_from_media(
                                        VLCPlayer.medias[bgi_num]);
                                    if (VLCPlayer.players[1] != IntPtr.Zero){
                                        VLCPlayer.media_textures[1] = new Texture2D(
                                            Convert.ToInt32(VLCPlayer.media_sizes[bgi_num].Split()[0]),
                                            Convert.ToInt32(VLCPlayer.media_sizes[bgi_num].Split()[1]),
                                            TextureFormat.RGBA32, false
                                        );
                                        VLCPlayer.color32s[1] = new Color32[
                                            VLCPlayer.media_textures[1].width * VLCPlayer.media_textures[1].height];
                                        VLCPlayer.libvlc_video_set_format(VLCPlayer.players[1],
                                            "RGBA", (uint)VLCPlayer.media_textures[1].width,
                                            (uint)VLCPlayer.media_textures[1].height,
                                            (uint)VLCPlayer.media_textures[1].width * 4);
                                        VLCPlayer.VLC_callback(VLCPlayer.players[1],
                                            Marshal.UnsafeAddrOfPinnedArrayElement(VLCPlayer.color32s[1], 0));
                                        //VLCPlayer.libvlc_media_player_set_time(VLCPlayer.players[1],
                                        //    (long)(1000 * 60 * 2 / BMS_Reader.start_bpm));
                                        VLCPlayer.libvlc_media_player_play(VLCPlayer.players[1]);
                                    }
                                }
                                catch (Exception e){
                                    Debug.LogWarning(e.Message);
                                }
                            }
                            else if (!BMS_Reader.isVideo[bgi_num]){
                                if (VLCPlayer.players[1] != IntPtr.Zero){
                                    VLCPlayer.libvlc_media_player_stop(VLCPlayer.players[1]);
                                    VLCPlayer.libvlc_media_player_release(VLCPlayer.players[1]);
                                    VLCPlayer.players[1] = IntPtr.Zero;
                                }
                                for (int i = 1; i < rawImages.Length; i += 4){
                                    rawImages[i].texture = BMS_Reader.textures[bgi_num];
                                }
                            }
                            break;
                        case "0A"://layer2
                            if (BMS_Reader.isVideo[bgi_num] && VLCPlayer.medias.ContainsKey(bgi_num)){
                                try{
                                    if (VLCPlayer.players[2] != IntPtr.Zero){
                                        VLCPlayer.libvlc_media_player_stop(VLCPlayer.players[2]);
                                        VLCPlayer.libvlc_media_player_release(VLCPlayer.players[2]);
                                        VLCPlayer.players[2] = IntPtr.Zero;
                                    }
                                    VLCPlayer.players[2] = VLCPlayer.libvlc_media_player_new_from_media(
                                        VLCPlayer.medias[bgi_num]);
                                    if (VLCPlayer.players[2] != IntPtr.Zero){
                                        VLCPlayer.media_textures[2] = new Texture2D(
                                            Convert.ToInt32(VLCPlayer.media_sizes[bgi_num].Split()[0]),
                                            Convert.ToInt32(VLCPlayer.media_sizes[bgi_num].Split()[1]),
                                            TextureFormat.RGBA32, false
                                        );
                                        VLCPlayer.color32s[2] = new Color32[
                                            VLCPlayer.media_textures[2].width * VLCPlayer.media_textures[2].height];
                                        VLCPlayer.libvlc_video_set_format(VLCPlayer.players[2],
                                            "RGBA", (uint)VLCPlayer.media_textures[2].width,
                                            (uint)VLCPlayer.media_textures[2].height,
                                            (uint)VLCPlayer.media_textures[2].width * 4);
                                        VLCPlayer.VLC_callback(VLCPlayer.players[2],
                                            Marshal.UnsafeAddrOfPinnedArrayElement(VLCPlayer.color32s[2], 0));
                                        //VLCPlayer.libvlc_media_player_set_time(VLCPlayer.players[2],
                                        //    (long)(1000 * 60 * 2 / BMS_Reader.start_bpm));
                                        VLCPlayer.libvlc_media_player_play(VLCPlayer.players[2]);
                                    }
                                }
                                catch (Exception e){
                                    Debug.LogWarning(e.Message);
                                }
                            }
                            else if (!BMS_Reader.isVideo[bgi_num]){
                                if (VLCPlayer.players[2] != IntPtr.Zero){
                                    VLCPlayer.libvlc_media_player_stop(VLCPlayer.players[2]);
                                    VLCPlayer.libvlc_media_player_release(VLCPlayer.players[2]);
                                    VLCPlayer.players[2] = IntPtr.Zero;
                                }
                                for (int i = 2; i < rawImages.Length; i += 4){
                                    rawImages[i].texture = BMS_Reader.textures[bgi_num];
                                }
                            }
                            break;
                        //case "06":// bad/poor
                        //    if (BMSReader.isVideo.ContainsKey(num) && BMSReader.isVideo[num]){
                        //        //
                        //    }else if (BMSReader.isVideo.ContainsKey(num) && !BMSReader.isVideo[num]){
                        //        //
                        //    }
                        //    break;
                    }
                    BMS_Player.bga_table_row++;
                }else{
                    break;
                }
            }
        }
    }
}
