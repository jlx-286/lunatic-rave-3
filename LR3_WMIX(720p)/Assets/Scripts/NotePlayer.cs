using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public class NotePlayer : MonoBehaviour {
    public BMSPlayer BMS_Player;
    private int[] note_nums;
    public RectTransform[] lanes;
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
    private void Awake(){
        laneKeyStates = Enumerable.Repeat(KeyState.Free, lanes.Length).ToArray();
        clipNums = Enumerable.Repeat<ushort>(36*36, lanes.Length).ToArray();
        judge_nums = Enumerable.Repeat<ulong>(0, judges.Length).ToArray();
        gaugeBars = gauge_bars.GetComponentsInChildren<Image>(true);
        note_nums = Enumerable.Repeat(0, laneKeyStates.Length).ToArray();
    }
	//private void Update(){}
    private void FixedUpdate(){
        if(BMS_Player.escaped) return;
        if(!BMS_Player.no_key_notes){
            for(int i = 0; i < note_nums.Length; i++){
                while(note_nums[i] < BMSInfo.note_list_lanes[i].Count){
                    if(BMSInfo.note_list_lanes[i][note_nums[i]].time <= BMS_Player.playingTimeAsNanoseconds){
                        switch(BMSInfo.note_list_lanes[i][note_nums[i]].noteType){
                            case NoteType.Longnote:
                                if(laneKeyStates[i] == KeyState.Free){
                                    laneKeyStates[i] = KeyState.Hold;
                                    clipNums[i] = BMSInfo.note_list_lanes[i][note_nums[i]].clipNum;
                                    MainMenu.audioSources[clipNums[i]].Play();
                                }
                                else if(laneKeyStates[i] == KeyState.Hold){
                                    laneKeyStates[i] = KeyState.Free;
                                    if(clipNums[i] != BMSInfo.note_list_lanes[i][note_nums[i]].clipNum){
                                        clipNums[i] = BMSInfo.note_list_lanes[i][note_nums[i]].clipNum;
                                        MainMenu.audioSources[clipNums[i]].Play();
                                    }
                                }
                                toUpdateScore = true;
                                judge_nums[(byte)NoteJudge.Perfect]++; score_num += 2; combo_nums++;
                                inc += BMSInfo.incr;
                                break;
                            case NoteType.Default:
                                laneKeyStates[i] = KeyState.Free;
                                clipNums[i] = BMSInfo.note_list_lanes[i][note_nums[i]].clipNum;
                                MainMenu.audioSources[clipNums[i]].Play();
                                toUpdateScore = true;
                                judge_nums[(byte)NoteJudge.Perfect]++; score_num += 2; combo_nums++;
                                inc += BMSInfo.incr;
                                break;
                            default: break;
                        }
                        note_nums[i]++;
                        BMS_Player.row_key++;
                    }
                    else break;
                }
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
