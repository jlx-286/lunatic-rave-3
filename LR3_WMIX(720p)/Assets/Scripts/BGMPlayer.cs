using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMPlayer : MonoBehaviour {
    private BMSReader BMS_Reader;
    public BMSPlayer BMS_Player;
    // private RectTransform BGM_pos;
    // Use this for initialization
    void Start () {
        BMS_Reader = MainVars.BMSReader;
        //BMS_Player = MainVars.BMSPlayer;
        // BGM_pos = this.gameObject.GetComponent<RectTransform>();
    }
	
	// Update is called once per frame
	//void Update () {}
    private void FixedUpdate(){
        if (BMS_Player.escaped) { return; }
        if (!BMS_Player.no_bgm_notes){
            while(BMS_Player.bgm_table_row < BMS_Reader.bgm_note_table.Rows.Count){
                if(BMS_Reader.bgm_time_arr[BMS_Player.bgm_table_row] - BMS_Player.playing_time < Time.fixedDeltaTime){
                // if ((double)BMS_Reader.bgm_note_table.Rows[BMS_Player.bgm_table_row][0] - BMS_Player.playing_time < Time.fixedDeltaTime){
                    // BMS_Player.currClipNum = (ushort)BMS_Reader.bgm_note_table.Rows[BMS_Player.bgm_table_row][1];
                    BMS_Player.currClipNum = BMS_Reader.bgm_num_arr[BMS_Player.bgm_table_row];
                    MainMenu.audioSources[BMS_Player.currClipNum].Play();
                    BMS_Player.bgm_table_row++;
                }
                else{
                    break;
                }
            }
        }
    }
}
