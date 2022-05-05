using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BGMPlayer : MonoBehaviour {
    private BMSReader BMS_Reader;
    public BMSPlayer BMS_Player;
    private RectTransform BGM_pos;
    // Use this for initialization
    void Start () {
        BMS_Reader = MainVars.BMSReader;
        //BMS_Player = MainVars.BMSPlayer;
        BGM_pos = this.gameObject.GetComponent<RectTransform>();
    }
	
	// Update is called once per frame
	//void Update () {}
    private void FixedUpdate(){
        if (BMS_Player.escaped) { return; }
        if (!BMS_Player.no_bgm_notes){
            while(BMS_Player.bgm_table_row < BMS_Reader.bgm_note_table.Rows.Count){
                if ((double)BMS_Reader.bgm_note_table.Rows[BMS_Player.bgm_table_row][0] - BMS_Player.playing_time < Time.fixedDeltaTime){
                    BMS_Player.currClipNum = (ushort)BMS_Reader.bgm_note_table.Rows[BMS_Player.bgm_table_row][1];
                    if (BMS_Player.totalSrcs.ContainsKey(BMS_Player.currClipNum)){
                        DestroyImmediate(BMS_Player.totalSrcs[BMS_Player.currClipNum].gameObject);
                        BMS_Player.totalSrcs.Remove(BMS_Player.currClipNum);
                    }
                    BMS_Player.currSrc = Instantiate(BMS_Player.audioSourceForm, BGM_pos);
                    BMS_Player.currSrc.clip = BMS_Reader.audioClips[BMS_Player.currClipNum];
                    BMS_Player.totalSrcs.Add(BMS_Player.currClipNum, BMS_Player.currSrc);
                    DelAudio delAudio = BMS_Player.currSrc.GetComponent<DelAudio>();
                    delAudio.clipNum = BMS_Player.currClipNum;
                    delAudio.hasClip = true;
                    BMS_Player.bgm_table_row++;
                }
                else{
                    break;
                }
            }
        }
    }
}
