using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class NotePlayer : MonoBehaviour {
    public BMSPlayer BMS_Player;
    [HideInInspector] public int[] note_nums;
    public RectTransform[] lanes;
    private ushort[] clipNums;
    public Text[] judges;
    [HideInInspector] public ulong[] judge_nums;
    public Text score;
    [HideInInspector] public ulong currScore = 0, maxScore = 0;
    public Text combo;
    [HideInInspector] public ulong runningCombo = 0, maxCombo = 0;
    [HideInInspector] public bool toUpdateScore = false;
    // public Text[] gauge_texts;
    // private decimal[] gauge_vals;
    public Text gauge_text;
    [HideInInspector] public decimal gauge_val = 20;
    public HorizontalLayoutGroup gauge_bars;
    [HideInInspector] public Image[] gaugeBars;
    [HideInInspector] public decimal inc = 0;
    [HideInInspector] public byte prev_bars = 10, now_bars = 10;
    public Canvas judgeCanvas;
    public TMP_Text curr_judge;
    public TMP_Text comboText;
    [HideInInspector] public Coroutine showJudge;
    public Text rateText;
    [HideInInspector] public readonly StringBuilder builder = new StringBuilder();
    [HideInInspector] public decimal[][] increases;
    private decimal hardGaugeMul;
    // private ulong row_key = 0;
    // private bool[] inLN;
    private void Awake(){
        // inLN = Enumerable.Repeat(false, lanes.Length).ToArray();
        clipNums = Enumerable.Repeat<ushort>(36*36, lanes.Length).ToArray();
        judge_nums = Enumerable.Repeat<ulong>(0, judges.Length).ToArray();
        gaugeBars = gauge_bars.GetComponentsInChildren<Image>(true);
        note_nums = Enumerable.Repeat(0, BMSInfo.note_list_lanes.Length).ToArray();
        if(BMSInfo.note_count <= 20) hardGaugeMul = 10;
        else if(BMSInfo.note_count < 30) hardGaugeMul = 8 + (30 - BMSInfo.note_count) / 5m;
        else if(BMSInfo.note_count < 60) hardGaugeMul = 5 + (60 - BMSInfo.note_count) / 15m;
        else if(BMSInfo.note_count < 125) hardGaugeMul = 4 + (125 - BMSInfo.note_count) / 65m;
        else if(BMSInfo.note_count < 250) hardGaugeMul = 3 + (250 - BMSInfo.note_count) / 125m;
        else if(BMSInfo.note_count < 500) hardGaugeMul = 2 + (500 - BMSInfo.note_count) / 250m;
        else if(BMSInfo.note_count < 1000) hardGaugeMul = 1 + (1000 - BMSInfo.note_count) / 500m;
        else hardGaugeMul = 1;
        if(BMSInfo.total >= 240) hardGaugeMul = Math.Max(hardGaugeMul, 1);
        else if(BMSInfo.total <= 96) hardGaugeMul = Math.Max(hardGaugeMul, 10);
        else hardGaugeMul = Math.Max(hardGaugeMul, 10 / Math.Min(10, Math.Max(1, Math.Floor(BMSInfo.total / 16) - 5)));
        increases = new decimal[12][]{
            //------------{               EP,              Miss,                BD,                 GD,                 GR,                 PG}
            new decimal[6]{            -1.6m,             -4.8m,             -3.2m,0.6m * BMSInfo.incr,1.2m * BMSInfo.incr,1.2m * BMSInfo.incr},// assist
            new decimal[6]{            -1.6m,             -4.8m,             -3.2m,0.6m * BMSInfo.incr,1.2m * BMSInfo.incr,1.2m * BMSInfo.incr},// easy
            new decimal[6]{               -2,                -6,                -4,0.5m * BMSInfo.incr,       BMSInfo.incr,       BMSInfo.incr},// normal
            new decimal[6]{-2 * hardGaugeMul,-10 * hardGaugeMul, -6 * hardGaugeMul,              0.05m,               0.1m,               0.1m},// hard (32% halve)
            new decimal[6]{-4 * hardGaugeMul,-20 * hardGaugeMul,-12 * hardGaugeMul,              0.05m,               0.1m,               0.1m},// exh
            new decimal[6]{                0,              -100,              -100,                 +0,                 +0,                 +0},// FC
            new decimal[6]{             -100,              -100,              -100,               -100,  -1 * hardGaugeMul,               0.1m},// PA (gr>=-1)
            new decimal[6]{               -2,                -3,                -2,              0.05m,               0.1m,               0.1m},// normal-Grade (32% halve)
            new decimal[6]{               -4,                -6,                -4,              0.05m,               0.1m,               0.1m},// hard-Grade
            new decimal[6]{               -6,               -10,                -6,              0.05m,               0.1m,               0.1m},// exh-Grade
            new decimal[6]{            -1.6m,             -2.4m,             -1.6m,              0.06m,              0.12m,              0.12m},// course (32% halve)
            new decimal[6]{-2 * hardGaugeMul,-10 * hardGaugeMul, -6 * hardGaugeMul,               0.1m,  -6 * hardGaugeMul, -10 * hardGaugeMul},// G-A
        };
    }
    public void Start(){
        showJudge = StartCoroutine(ShowJudge());
        StopCoroutine(showJudge);
        judgeCanvas.enabled = false;
    }
    //private void Update(){}
    private void FixedUpdate(){
        if(BMS_Player.escaped) return;
        if(BMS_Player.playingTimeAsNanoseconds <= BMSInfo.totalTimeAsNanoseconds){
            for(int i = 0; i < note_nums.Length; i++){
                while(note_nums[i] < BMSInfo.note_list_lanes[i].Count &&
                    BMSInfo.note_list_lanes[i][note_nums[i]].time <= BMS_Player.playingTimeAsNanoseconds
                ){
                    switch(BMSInfo.note_list_lanes[i][note_nums[i]].noteType){
                        case NoteType.LongnoteStart:
                            // inLN[i] = true;
                            clipNums[i] = BMSInfo.note_list_lanes[i][note_nums[i]].clipNum;
                            MainMenu.audioSources[clipNums[i]].Play();
                            toUpdateScore = true;
                            judge_nums[(byte)NoteJudge.Perfect]++;
                            currScore += 2; maxScore += 2;
                            runningCombo++; maxCombo++;
                            inc += BMSInfo.incr;
                            break;
                        case NoteType.LongnoteEnd:
                            // inLN[i] = false;
                            if(clipNums[i] != BMSInfo.note_list_lanes[i][note_nums[i]].clipNum){
                                MainMenu.audioSources[BMSInfo.note_list_lanes[i][note_nums[i]].clipNum].Play();
                                // clipNums[i] = BMSInfo.note_list_lanes[i][note_nums[i]].clipNum;
                            }
                            toUpdateScore = true;
                            judge_nums[(byte)NoteJudge.Perfect]++;
                            currScore += 2; maxScore += 2;
                            runningCombo++; maxCombo++;
                            inc += BMSInfo.incr;
                            break;
                        case NoteType.Default:
                            // inLN[i] = false;
                            clipNums[i] = BMSInfo.note_list_lanes[i][note_nums[i]].clipNum;
                            MainMenu.audioSources[clipNums[i]].Play();
                            toUpdateScore = true;
                            judge_nums[(byte)NoteJudge.Perfect]++;
                            currScore += 2; maxScore += 2;
                            runningCombo++; maxCombo++;
                            inc += BMSInfo.incr;
                            break;
                        default: break;
                    }
                    note_nums[i]++;
                    // row_key++;
                }
            }
            if(toUpdateScore){
                judges[(byte)NoteJudge.Perfect].text = judge_nums[(byte)NoteJudge.Perfect].ToString();
                score.text = currScore.ToString(); combo.text = maxCombo.ToString();
                comboText.text = builder.ComboNumToTMP(in runningCombo, NoteJudge.Perfect);
                curr_judge.text = StaticClass.judge_tmp[(byte)NoteJudge.Perfect];
                if((gauge_val < 100 && inc > 0) ||
                    (gauge_val > 2 && inc < 0)){
                    gauge_val += inc;
                    if(gauge_val > 100) gauge_val = 100;
                    else if(gauge_val < 2) gauge_val = 2;
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
    private void OnDestroy(){
        if(builder != null)
            builder.Clear();
        StopAllCoroutines();
    }
    public IEnumerator<WaitForFixedUpdate> ShowJudge(){
        judgeCanvas.enabled = true;
        for(ushort i = 0; i < 500u; i++)
            yield return StaticClass.waitForFixedUpdate;
        judgeCanvas.enabled = false;
    }
}
