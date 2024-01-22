using System;
using UnityEngine;
using UnityEngine.UI;

public class BPMPlayer : MonoBehaviour{
    public BMSPlayer BMS_Player;
    public GameObject max;
    public Text max_val;
    public Text now;
    public GameObject min;
    public Text min_val;
    private int bpm_table_row = 0;
    private void Awake(){
        now.text = (BMSInfo.start_bpm * FFmpegVideoPlayer.speedAsDecimal).ToString("G29");
        Debug.Log(now.text);
        if(BMSInfo.min_bpm < BMSInfo.max_bpm){
            min.SetActive(true);
            try{ min_val.text = (BMSInfo.min_bpm * FFmpegVideoPlayer.speedAsDecimal).ToString("G29"); }
            catch(OverflowException){ min_val.text = "Infinity"; }
            max.SetActive(true);
            try{ max_val.text = (BMSInfo.max_bpm * FFmpegVideoPlayer.speedAsDecimal).ToString("G29"); }
            catch(OverflowException){ max_val.text = "Infinity"; }
        }
    }
    private void Update(){
        if(BMS_Player.escaped) return;
        if(BMS_Player.playingTimeAsNanoseconds <= BMSInfo.totalTimeAsNanoseconds){
            while(bpm_table_row < BMSInfo.bpm_list_table.Count &&
                BMSInfo.bpm_list_table[bpm_table_row].time <= BMS_Player.playingTimeAsNanoseconds
            ){
                now.text = BMSInfo.bpm_list_table[bpm_table_row].value;
                bpm_table_row++;
            }
        }
    }
}
