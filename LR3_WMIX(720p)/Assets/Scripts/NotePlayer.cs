﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NotePlayer : MonoBehaviour {
    private BMSReader BMS_Reader;
    public BMSPlayer BMS_Player;
    private string str_note = string.Empty;
    public GameObject[] lanes;
    private Dictionary<string, sbyte> laneDict;
    private enum KeyState{
        Free = 0,
        Down = 1,
        Up = 2,
        Hold = 3
    }
    private KeyState[] laneKeyStates;
    // Use this for initialization
    void Start () {
        BMS_Reader = MainVars.BMSReader;
        BMS_Reader.row_key = 0;
        //BMS_Player = MainVars.BMSPlayer;
        laneKeyStates = new KeyState[lanes.Length];
        ArrayList.Repeat(KeyState.Free, laneKeyStates.Length).CopyTo(laneKeyStates);
        laneDict = new Dictionary<string, sbyte>();
        switch (BMS_Reader.scriptType){
            case BMSReader.ScriptType.BMS:
                laneDict["11"] = laneDict["51"] = 1;// laneDict["D1"] = 1;
                laneDict["12"] = laneDict["52"] = 2;// laneDict["D2"] = 2;
                laneDict["13"] = laneDict["53"] = 3;// laneDict["D3"] = 3;
                laneDict["14"] = laneDict["54"] = 4;// laneDict["D4"] = 4;
                laneDict["15"] = laneDict["55"] = 5;// laneDict["D5"] = 5;
                laneDict["16"] = laneDict["56"] = 0;// laneDict["D6"] = 0;
                laneDict["18"] = laneDict["58"] = 6;// laneDict["D8"] = 6;
                laneDict["19"] = laneDict["59"] = 7;// laneDict["D9"] = 7;
                laneDict["21"] = laneDict["61"] = 9;// laneDict["E1"] = 9;
                laneDict["22"] = laneDict["62"] = 10;// laneDict["E2"] = 10;
                laneDict["23"] = laneDict["63"] = 11;// laneDict["E3"] = 11;
                laneDict["24"] = laneDict["64"] = 12;// laneDict["E4"] = 12;
                laneDict["25"] = laneDict["65"] = 13;// laneDict["E5"] = 13;
                laneDict["26"] = laneDict["66"] = 8;// laneDict["E6"] = 8;
                laneDict["28"] = laneDict["68"] = 14;// laneDict["E8"] = 14;
                laneDict["29"] = laneDict["69"] = 15;// laneDict["E9"] = 15;
                break;
            case BMSReader.ScriptType.PMS:
                laneDict["11"] = laneDict["51"] = 0;// laneDict["D1"] = 0;
                laneDict["12"] = laneDict["52"] = 1;// laneDict["D2"] = 1;
                laneDict["13"] = laneDict["53"] = 2;// laneDict["D3"] = 2;
                laneDict["14"] = laneDict["54"] = 3;// laneDict["D4"] = 3;
                laneDict["15"] = laneDict["55"] = 4;// laneDict["D5"] = 4;
                laneDict["16"] = laneDict["56"] = 5;// laneDict["D6"] = 5;
                laneDict["17"] = laneDict["57"] = 6;// laneDict["D7"] = 6;
                laneDict["18"] = laneDict["58"] = 7;// laneDict["D8"] = 7;
                laneDict["19"] = laneDict["59"] = 8;// laneDict["D9"] = 8;
                laneDict["22"] = laneDict["62"] = 5;// laneDict["E2"] = 5;
                laneDict["23"] = laneDict["63"] = 6;// laneDict["E3"] = 6;
                laneDict["24"] = laneDict["64"] = 7;// laneDict["E4"] = 7;
                laneDict["25"] = laneDict["65"] = 8;// laneDict["E5"] = 8;
                break;
        }
    }
	
	// Update is called once per frame
	//void Update () {}
    private void FixedUpdate(){
        if (BMS_Player.escaped) { return; }
        if (!BMS_Player.no_key_notes){
            while(BMS_Reader.row_key < BMS_Reader.note_dataTable.Rows.Count){
                if ((double)BMS_Reader.note_dataTable.Rows[BMS_Reader.row_key][1] - BMS_Player.playing_time < Time.fixedDeltaTime
                    && (double)BMS_Reader.note_dataTable.Rows[BMS_Reader.row_key][1] - BMS_Player.playing_time > -double.Epsilon
                ){
                    //Debug.Log("while?");
                    str_note = BMS_Reader.note_dataTable.Rows[BMS_Reader.row_key][0].ToString();
                    if (laneDict.ContainsKey(str_note)){
                        //channel = laneDict[str_note];
                        //if (channel >= 0 && channel < 16){
                        BMS_Player.currClipNum = (ushort)BMS_Reader.note_dataTable.Rows[BMS_Reader.row_key][2];
                        if (BMS_Player.totalSrcs.ContainsKey(BMS_Player.currClipNum)){
                            DestroyImmediate(BMS_Player.totalSrcs[BMS_Player.currClipNum].gameObject);
                            BMS_Player.totalSrcs.Remove(BMS_Player.currClipNum);
                        }
                        BMS_Player.currSrc = Instantiate(BMS_Player.audioSourceForm, lanes[laneDict[str_note]].GetComponent<RectTransform>());
                        BMS_Player.currSrc.clip = BMS_Reader.audioClips[BMS_Player.currClipNum];
                        BMS_Player.totalSrcs.Add(BMS_Player.currClipNum, BMS_Player.currSrc);
                        DelAudio delAudio = BMS_Player.currSrc.GetComponent<DelAudio>();
                        delAudio.clipNum = BMS_Player.currClipNum;
                        delAudio.hasClip = true;
                        //}
                    }//else { channel = -1; }
                    if (BMS_Reader.row_key >= BMS_Reader.note_dataTable.Rows.Count - 10){
                        Debug.Log("near note end");
                    }
                    BMS_Reader.row_key++;
                }
                else{
                    break;
                }
            }
        }
    }
}