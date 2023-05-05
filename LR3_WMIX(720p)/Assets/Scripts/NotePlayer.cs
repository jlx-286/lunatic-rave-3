using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public class NotePlayer : MonoBehaviour {
    public BMSPlayer BMS_Player;
    private byte laneNum = byte.MaxValue;
    public RectTransform[] lanes;
    [HideInInspector] public readonly byte[] laneDict =
        Enumerable.Repeat(byte.MaxValue, byte.MaxValue).ToArray();
    private ushort[] clipNums;
    public Text[] judges;
    private ulong[] judge_nums;
    public Text score;
    private ulong score_num = 0;
    public Text combo;
    private ulong combo_nums = 0;
    private bool toUpdateScore = false;
    // public Text[] gauge_texts;
    // private decimal[] gauge_vals;
    public Text gauge_text;
    private decimal gauge_val = 20;
    public HorizontalLayoutGroup gauge_bars;
    private Image[] gaugeBars;
    private decimal inc = 0;
    private byte prev_bars = 10, now_bars = 10;
    private KeyState[] laneKeyStates;
    private void OnEnable(){
        laneKeyStates = Enumerable.Repeat(KeyState.Free, lanes.Length).ToArray();
        clipNums = Enumerable.Repeat(ushort.MaxValue, lanes.Length).ToArray();
        judge_nums = Enumerable.Repeat<ulong>(0, judges.Length).ToArray();
        gaugeBars = gauge_bars.GetComponentsInChildren<Image>(true);
        switch(BMSInfo.scriptType){
            case ScriptType.BMS:
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
            case ScriptType.PMS:
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
                if(BMSInfo.note_list_table[BMS_Player.row_key].time <= BMS_Player.playingTimeAsNanoseconds){
                    laneNum = laneDict[(byte)BMSInfo.note_list_table[BMS_Player.row_key].channel];
                    switch(BMSInfo.note_list_table[BMS_Player.row_key].noteType){
                        case NoteType.Longnote:
                            if(laneKeyStates[laneNum] == KeyState.Free){
                                laneKeyStates[laneNum] = KeyState.Hold;
                                clipNums[laneNum] = BMSInfo.note_list_table[BMS_Player.row_key].clipNum;
                                MainMenu.audioSources[clipNums[laneNum]].Play();
                            }
                            else if(laneKeyStates[laneNum] == KeyState.Hold){
                                laneKeyStates[laneNum] = KeyState.Free;
                                if(clipNums[laneNum] != BMSInfo.note_list_table[BMS_Player.row_key].clipNum){
                                    clipNums[laneNum] = BMSInfo.note_list_table[BMS_Player.row_key].clipNum;
                                    MainMenu.audioSources[clipNums[laneNum]].Play();
                                }
                            }
                            toUpdateScore = true;
                            judge_nums[(byte)NoteJudge.Perfect]++; score_num += 2; combo_nums++;
                            inc += BMSInfo.incr;
                            break;
                        case NoteType.Default:
                            laneKeyStates[laneNum] = KeyState.Free;
                            clipNums[laneNum] = BMSInfo.note_list_table[BMS_Player.row_key].clipNum;
                            MainMenu.audioSources[clipNums[laneNum]].Play();
                            toUpdateScore = true;
                            judge_nums[(byte)NoteJudge.Perfect]++; score_num += 2; combo_nums++;
                            inc += BMSInfo.incr;
                            break;
                        default: break;
                    }
                    if(BMS_Player.row_key >= BMSInfo.note_list_table.Count - 10){
                        Debug.Log("near note end");
                    }
                    BMS_Player.row_key++;
                }
                else break;
            }
            if(toUpdateScore){
                judges[(byte)NoteJudge.Perfect].text = judge_nums[(byte)NoteJudge.Perfect].ToString();
                score.text = score_num.ToString(); combo.text = combo_nums.ToString();
                if((gauge_val < 100 && inc > 0) ||
                    (gauge_val > 0.1m && inc < 0)){
                    gauge_val += inc;
                    if(gauge_val > 100) gauge_val = 100;
                    else if(gauge_val < 0.1m) gauge_val = 0.1m;
                    now_bars = (byte)gauge_val; now_bars /= 2;
                    if(prev_bars != now_bars){
                        if(now_bars > prev_bars){
                            for(byte i = prev_bars; i < now_bars; i++)
                                gaugeBars[i].enabled = true;
                        }else{
                            for(byte i = prev_bars; i > now_bars; i--)
                                gaugeBars[i - 1].enabled = false;
                        }
                        prev_bars = now_bars;
                    }
                    gauge_text.text = gauge_val.GaugeToString();
                }
                toUpdateScore = false;
                inc = 0;
            }
        }
    }
}
