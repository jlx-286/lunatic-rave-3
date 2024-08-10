using System;
using UnityEngine;
using UnityEngine.UI;

public class BPMPlayer : MonoBehaviour{
    public BMSPlayer BMS_Player;
    public Canvas max;
    public Text max_val;
    public Text now;
    public Canvas min;
    public Text min_val;
    private ulong bpm_table_row = 0;
    private ushort key;
    private void Awake(){
        now.text = BMSInfo.start_bpm.ToString("G29");
        Debug.Log(now.text);
        if(BMSInfo.min_bpm < BMSInfo.max_bpm){
            min.enabled = true;
            min_val.text = BMSInfo.min_bpm.ToString("G29");
            max.enabled = true;
            max_val.text = BMSInfo.max_bpm.ToString("G29");
        }
    }
    private void Update(){
        if(BMS_Player.escaped) return;
        if(BMS_Player.playingTimeAsNanoseconds <= BMSInfo.totalTimeAsNanoseconds){
            while(bpm_table_row < BMSInfo.bpmCount &&
                BMSInfo.bpm_list_table[bpm_table_row].time <= BMS_Player.playingTimeAsNanoseconds
            ){
                key = BMSInfo.bpm_list_table[bpm_table_row].key;
                if(BMSInfo.exBPMDict.ContainsKey(key)) now.text = BMSInfo.exBPMDict[key];
                else now.text = BMSInfo.hexBPMDict[(key & 0x00FF) - 1];
                bpm_table_row++;
            }
        }
    }
}
