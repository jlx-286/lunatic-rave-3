using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
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
    public Canvas judgeCanvas;
    public TMP_Text curr_judge;
    public TMP_Text comboText;
    private Coroutine showJudge;
    public Text rateText;
    private readonly StringBuilder builder = new StringBuilder();
    // private bool[] inLN;
    private void Awake(){
        // inLN = Enumerable.Repeat(false, lanes.Length).ToArray();
        clipNums = Enumerable.Repeat<ushort>(36*36, lanes.Length).ToArray();
        judge_nums = Enumerable.Repeat<ulong>(0, judges.Length).ToArray();
        gaugeBars = gauge_bars.GetComponentsInChildren<Image>(true);
        note_nums = Enumerable.Repeat(0, lanes.Length).ToArray();
    }
    private void Start(){
        showJudge = StartCoroutine(ShowJudge());
        StopCoroutine(showJudge);
        judgeCanvas.enabled = false;
    }
	//private void Update(){}
    private void FixedUpdate(){
        if(BMS_Player.escaped) return;
        if(!BMS_Player.no_key_notes){
            for(int i = 0; i < note_nums.Length; i++){
                while(note_nums[i] < BMSInfo.note_list_lanes[i].Count){
                    if(BMSInfo.note_list_lanes[i][note_nums[i]].time <= BMS_Player.playingTimeAsNanoseconds){
                        switch(BMSInfo.note_list_lanes[i][note_nums[i]].noteType){
                            case NoteType.LongnoteStart:
                                // inLN[i] = true;
                                clipNums[i] = BMSInfo.note_list_lanes[i][note_nums[i]].clipNum;
                                MainMenu.audioSources[clipNums[i]].Play();
                                toUpdateScore = true;
                                judge_nums[(byte)NoteJudge.Perfect]++; score_num += 2; combo_nums++;
                                inc += BMSInfo.incr;
                                break;
                            case NoteType.LongnoteEnd:
                                // inLN[i] = false;
                                if(clipNums[i] != BMSInfo.note_list_lanes[i][note_nums[i]].clipNum){
                                    MainMenu.audioSources[BMSInfo.note_list_lanes[i][note_nums[i]].clipNum].Play();
                                    // clipNums[i] = BMSInfo.note_list_lanes[i][note_nums[i]].clipNum;
                                }
                                toUpdateScore = true;
                                judge_nums[(byte)NoteJudge.Perfect]++; score_num += 2; combo_nums++;
                                inc += BMSInfo.incr;
                                break;
                            case NoteType.Default:
                                // inLN[i] = false;
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
                comboText.text = builder.ComboNumToTMP(in combo_nums, NoteJudge.Perfect);
                curr_judge.text = StaticClass.judge_tmp[(byte)NoteJudge.Perfect];
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
                rateText.text = "100.00";
                toUpdateScore = false;
                inc = 0;
                StopCoroutine(showJudge);
                showJudge = StartCoroutine(ShowJudge());
            }
        }
    }
    private IEnumerator<WaitForFixedUpdate> ShowJudge(){
        judgeCanvas.enabled = true;
        for(ushort i = 0; i < 500u; i++)
            yield return StaticClass.waitForFixedUpdate;
        judgeCanvas.enabled = false;
    }
}
