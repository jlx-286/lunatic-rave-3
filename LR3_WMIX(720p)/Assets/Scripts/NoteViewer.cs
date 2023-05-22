using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
public class NoteViewer : MonoBehaviour{
    public NotePlayer notePlayer;
    public RectTransform mask;
    [HideInInspector] public float downSpeed = float.NaN;// height per ms
    private BMSPlayer BMS_Player;
    private Image[] NoteForms;
    private Image[] LNStartForms;
    private Image[] LNEndForms;
    private Image[] LNCenterForms;
    private LinkedList<Image>[] lanes_notes;
    private int[] v_note_ids;
    private KeyState[] laneKeyStates;
    private const uint ns_per_ms = 1000000u;
    [HideInInspector] public ulong offset;
    [HideInInspector] public float maskHeight;
    // private bool no_key_notes = false;
    [HideInInspector] public float yTime;
    private readonly byte[] noteImgsLane = Enumerable.Repeat(byte.MaxValue, byte.MaxValue).ToArray();
    private void Awake(){
        BMS_Player = notePlayer.BMS_Player;
        laneKeyStates = Enumerable.Repeat(KeyState.Free, notePlayer.lanes.Length).ToArray();
        lanes_notes = new LinkedList<Image>[notePlayer.lanes.Length];
        downSpeed = mask.rect.height / MainVars.GreenNumber;
        // MainVars.GreenNumber = 573;
        offset = (ulong)MainVars.GreenNumber * ns_per_ms;
        maskHeight = mask.rect.height + 21;
        yTime = Time.fixedDeltaTime * -1000;
        switch(BMSInfo.scriptType){
            case ScriptType.BMS:
                NoteForms = MainVars.BMSNoteForms;
                LNStartForms = MainVars.BMSLNStartForms;
                LNEndForms = MainVars.BMSLNEndForms;
                LNCenterForms = MainVars.BMSLNCenterForms;
                noteImgsLane[1] = noteImgsLane[3] = noteImgsLane[5] = noteImgsLane[7] = 0;
                noteImgsLane[9] = noteImgsLane[11] = noteImgsLane[13] = noteImgsLane[15] = 0;
                noteImgsLane[2] = noteImgsLane[4] = noteImgsLane[6] = 1;
                noteImgsLane[10] = noteImgsLane[12] = noteImgsLane[14] = 1;
                noteImgsLane[0] = noteImgsLane[8] = 2;
                break;
            case ScriptType.PMS:
                NoteForms = MainVars.PMSNoteForms;
                LNStartForms = MainVars.pMSLNStartForms;
                LNEndForms = MainVars.pMSLNEndForms;
                LNCenterForms = MainVars.pMSLNCenterForms;
                noteImgsLane[0] = noteImgsLane[2] = noteImgsLane[4] =
                noteImgsLane[6] = noteImgsLane[8] = 0;
                noteImgsLane[1] = noteImgsLane[3] = noteImgsLane[5] = noteImgsLane[7] = 1;
                break;
        }
        v_note_ids = Enumerable.Repeat(0, lanes_notes.Length).ToArray();
        for(int i = 0; i < lanes_notes.Length; i++){
            lanes_notes[i] = new LinkedList<Image>();
            while(v_note_ids[i] < BMSInfo.note_list_lanes[i].Count && BMSInfo.note_list_lanes[i][v_note_ids[i]].time <= offset){
                lanes_notes[i].AddLast(Instantiate<Image>(NoteForms[noteImgsLane[i]], notePlayer.lanes[i], false));
                lanes_notes[i].Last.Value.rectTransform.Translate(0,
                    ((offset - BMSInfo.note_list_lanes[i][v_note_ids[i]].time) / offset) * -mask.rect.height, 0);
                v_note_ids[i]++;
            }
        }
    }
    private void OnDestroy(){
        for(int i = 0; i < lanes_notes.Length; i++)
            lanes_notes[i].Clear();
    }
    private void FixedUpdate(){
        if(BMS_Player.escaped) return;
        for(int i = 0; i < lanes_notes.Length; i++){
            while(v_note_ids[i] < BMSInfo.note_list_lanes[i].Count && BMSInfo.note_list_lanes[i][v_note_ids[i]].time <= BMS_Player.playingTimeAsNanoseconds + offset){
                lanes_notes[i].AddLast(Instantiate<Image>(NoteForms[noteImgsLane[i]], notePlayer.lanes[i], false));
                /*switch(BMSInfo.note_list_lanes[i][v_note_ids[i]].noteType){
                    case NoteType.Longnote:
                        if(laneKeyStates[i] == KeyState.Free){
                            laneKeyStates[i] = KeyState.Hold;
                            lanes_notes[i].AddLast(Instantiate<Image>(
                                LNCenterForms[noteImgsLane[i]],
                                notePlayer.lanes[i], false));
                            lanes_notes[i].AddLast(Instantiate<Image>(
                                LNStartForms[noteImgsLane[i]],
                                notePlayer.lanes[i], false));
                        }
                        else if(laneKeyStates[i] == KeyState.Hold){
                            laneKeyStates[i] = KeyState.Free;
                            lanes_notes[i].AddLast(Instantiate<Image>(
                                LNEndForms[noteImgsLane[i]],
                                notePlayer.lanes[i], false));
                        }
                        break;
                    case NoteType.Default:
                        if(laneKeyStates[i] == KeyState.Free){
                            lanes_notes[i].AddLast(Instantiate<Image>(
                                NoteForms[noteImgsLane[i]],
                                notePlayer.lanes[i], false));
                        }
                        else if(laneKeyStates[i] == KeyState.Hold){
                            laneKeyStates[i] = KeyState.Free;
                            lanes_notes[i].AddLast(Instantiate<Image>(
                                LNEndForms[noteImgsLane[i]],
                                notePlayer.lanes[i], false));
                        }
                        break;
                    default: break;
                }*/
                v_note_ids[i]++;
            }
            for(LinkedListNode<Image> note = lanes_notes[i].Last; note != null; note = note.Previous){
                /*if(laneKeyStates[i] == KeyState.Hold && note == lanes_notes[i].Last){
                    note.Value.rectTransform.Translate(0, yTime * downSpeed, 0);
                    note = note.Previous; if(note == null) break;
                    // note.Value.rectTransform.sizeDelta.y += yTime * downSpeed;
                    if(note.Value.rectTransform.sizeDelta.y < maskHeight)
                        note.Value.rectTransform.sizeDelta -= new Vector2(0, yTime * downSpeed);
                    // note.Value.rectTransform.rect.size.Set(note.Value.rectTransform.rect.size.x, note.Value.rectTransform.rect.size.y + yTime * downSpeed);
                    // note.Value.rectTransform.rect.Set(0, 0, note.Value.rectTransform.rect.width, note.Value.rectTransform.rect.height + yTime * downSpeed);
                }
                else */note.Value.rectTransform.Translate(0, yTime * downSpeed, 0);
            }
            while(lanes_notes[i].First != null && lanes_notes[i].First.Value.rectTransform.anchoredPosition.y < -maskHeight){
                DestroyImmediate(lanes_notes[i].First.Value.gameObject);
                lanes_notes[i].RemoveFirst();
            }
        }
    }
}
