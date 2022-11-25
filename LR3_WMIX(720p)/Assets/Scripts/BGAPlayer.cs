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
	// Use this for initialization
	void Start () {
        BMS_Reader = MainVars.BMSReader;
        //BMS_Player = MainVars.BMSPlayer;
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
                                VLCPlayer.PlayerFree(ref VLCPlayer.players[0]);
                                try{
                                    VLCPlayer.media_textures[0] = new Texture2D(
                                        Convert.ToInt32(VLCPlayer.media_sizes[bgi_num].Split()[0]),
                                        Convert.ToInt32(VLCPlayer.media_sizes[bgi_num].Split()[1]),
                                        TextureFormat.RGBA32, false);
                                    VLCPlayer.color32s[0] = new Color32[
                                        VLCPlayer.media_textures[0].width * VLCPlayer.media_textures[0].height];
                                    VLCPlayer.players[0] = VLCPlayer.PlayerNew(VLCPlayer.medias[bgi_num], "RGBA",
                                        (uint)VLCPlayer.media_textures[0].width,
                                        (uint)VLCPlayer.media_textures[0].height,
                                        (uint)VLCPlayer.media_textures[0].width * 4,
                                        Marshal.UnsafeAddrOfPinnedArrayElement(VLCPlayer.color32s[0], 0)
                                        // , (long)(1000 * 60 * 2 / BMS_Reader.start_bpm)
                                    );
                                }
                                catch (Exception e){
                                    Debug.LogWarning(e.Message);
                                }
                            }
                            else if (!BMS_Reader.isVideo[bgi_num]){
                                VLCPlayer.PlayerFree(ref VLCPlayer.players[0]);
                                for(int i = 0; i < rawImages.Length; i += 4){
                                    rawImages[i].texture = BMS_Reader.textures[bgi_num];
                                }
                            }
                            break;
                        case "07"://layer
                            if (BMS_Reader.isVideo[bgi_num] && VLCPlayer.medias.ContainsKey(bgi_num)){
                                VLCPlayer.PlayerFree(ref VLCPlayer.players[1]);
                                try{
                                    VLCPlayer.media_textures[1] = new Texture2D(
                                        Convert.ToInt32(VLCPlayer.media_sizes[bgi_num].Split()[0]),
                                        Convert.ToInt32(VLCPlayer.media_sizes[bgi_num].Split()[1]),
                                        TextureFormat.RGBA32, false);
                                    VLCPlayer.color32s[1] = new Color32[
                                        VLCPlayer.media_textures[1].width * VLCPlayer.media_textures[1].height];
                                    VLCPlayer.players[1] = VLCPlayer.PlayerNew(VLCPlayer.medias[bgi_num], "RGBA",
                                        (uint)VLCPlayer.media_textures[1].width,
                                        (uint)VLCPlayer.media_textures[1].height,
                                        (uint)VLCPlayer.media_textures[1].width * 4,
                                        Marshal.UnsafeAddrOfPinnedArrayElement(VLCPlayer.color32s[1], 0)
                                        // , (long)(1000 * 60 * 2 / BMS_Reader.start_bpm)
                                    );
                                }
                                catch (Exception e){
                                    Debug.LogWarning(e.Message);
                                }
                            }
                            else if (!BMS_Reader.isVideo[bgi_num]){
                                VLCPlayer.PlayerFree(ref VLCPlayer.players[1]);
                                for (int i = 1; i < rawImages.Length; i += 4){
                                    rawImages[i].texture = BMS_Reader.textures[bgi_num];
                                }
                            }
                            break;
                        case "0A"://layer2
                            if (BMS_Reader.isVideo[bgi_num] && VLCPlayer.medias.ContainsKey(bgi_num)){
                                VLCPlayer.PlayerFree(ref VLCPlayer.players[2]);
                                try{
                                    VLCPlayer.media_textures[2] = new Texture2D(
                                        Convert.ToInt32(VLCPlayer.media_sizes[bgi_num].Split()[0]),
                                        Convert.ToInt32(VLCPlayer.media_sizes[bgi_num].Split()[1]),
                                        TextureFormat.RGBA32, false);
                                    VLCPlayer.color32s[2] = new Color32[
                                        VLCPlayer.media_textures[2].width * VLCPlayer.media_textures[2].height];
                                    VLCPlayer.players[2] = VLCPlayer.PlayerNew(VLCPlayer.medias[bgi_num], "RGBA",
                                        (uint)VLCPlayer.media_textures[2].width,
                                        (uint)VLCPlayer.media_textures[2].height,
                                        (uint)VLCPlayer.media_textures[2].width * 4,
                                        Marshal.UnsafeAddrOfPinnedArrayElement(VLCPlayer.color32s[2], 0)
                                        // , (long)(1000 * 60 * 2 / BMS_Reader.start_bpm)
                                    );
                                }
                                catch (Exception e){
                                    Debug.LogWarning(e.Message);
                                }
                            }
                            else if (!BMS_Reader.isVideo[bgi_num]){
                                VLCPlayer.PlayerFree(ref VLCPlayer.players[2]);
                                for (int i = 2; i < rawImages.Length; i += 4){
                                    rawImages[i].texture = BMS_Reader.textures[bgi_num];
                                }
                            }
                            break;
                        //case "06":// bad/poor
                        //    if (BMS_Reader.isVideo[bgi_num] && VLCPlayer.medias.ContainsKey(bgi_num)){
                        //        //
                        //    }else if (!BMSReader.isVideo[num]){
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
