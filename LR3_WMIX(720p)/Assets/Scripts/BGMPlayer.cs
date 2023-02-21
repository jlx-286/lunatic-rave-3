using System;
using UnityEngine;
public class BGMPlayer : MonoBehaviour {
    public BMSPlayer BMS_Player;
    //private void Start(){}
	//private void Update(){}
    private void FixedUpdate(){
        if(BMS_Player.escaped) return;
        if(!BMS_Player.no_bgm_notes){
            while(BMS_Player.bgm_table_row < BMSInfo.bgm_list_table.Count){
                if(BMSInfo.bgm_list_table[BMS_Player.bgm_table_row].time <= BMS_Player.playingTimeAsMilliseconds){
                    MainMenu.audioSources[BMSInfo.bgm_list_table[BMS_Player.bgm_table_row].clipNum].Play();
                    BMS_Player.bgm_table_row++;
                }
                else break;
            }
        }
    }
}
