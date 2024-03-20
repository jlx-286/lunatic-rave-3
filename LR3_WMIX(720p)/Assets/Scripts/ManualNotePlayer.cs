using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class ManualNotePlayer : MonoBehaviour{
    public KeyLaser keyLaser;
    public BGAPlayer BGA_Player;
    private NotePlayer notePlayer;
    private BMSPlayer BMS_Player;
    private readonly long[] offsets = new long[MainVars.JudgeWindows[BMSInfo.judge_rank].Length];
    private NoteJudge[] noteJudges;
    private byte thisCombo = 0;
    // private NoteJudge noteJudge = NoteJudge.None;
    private bool cbrk = false;
    private ushort[] keyDownClips;//, keyUpClips;
    private Coroutine coroutine;
    [SerializeField] private ulong fast = 0, slow = 0;
    private void Awake(){
        notePlayer = keyLaser.notePlayer;
        BMS_Player = notePlayer.BMS_Player;
        noteJudges = Enumerable.Repeat(NoteJudge.None, notePlayer.lanes.Length).ToArray();
        keyDownClips = Enumerable.Repeat<ushort>(36*36, notePlayer.lanes.Length).ToArray();
        for(int i = 0; i < offsets.Length; i++) offsets[i] = MainVars.JudgeWindows[BMSInfo.judge_rank][i] * 1000000L;
    }
    private void Start(){
        notePlayer.Start();
        keyLaser.enabled = false;
        coroutine = StartCoroutine(MissLayer(1000));
        StopCoroutine(coroutine);
        for(int i = 0; i < BGA_Player.poors.Length; i++)
            BGA_Player.poors[i].SetActive(false);
        for(int i = 0; i < keyLaser.keyLanes.Length; i++)
            if(BMSInfo.noteCounts[keyLaser.keyLanes[i]] > 0)
                keyDownClips[keyLaser.keyLanes[i]] = BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].clipNum;
    }
    private void FixedUpdate(){
        if(BMS_Player.escaped) return;
        if(BMS_Player.playingTimeAsNanoseconds <= BMSInfo.totalTimeAsNanoseconds){
            for(int i = 0; i < keyLaser.keyCodes.Length; i++){
                while(notePlayer.note_nums[keyLaser.keyLanes[i]] < BMSInfo.noteCounts[keyLaser.keyLanes[i]]){
                    if((BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].noteType == NoteType.Default
                        || BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].noteType == NoteType.LongnoteStart
                        || BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].noteType == NoteType.LongnoteEnd) &&
                        BMS_Player.playingTimeAsNanoseconds > BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].time + offsets[3] + MainVars.Latency
                    ){// late poor
                        noteJudges[keyLaser.keyLanes[i]] = NoteJudge.Miss;
                        Debug.Log(NoteJudge.Miss);
                        slow++;
                        notePlayer.maxScore += 2;
                        notePlayer.judge_nums[(byte)NoteJudge.Miss]++;
                        notePlayer.inc += notePlayer.increases[(byte)GaugeType.Groove][(byte)NoteJudge.Miss];
                        notePlayer.toUpdateScore = cbrk = true;
                        notePlayer.note_nums[keyLaser.keyLanes[i]]++;
                    }else if(BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].noteType == NoteType.Landmine &&
                        BMS_Player.playingTimeAsNanoseconds > BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].time + MainVars.Latency
                    ){// mine
                        notePlayer.note_nums[keyLaser.keyLanes[i]]++;
                    }else break;
                }
                // if(!keyLaser.pressed[i] && Input.GetKey(keyLaser.keyCodes[i])){// key down
                if(!keyLaser.pressed[i] && Input.GetKeyDown(keyLaser.keyCodes[i])){// key down
                    if(notePlayer.note_nums[keyLaser.keyLanes[i]] >= BMSInfo.noteCounts[keyLaser.keyLanes[i]]
                        || (BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].noteType == NoteType.Default
                        || BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].noteType == NoteType.LongnoteStart)
                    ){
                        CalcJudge(i);
                        if(noteJudges[keyLaser.keyLanes[i]] != NoteJudge.None){
                            Debug.Log(noteJudges[keyLaser.keyLanes[i]]);
                            switch(noteJudges[keyLaser.keyLanes[i]]){
                                case NoteJudge.Perfect: notePlayer.currScore += 2; thisCombo++; break;
                                case NoteJudge.Great: notePlayer.currScore++; thisCombo++; break;
                                case NoteJudge.Good: thisCombo++; break;
                                case NoteJudge.Bad: cbrk = true; break;
                                case NoteJudge.Miss: cbrk = true; break;
                                // default: break;
                            }
                            notePlayer.judge_nums[(byte)noteJudges[keyLaser.keyLanes[i]]]++;
                            notePlayer.inc += notePlayer.increases[(byte)GaugeType.Groove][(byte)noteJudges[keyLaser.keyLanes[i]]];
                            if(notePlayer.note_nums[keyLaser.keyLanes[i]] < BMSInfo.noteCounts[keyLaser.keyLanes[i]]){
                                keyDownClips[keyLaser.keyLanes[i]] = BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].clipNum;
                                if(noteJudges[keyLaser.keyLanes[i]] >= NoteJudge.Miss && noteJudges[keyLaser.keyLanes[i]] <= NoteJudge.Perfect){
                                    notePlayer.maxScore += 2;
                                    notePlayer.note_nums[keyLaser.keyLanes[i]]++;
                                }
                            }
                            notePlayer.toUpdateScore = true;
                        }
                    }
                    MainMenu.audioSources[keyDownClips[keyLaser.keyLanes[i]]].Play();
                    keyLaser.pressed[i] = keyLaser.keys[i].enabled = true;
                }
                // else if(keyLaser.pressed[i] && !Input.GetKey(keyLaser.keyCodes[i])){// key up
                else if(keyLaser.pressed[i] && Input.GetKeyUp(keyLaser.keyCodes[i])){// key up
                    if(notePlayer.note_nums[keyLaser.keyLanes[i]] < BMSInfo.noteCounts[keyLaser.keyLanes[i]] &&
                        BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].noteType == NoteType.LongnoteEnd
                    ){
                        CalcJudge(i, true);
                        if(noteJudges[keyLaser.keyLanes[i]] != NoteJudge.None){
                            Debug.Log(noteJudges[keyLaser.keyLanes[i]]);
                            switch(noteJudges[keyLaser.keyLanes[i]]){
                                case NoteJudge.Perfect: notePlayer.currScore += 2; thisCombo++; break;
                                case NoteJudge.Great: notePlayer.currScore++; thisCombo++; break;
                                case NoteJudge.Good: thisCombo++; break;
                                case NoteJudge.Bad: cbrk = true; break;
                                case NoteJudge.Miss: cbrk = true; break;
                                // default: break;
                            }
                            notePlayer.judge_nums[(byte)noteJudges[keyLaser.keyLanes[i]]]++;
                            notePlayer.inc += notePlayer.increases[(byte)GaugeType.Groove][(byte)noteJudges[keyLaser.keyLanes[i]]];
                            if(notePlayer.note_nums[keyLaser.keyLanes[i]] < BMSInfo.noteCounts[keyLaser.keyLanes[i]]){
                                // keyUpClip = BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].clipNum;
                                if(noteJudges[keyLaser.keyLanes[i]] >= NoteJudge.Miss && noteJudges[keyLaser.keyLanes[i]] <= NoteJudge.Perfect){
                                    notePlayer.maxScore += 2;
                                    notePlayer.note_nums[keyLaser.keyLanes[i]]++;
                                }
                            }
                            notePlayer.toUpdateScore = true;
                        }
                        // MainMenu.audioSources[keyUpClip].Play();
                    }
                    // Debug.Log($"up:{keyCodes[i]}");
                    keyLaser.pressed[i] = keyLaser.keys[i].enabled = false;
                }
            }
            if(notePlayer.toUpdateScore){
                notePlayer.score.text = notePlayer.currScore.ToString();
                notePlayer.runningCombo += thisCombo;
                if(notePlayer.maxCombo < notePlayer.runningCombo) notePlayer.maxCombo = notePlayer.runningCombo;
                notePlayer.combo.text = notePlayer.maxCombo.ToString();
                for(byte i = (byte)NoteJudge.Bad; i <= (byte)NoteJudge.Perfect; i++){
                    notePlayer.judges[i].text = notePlayer.judge_nums[i].ToString();
                }
                if(noteJudges.Contains(NoteJudge.Perfect)){
                    notePlayer.curr_judge.text = StaticClass.judge_tmp[(byte)NoteJudge.Perfect];
                    notePlayer.comboText.text = notePlayer.builder.ComboNumToTMP(in notePlayer.runningCombo, NoteJudge.Perfect);
                }
                else if(noteJudges.Contains(NoteJudge.Great)){
                    notePlayer.curr_judge.text = StaticClass.judge_tmp[(byte)NoteJudge.Great];
                    notePlayer.comboText.text = notePlayer.builder.ComboNumToTMP(in notePlayer.runningCombo, NoteJudge.Great);
                }
                else if(noteJudges.Contains(NoteJudge.Good)){
                    notePlayer.curr_judge.text = StaticClass.judge_tmp[(byte)NoteJudge.Good];
                    notePlayer.comboText.text = notePlayer.builder.ComboNumToTMP(in notePlayer.runningCombo, NoteJudge.Good);
                }
                else if(noteJudges.Contains(NoteJudge.Bad)){
                    notePlayer.curr_judge.text = StaticClass.judge_tmp[(byte)NoteJudge.Bad];
                    notePlayer.comboText.text = " ";
                    StopCoroutine(coroutine);
                    coroutine = StartCoroutine(MissLayer(1000));
                }
                else if(noteJudges.Any(v => v == NoteJudge.Miss || v == NoteJudge.ExcessivePoor)){
                    notePlayer.judges[(byte)NoteJudge.Miss].text = notePlayer.judges[(byte)NoteJudge.ExcessivePoor].text =
                    (notePlayer.judge_nums[(byte)NoteJudge.Miss] + notePlayer.judge_nums[(byte)NoteJudge.ExcessivePoor]).ToString();
                    notePlayer.curr_judge.text = StaticClass.judge_tmp[(byte)NoteJudge.ExcessivePoor];
                    notePlayer.comboText.text = " ";
                    StopCoroutine(coroutine);
                    coroutine = StartCoroutine(MissLayer(1000));
                }
                for(int i = 0; i < noteJudges.Length; i++) noteJudges[i] = NoteJudge.None;
                if((notePlayer.gauge_val < 100 && notePlayer.inc > 0) ||
                    (notePlayer.gauge_val > 2 && notePlayer.inc < 0)){
                    notePlayer.gauge_val += notePlayer.inc;
                    if(notePlayer.gauge_val > 100) notePlayer.gauge_val = 100;
                    else if(notePlayer.gauge_val < 2) notePlayer.gauge_val = 2;
                    notePlayer.now_bars = (byte)notePlayer.gauge_val; notePlayer.now_bars /= 2;
                    if(notePlayer.prev_bars != notePlayer.now_bars){
                        if(notePlayer.now_bars > notePlayer.prev_bars){
                            for(byte i = notePlayer.prev_bars; i < notePlayer.now_bars; i++)
                                notePlayer.gaugeBars[i].enabled = true;
                        }else{
                            for(byte i = notePlayer.prev_bars; i > notePlayer.now_bars; i--)
                                notePlayer.gaugeBars[i - 1].enabled = false;
                        }
                        notePlayer.prev_bars = notePlayer.now_bars;
                    }
                    notePlayer.gauge_text.text = notePlayer.gauge_val.GaugeToString();
                }
                notePlayer.rateText.text = ((double)(notePlayer.currScore * 100) / notePlayer.maxScore).RateToString();
                notePlayer.toUpdateScore = false;
                if(cbrk){
                    notePlayer.runningCombo = 0;
                    cbrk = false;
                }
                notePlayer.inc = thisCombo = 0;
                StopCoroutine(notePlayer.showJudge);
                notePlayer.showJudge = StartCoroutine(notePlayer.ShowJudge());
            }
        }else{
            for(int i = 0; i < keyLaser.keyCodes.Length; i++){
                // if(!keyLaser.pressed[i] && Input.GetKey(keyLaser.keyCodes[i])){// key down
                if(!keyLaser.pressed[i] && Input.GetKeyDown(keyLaser.keyCodes[i])){// key down
                    MainMenu.audioSources[keyDownClips[keyLaser.keyLanes[i]]].Play();
                    keyLaser.pressed[i] = keyLaser.keys[i].enabled = true;
                }
                // else if(keyLaser.pressed[i] && !Input.GetKey(keyLaser.keyCodes[i])){// key up
                else if(keyLaser.pressed[i] && Input.GetKeyUp(keyLaser.keyCodes[i])){// key up
                    keyLaser.pressed[i] = keyLaser.keys[i].enabled = false;
                }
            }
        }
    }
    private void CalcJudge(int i, bool earlyMiss = false){
        if(BMSInfo.noteCounts[keyLaser.keyLanes[i]] < 1 || notePlayer.note_nums[keyLaser.keyLanes[i]] > BMSInfo.noteCounts[keyLaser.keyLanes[i]]){
            noteJudges[keyLaser.keyLanes[i]] = NoteJudge.None;
        }
        else if(notePlayer.note_nums[keyLaser.keyLanes[i]] == BMSInfo.noteCounts[keyLaser.keyLanes[i]]){
            NoteTimeRow row = BMSInfo.note_list_lanes[keyLaser.keyLanes[i]].Last();
            if(BMS_Player.playingTimeAsNanoseconds <= row.time + offsets[2] + MainVars.Latency && row.noteType != NoteType.Landmine)
                noteJudges[keyLaser.keyLanes[i]] = NoteJudge.ExcessivePoor;
            else{
                noteJudges[keyLaser.keyLanes[i]] = NoteJudge.None;
                notePlayer.note_nums[keyLaser.keyLanes[i]]++;
            }
        }
        else if(BMS_Player.playingTimeAsNanoseconds + offsets[4] < BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].time + MainVars.Latency){
            noteJudges[keyLaser.keyLanes[i]] = earlyMiss ? NoteJudge.Miss : NoteJudge.None;//early
            if(noteJudges[keyLaser.keyLanes[i]] == NoteJudge.None && notePlayer.note_nums[keyLaser.keyLanes[i]] > 0 && BMS_Player.playingTimeAsNanoseconds
                <= BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]] - 1].time + offsets[2] + MainVars.Latency
            ){
                noteJudges[keyLaser.keyLanes[i]] = NoteJudge.ExcessivePoor;
            }else if(noteJudges[keyLaser.keyLanes[i]] == NoteJudge.Miss) fast++;
        }
        else if(BMS_Player.playingTimeAsNanoseconds + offsets[4] >= BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].time + MainVars.Latency
            && BMS_Player.playingTimeAsNanoseconds + offsets[3] < BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].time + MainVars.Latency
        ){
            noteJudges[keyLaser.keyLanes[i]] = earlyMiss ? NoteJudge.Miss : NoteJudge.ExcessivePoor;//early
            if(noteJudges[keyLaser.keyLanes[i]] == NoteJudge.Miss) fast++;
        }
        else if(BMS_Player.playingTimeAsNanoseconds + offsets[3] >= BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].time + MainVars.Latency
            && BMS_Player.playingTimeAsNanoseconds + offsets[2] < BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].time + MainVars.Latency
        ){
            noteJudges[keyLaser.keyLanes[i]] = NoteJudge.Bad;//early
            fast++;
        }
        else if(BMS_Player.playingTimeAsNanoseconds + offsets[2] >= BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].time + MainVars.Latency
            && BMS_Player.playingTimeAsNanoseconds + offsets[1] < BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].time + MainVars.Latency
        ){
            noteJudges[keyLaser.keyLanes[i]] = NoteJudge.Good;//early
            fast++;
        }
        else if(BMS_Player.playingTimeAsNanoseconds + offsets[1] >= BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].time + MainVars.Latency
            && BMS_Player.playingTimeAsNanoseconds + offsets[0] < BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].time + MainVars.Latency
        ){
            noteJudges[keyLaser.keyLanes[i]] = NoteJudge.Great;//early
            fast++;
        }
        else if(BMS_Player.playingTimeAsNanoseconds + offsets[0] >= BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].time + MainVars.Latency
            && BMS_Player.playingTimeAsNanoseconds <= BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].time + offsets[0] + MainVars.Latency
        ){
            noteJudges[keyLaser.keyLanes[i]] = NoteJudge.Perfect;
        }
        else if(BMS_Player.playingTimeAsNanoseconds <= BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].time + offsets[1] + MainVars.Latency
            && BMS_Player.playingTimeAsNanoseconds > BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].time + offsets[0] + MainVars.Latency
        ){
            noteJudges[keyLaser.keyLanes[i]] = NoteJudge.Great;//late
            slow++;
        }
        else if(BMS_Player.playingTimeAsNanoseconds <= BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].time + offsets[2] + MainVars.Latency
            && BMS_Player.playingTimeAsNanoseconds > BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].time + offsets[1] + MainVars.Latency
        ){
            noteJudges[keyLaser.keyLanes[i]] = NoteJudge.Good;//late
            slow++;
        }
        else if(BMS_Player.playingTimeAsNanoseconds <= BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].time + offsets[3] + MainVars.Latency
            && BMS_Player.playingTimeAsNanoseconds > BMSInfo.note_list_lanes[keyLaser.keyLanes[i]][notePlayer.note_nums[keyLaser.keyLanes[i]]].time + offsets[2] + MainVars.Latency
        ){
            noteJudges[keyLaser.keyLanes[i]] = NoteJudge.Bad;//late
            slow++;
        }
    }
    private IEnumerator<WaitForFixedUpdate> MissLayer(ushort ms){
        for(int i = 0; i < BGA_Player.poors.Length; i++)
            BGA_Player.poors[i].SetActive(true);
        for(ushort i = 0; i < ms; i++)
            yield return StaticClass.waitForFixedUpdate;
        for(int i = 0; i < BGA_Player.poors.Length; i++)
            BGA_Player.poors[i].SetActive(false);
    }
}
