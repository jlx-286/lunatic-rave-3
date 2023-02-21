using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class NotePlayer : MonoBehaviour {
    public BMSPlayer BMS_Player;
    // private string str_note = string.Empty;
    public GameObject[] lanes;
    private Dictionary<byte, byte> laneDict = new Dictionary<byte, byte>();
    private enum KeyState : byte{
        Free = 0,
        Down = 1,
        Up = 2,
        Hold = 3
    }
    private KeyState[] laneKeyStates;
    private void Start(){
        laneKeyStates = (KeyState[])ArrayList.Repeat(KeyState.Free, lanes.Length).ToArray(typeof(KeyState));
        switch(BMSInfo.scriptType){
            case BMSInfo.ScriptType.BMS:
                laneDict[0x11] = laneDict[0x51] = 1;// laneDict[0xD1] = 1;
                laneDict[0x12] = laneDict[0x52] = 2;// laneDict[0xD2] = 2;
                laneDict[0x13] = laneDict[0x53] = 3;// laneDict[0xD3] = 3;
                laneDict[0x14] = laneDict[0x54] = 4;// laneDict[0xD4] = 4;
                laneDict[0x15] = laneDict[0x55] = 5;// laneDict[0xD5] = 5;
                laneDict[0x16] = laneDict[0x56] = 0;// laneDict[0xD6] = 0;
                laneDict[0x18] = laneDict[0x58] = 6;// laneDict[0xD8] = 6;
                laneDict[0x19] = laneDict[0x59] = 7;// laneDict[0xD9] = 7;
                laneDict[0x21] = laneDict[0x61] = 9;// laneDict[0xE1] = 9;
                laneDict[0x22] = laneDict[0x62] = 10;// laneDict[0xE2] = 10;
                laneDict[0x23] = laneDict[0x63] = 11;// laneDict[0xE3] = 11;
                laneDict[0x24] = laneDict[0x64] = 12;// laneDict[0xE4] = 12;
                laneDict[0x25] = laneDict[0x65] = 13;// laneDict[0xE5] = 13;
                laneDict[0x26] = laneDict[0x66] = 8;// laneDict[0xE6] = 8;
                laneDict[0x28] = laneDict[0x68] = 14;// laneDict[0xE8] = 14;
                laneDict[0x29] = laneDict[0x69] = 15;// laneDict[0xE9] = 15;
                break;
            case BMSInfo.ScriptType.PMS:
                laneDict[0x11] = laneDict[0x51] = 0;// laneDict[0xD1] = 0;
                laneDict[0x12] = laneDict[0x52] = 1;// laneDict[0xD2] = 1;
                laneDict[0x13] = laneDict[0x53] = 2;// laneDict[0xD3] = 2;
                laneDict[0x14] = laneDict[0x54] = 3;// laneDict[0xD4] = 3;
                laneDict[0x15] = laneDict[0x55] = 4;// laneDict[0xD5] = 4;
                laneDict[0x16] = laneDict[0x56] = 5;// laneDict[0xD6] = 5;
                laneDict[0x17] = laneDict[0x57] = 6;// laneDict[0xD7] = 6;
                laneDict[0x18] = laneDict[0x58] = 7;// laneDict[0xD8] = 7;
                laneDict[0x19] = laneDict[0x59] = 8;// laneDict[0xD9] = 8;
                laneDict[0x22] = laneDict[0x62] = 5;// laneDict[0xE2] = 5;
                laneDict[0x23] = laneDict[0x63] = 6;// laneDict[0xE3] = 6;
                laneDict[0x24] = laneDict[0x64] = 7;// laneDict[0xE4] = 7;
                laneDict[0x25] = laneDict[0x65] = 8;// laneDict[0xE5] = 8;
                break;
        }
    }
	//private void Update(){}
    private void FixedUpdate(){
        if(BMS_Player.escaped) return;
        if(!BMS_Player.no_key_notes){
            while(BMS_Player.row_key < BMSInfo.note_list_table.Count){
                if(BMSInfo.note_list_table[BMS_Player.row_key].time <= BMS_Player.playingTimeAsMilliseconds){
                    MainMenu.audioSources[BMSInfo.note_list_table[BMS_Player.row_key].clipNum].Play();
                    // str_note = BMS_Reader.note_dataTable.Rows[BMS_Player.row_key][0].ToString();
                    // if(laneDict.ContainsKey(str_note)){
                    //     //channel = laneDict[str_note];
                    //     //if(channel >= 0 && channel < 16){
                    //     //}
                    // }//else{ channel = -1; }
                    if(BMS_Player.row_key >= BMSInfo.note_list_table.Count - 10){
                        Debug.Log("near note end");
                    }
                    BMS_Player.row_key++;
                }
                else break;
            }
        }
    }
}
