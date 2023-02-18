using System;
using UnityEngine;
public class BGMPlayer : MonoBehaviour {
    public BMSPlayer BMS_Player;
    //private void Start(){}
	//private void Update(){}
    private void FixedUpdate(){
        if(BMS_Player.escaped) return;
        if(!BMS_Player.no_bgm_notes){
            while(BMS_Player.bgm_table_row < BMSInfo.bgm_time_arr.Length){
                if(BMSInfo.bgm_time_arr[BMS_Player.bgm_table_row] <= BMS_Player.playingTimeAsMilliseconds){
                    MainMenu.audioSources[BMSInfo.bgm_num_arr[BMS_Player.bgm_table_row]].Play();
                    BMS_Player.bgm_table_row++;
                }
                else break;
            }
        }
    }
}
