using System;
using UnityEngine;
public class BGMPlayer : MonoBehaviour {
    public BMSPlayer BMS_Player;
    private int bgm_table_row = 0;
    private void FixedUpdate(){
        if(BMS_Player.escaped) return;
        if(BMS_Player.playingTimeAsNanoseconds <= BMSInfo.totalTimeAsNanoseconds){
            while(bgm_table_row < BMSInfo.bgm_list_table.Count &&
                BMSInfo.bgm_list_table[bgm_table_row].time <= BMS_Player.playingTimeAsNanoseconds
            ){
                MainMenu.audioSources[BMSInfo.bgm_list_table[bgm_table_row].clipNum].Play();
                bgm_table_row++;
            }
        }
    }
}
