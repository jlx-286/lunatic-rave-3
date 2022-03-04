using RenderHeads.Media.AVProVideo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BGAPlayer : MonoBehaviour {
    private BMSReader BMS_Reader;
    public BMSPlayer BMS_Player;
    public DisplayUGUI[] displayUGUIs;
    public MediaPlayer[] mediaPlayers;
    private ushort bgi_num;
    private float video_speed;
	// Use this for initialization
	void Start () {
        BMS_Reader = MainVars.BMSReader;
        //BMS_Player = MainVars.BMSPlayer;
        video_speed = Mathf.Pow(2f, MainVars.freq / 12f);
    }
	
	// Update is called once per frame
	//void Update () {}
    private void FixedUpdate(){
        if (BMS_Player.escaped) { return; }
        if (!BMS_Player.no_bgi){
            while (BMS_Reader.bga_table_row < BMS_Reader.bga_table.Rows.Count){
                if((double)BMS_Reader.bga_table.Rows[BMS_Reader.bga_table_row][1] - BMS_Player.playing_time < Time.fixedDeltaTime
                    && (double)BMS_Reader.bga_table.Rows[BMS_Reader.bga_table_row][1] - BMS_Player.playing_time > -double.Epsilon
                ){
                    bgi_num = StaticClass.Convert36To10(BMS_Reader.bga_table.Rows[BMS_Reader.bga_table_row][2].ToString());
                    switch (BMS_Reader.bga_table.Rows[BMS_Reader.bga_table_row][0].ToString()){
                        case "04"://base
                            if (BMS_Reader.isVideo[bgi_num]){
                                for (int i = 0; i < displayUGUIs.Length; i += 4){
                                    displayUGUIs[i].CurrentMediaPlayer = mediaPlayers[0];
                                }
                                try{
                                    if (File.Exists(BMS_Reader.bms_directory + BMS_Reader.bga_paths[bgi_num])){
                                        mediaPlayers[0].CloseMedia();
                                        mediaPlayers[0].OpenMedia(MediaPathType.AbsolutePathOrURL,
                                            BMS_Reader.bms_directory + BMS_Reader.bga_paths[bgi_num]);
                                        Debug.Log(mediaPlayers[0].Control.IsPlaying());
                                        mediaPlayers[0].PlaybackRate = video_speed;
                                        mediaPlayers[0].Control.SeekFast(60 * 2 / BMS_Reader.start_bpm);
                                        mediaPlayers[0].Control.Play();
                                    }
                                }
                                catch (Exception e){
                                    Debug.LogWarning(e.Message);
                                }
                            }
                            else if (!BMS_Reader.isVideo[bgi_num]){
                                if(File.Exists(mediaPlayers[0].MediaPath.Path) && mediaPlayers[0].Control.IsPlaying()){
                                    mediaPlayers[0].Control.Stop();
                                    mediaPlayers[0].CloseMedia();
                                }
                                for (int i = 0; i < displayUGUIs.Length; i += 4){
                                    displayUGUIs[i].DefaultTexture = BMS_Reader.textures[bgi_num];
                                    displayUGUIs[i].CurrentMediaPlayer = null;
                                }
                            }
                            break;
                        case "07"://layer
                            if (BMS_Reader.isVideo[bgi_num]){
                                for (int i = 1; i < displayUGUIs.Length; i += 4){
                                    displayUGUIs[i].CurrentMediaPlayer = mediaPlayers[1];
                                }
                                try{
                                    if (File.Exists(BMS_Reader.bms_directory + BMS_Reader.bga_paths[bgi_num])){
                                        mediaPlayers[1].CloseMedia();
                                        mediaPlayers[1].OpenMedia(MediaPathType.AbsolutePathOrURL,
                                            BMS_Reader.bms_directory + BMS_Reader.bga_paths[bgi_num]);
                                        mediaPlayers[1].PlaybackRate = video_speed;
                                        mediaPlayers[1].Control.SeekFast(60 * 2 / BMS_Reader.start_bpm);
                                        mediaPlayers[1].Control.Play();
                                    }
                                }catch (Exception e){
                                    Debug.LogWarning(e.Message);
                                }
                            }
                            else if (!BMS_Reader.isVideo[bgi_num]){
                                if(File.Exists(mediaPlayers[1].MediaPath.Path) && mediaPlayers[0].Control.IsPlaying()){
                                    mediaPlayers[1].Control.Stop();
                                    mediaPlayers[1].CloseMedia();
                                }
                                for (int i = 1; i < displayUGUIs.Length; i += 4){
                                    displayUGUIs[i].DefaultTexture = BMS_Reader.textures[bgi_num];
                                    displayUGUIs[i].CurrentMediaPlayer = null;
                                }
                            }
                            break;
                        case "0A"://layer2
                            if (BMS_Reader.isVideo[bgi_num]){
                                for (int i = 2; i < displayUGUIs.Length; i += 4){
                                    displayUGUIs[i].CurrentMediaPlayer = mediaPlayers[2];
                                }
                                try{
                                    if (File.Exists(BMS_Reader.bms_directory + BMS_Reader.bga_paths[bgi_num])){
                                        mediaPlayers[2].CloseMedia();
                                        mediaPlayers[2].OpenMedia(MediaPathType.AbsolutePathOrURL,
                                            BMS_Reader.bms_directory + BMS_Reader.bga_paths[bgi_num]);
                                        mediaPlayers[2].PlaybackRate = video_speed;
                                        mediaPlayers[2].Control.SeekFast(60 * 2 / BMS_Reader.start_bpm);
                                        mediaPlayers[2].Control.Play(); 
                                    }
                                }catch (Exception e){
                                    Debug.LogWarning(e.Message);
                                }
                            }
                            else if (!BMS_Reader.isVideo[bgi_num]){
                                if(File.Exists(mediaPlayers[2].MediaPath.Path) && mediaPlayers[0].Control.IsPlaying()){
                                    mediaPlayers[2].Control.Stop();
                                    mediaPlayers[2].CloseMedia();
                                }
                                for (int i = 2; i < displayUGUIs.Length; i += 4){
                                    displayUGUIs[i].DefaultTexture = BMS_Reader.textures[bgi_num];
                                    displayUGUIs[i].CurrentMediaPlayer = null;
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
                    BMS_Reader.bga_table_row++;
                }else{
                    break;
                }
            }
        }
    }
}
