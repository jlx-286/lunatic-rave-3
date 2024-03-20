using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
public class NoteViewer : MonoBehaviour{
    public NotePlayer notePlayer;
    public RectTransform mask;
    [HideInInspector] public float downSpeed = float.NaN;// height per ms
    private BMSPlayer BMS_Player;
    private Texture2D[] NotesTex;
    private Texture2D[] LNsStartTex;
    private Texture2D[] LNsEndTex;
    private Image[] LNCenterForms;
    private LinkedList<Image>[] lanes_notes;
    private ulong[] v_note_ids;
    private bool[] inLN;
    private const uint ns_per_ms = 1000000u;
    [HideInInspector] public long offset;
    [HideInInspector] public int maskHeight;
    // private bool no_key_notes = false;
    [HideInInspector] public short yTime;
    private readonly byte[] noteImgsLane = Enumerable.Repeat(byte.MaxValue, byte.MaxValue).ToArray();
    public RawImage pageForm;
    public RectTransform pagePos;
    private readonly LinkedList<RawImage> pages = new LinkedList<RawImage>();
    private Texture2D pageTexForm, lastPage;
    private short[] pageXOffsets;
    private float pageYOffset;
    private ushort trackNum = 0;
    private int pageWidth;
    private void Awake(){
        BMS_Player = notePlayer.BMS_Player;
        inLN = Enumerable.Repeat(false, notePlayer.lanes.Length).ToArray();
        lanes_notes = new LinkedList<Image>[notePlayer.lanes.Length];
        downSpeed = mask.rect.height / MainVars.GreenNumber;
        // MainVars.GreenNumber = 573;
        offset = (long)MainVars.GreenNumber * ns_per_ms;
        maskHeight = (int)mask.rect.height;
        yTime = (short)(Time.fixedDeltaTime * -1000);
        switch(BMSInfo.scriptType){
            case ScriptType.BMS:
                switch(BMSInfo.playerType){
                    case PlayerType.Keys7: case PlayerType.Keys14:
                        NotesTex = MainVars.BMENotesTex;
                        LNsStartTex = MainVars.BMELNsStartTex;
                        LNsEndTex = MainVars.BMELNsEndTex;
                        LNCenterForms = MainVars.BMELNCenterForms;
                        break;
                    case PlayerType.Keys5: case PlayerType.Keys10:
                        NotesTex = MainVars.BMSNotesTex;
                        LNsStartTex = MainVars.BMSLNsStartTex;
                        LNsEndTex = MainVars.BMSLNsEndTex;
                        LNCenterForms = MainVars.BMSLNCenterForms;
                        break;
                }
                noteImgsLane[1] = noteImgsLane[3] = noteImgsLane[5] = noteImgsLane[7] = 0;
                noteImgsLane[9] = noteImgsLane[11] = noteImgsLane[13] = noteImgsLane[15] = 0;
                noteImgsLane[2] = noteImgsLane[4] = noteImgsLane[6] = 1;
                noteImgsLane[10] = noteImgsLane[12] = noteImgsLane[14] = 1;
                noteImgsLane[0] = noteImgsLane[8] = 2;
                break;
            case ScriptType.PMS:
                NotesTex = MainVars.PMSNotesTex;
                LNsStartTex = MainVars.PMSLNsStartTex;
                LNsEndTex = MainVars.PMSLNsEndTex;
                LNCenterForms = MainVars.PMSLNCenterForms;
                noteImgsLane[0] = noteImgsLane[2] = noteImgsLane[4] =
                noteImgsLane[6] = noteImgsLane[8] = 0;
                noteImgsLane[1] = noteImgsLane[3] = noteImgsLane[5] = noteImgsLane[7] = 1;
                break;
        }
        v_note_ids = Enumerable.Repeat(0UL, lanes_notes.Length).ToArray();
        pageXOffsets = new short[notePlayer.lanes.Length];
        for(int i = 0; i < pageXOffsets.Length; i++)
            pageXOffsets[i] = (short)notePlayer.lanes[i].anchoredPosition.x;
        pageWidth = (int)mask.rect.width;
        pageTexForm = new Texture2D((int)mask.rect.width, (int)
            pageForm.rectTransform.rect.height, TextureFormat.RGBA32,
            false){filterMode = FilterMode.Point};
        NativeArray<byte> arr = pageTexForm.GetRawTextureData<byte>();
        unsafe{StaticClass.memset(arr.GetUnsafePtr(), 0, (IntPtr)arr.Length);}
        pageTexForm.Apply(false);
        lastPage = Instantiate<Texture2D>(pageTexForm);
        lastPage.Apply(false, true);
        pageForm.texture = lastPage;
        pages.AddLast(Instantiate<RawImage>(pageForm, pagePos, true));
        lastPage = Instantiate<Texture2D>(pageTexForm);
        lastPage.Apply(false, true);
        pages.Last.Value.texture = lastPage;
        pages.Last.Value.rectTransform.anchoredPosition -= new Vector2(0, maskHeight);
        while(trackNum < BMSInfo.track_end_time_as_ns.Length && BMSInfo.track_end_time_as_ns[trackNum] < offset){
            pageYOffset = BMSInfo.track_end_time_as_ns[trackNum] * maskHeight / offset;
            Graphics.CopyTexture(MainVars.MeterLine, 0, 0, 0, 0, pageWidth,
                MainVars.MeterLine.height, lastPage, 0, 0, 0, (int)pageYOffset);
            trackNum++;
        }
        for(int i = 0; i < lanes_notes.Length; i++){
            lanes_notes[i] = new LinkedList<Image>();
            while(v_note_ids[i] < BMSInfo.noteCounts[i] && BMSInfo.note_list_lanes[i][v_note_ids[i]].time < offset){
                pageYOffset = BMSInfo.note_list_lanes[i][v_note_ids[i]].time * maskHeight / offset;
                switch(BMSInfo.note_list_lanes[i][v_note_ids[i]].noteType){
                    case NoteType.Default:
                        // inLN[i] = false;
                        Graphics.CopyTexture(NotesTex[noteImgsLane[i]], 0, 0, 0, 0, 
                            NotesTex[noteImgsLane[i]].width, NotesTex[noteImgsLane[i]].height,
                            lastPage, 0, 0, pageXOffsets[i], (int)pageYOffset);
                        break;
                    case NoteType.LongnoteStart:
                        inLN[i] = true;
                        Graphics.CopyTexture(LNsStartTex[noteImgsLane[i]], 0, 0, 0, 0, 
                            LNsStartTex[noteImgsLane[i]].width, LNsStartTex[noteImgsLane[i]].height,
                            lastPage, 0, 0, pageXOffsets[i], (int)pageYOffset);
                        lanes_notes[i].AddLast(Instantiate<Image>(LNCenterForms[noteImgsLane[i]], notePlayer.lanes[i], false));
                        long min = Math.Min(offset, BMSInfo.note_list_lanes[i][v_note_ids[i] + 1].time);
                        lanes_notes[i].Last.Value.rectTransform.anchoredPosition -= new Vector2(0, (offset - min) * maskHeight / offset);
                        lanes_notes[i].Last.Value.rectTransform.sizeDelta = new Vector2(0, (min -
                            BMSInfo.note_list_lanes[i][v_note_ids[i]].time) * maskHeight / offset);
                        break;
                    case NoteType.LongnoteEnd:
                        inLN[i] = false;
                        Graphics.CopyTexture(LNsEndTex[noteImgsLane[i]], 0, 0, 0, 0, 
                            LNsEndTex[noteImgsLane[i]].width, LNsEndTex[noteImgsLane[i]].height,
                            lastPage, 0, 0, pageXOffsets[i], (int)pageYOffset);
                        break;
                    default: break;
                }
                v_note_ids[i]++;
            }
        }
        pages.AddLast(Instantiate<RawImage>(pageForm, pagePos, true));
        lastPage = Instantiate<Texture2D>(pageTexForm);
        lastPage.Apply(false, true);
        pages.Last.Value.texture = lastPage;
        pageYOffset = 0;
    }
    private void OnDestroy(){
        for(int i = 0; i < lanes_notes.Length; i++)
            lanes_notes[i].Clear();
        pages.Clear();
    }
    private void FixedUpdate(){
        if(BMS_Player.escaped) return;
        if(BMS_Player.playingTimeAsNanoseconds <= BMSInfo.totalTimeAsNanoseconds){
            for(LinkedListNode<RawImage> item = pages.First; item != null; item = item.Next){
                item.Value.rectTransform.anchoredPosition += new Vector2(0, yTime * downSpeed);
                if(item == pages.Last){
                    pageYOffset -= downSpeed * yTime;
                    while(trackNum < BMSInfo.track_end_time_as_ns.Length &&
                        BMSInfo.track_end_time_as_ns[trackNum] < BMS_Player.playingTimeAsNanoseconds + offset
                    ){
                        Graphics.CopyTexture(MainVars.MeterLine, 0, 0, 0, 0, pageWidth,
                            MainVars.MeterLine.height, lastPage, 0, 0, 0, (int)pageYOffset);
                        trackNum++;
                    }
                    for(int i = 0; i < lanes_notes.Length; i++){
                        while(v_note_ids[i] < BMSInfo.noteCounts[i] &&
                            BMSInfo.note_list_lanes[i][v_note_ids[i]].time < BMS_Player.playingTimeAsNanoseconds + offset
                        ){
                            switch(BMSInfo.note_list_lanes[i][v_note_ids[i]].noteType){
                                case NoteType.LongnoteStart:
                                    inLN[i] = true;
                                    lanes_notes[i].AddLast(Instantiate<Image>(
                                        LNCenterForms[noteImgsLane[i]],
                                        notePlayer.lanes[i], false));
                                    Graphics.CopyTexture(LNsStartTex[noteImgsLane[i]], 0, 0, 0, 0, 
                                        LNsStartTex[noteImgsLane[i]].width, LNsStartTex[noteImgsLane[i]].height,
                                        lastPage, 0, 0, pageXOffsets[i], (int)pageYOffset);
                                    break;
                                case NoteType.LongnoteEnd:
                                    inLN[i] = false;
                                    Graphics.CopyTexture(LNsEndTex[noteImgsLane[i]], 0, 0, 0, 0, 
                                        LNsEndTex[noteImgsLane[i]].width, LNsEndTex[noteImgsLane[i]].height,
                                        lastPage, 0, 0, pageXOffsets[i], (int)pageYOffset);
                                    break;
                                case NoteType.Default:
                                    // inLN[i] = false;
                                    Graphics.CopyTexture(NotesTex[noteImgsLane[i]], 0, 0, 0, 0, 
                                        NotesTex[noteImgsLane[i]].width, NotesTex[noteImgsLane[i]].height,
                                        lastPage, 0, 0, pageXOffsets[i], (int)pageYOffset);
                                    break;
                                default: break;
                            }
                            v_note_ids[i]++;
                        }
                        for(LinkedListNode<Image> note = lanes_notes[i].First; note != null; note = note.Next){
                            if(inLN[i] && note == lanes_notes[i].Last){
                                // note.Value.rectTransform.sizeDelta.y += yTime * downSpeed;
                                if(note.Value.rectTransform.sizeDelta.y < maskHeight)
                                    note.Value.rectTransform.sizeDelta -= new Vector2(0, yTime * downSpeed);
                                // note.Value.rectTransform.rect.size.Set(note.Value.rectTransform.rect.size.x, note.Value.rectTransform.rect.size.y + yTime * downSpeed);
                                // note.Value.rectTransform.rect.Set(0, 0, note.Value.rectTransform.rect.width, note.Value.rectTransform.rect.height + yTime * downSpeed);
                            }
                            else note.Value.rectTransform.anchoredPosition += new Vector2(0, yTime * downSpeed);
                        }
                        while(lanes_notes[i].First != null && lanes_notes[i].First.Value.rectTransform.anchoredPosition.y < -maskHeight){
                            DestroyImmediate(lanes_notes[i].First.Value.gameObject, true);
                            lanes_notes[i].RemoveFirst();
                        }
                    }
                }
            }
            if(pages.First != null && pages.First.Value.rectTransform.anchoredPosition.y < -maskHeight * 2){
                DestroyImmediate(pages.First.Value.gameObject, true);
                pages.RemoveFirst();
                pages.AddLast(Instantiate<RawImage>(pageForm, pagePos, true));
                lastPage = Instantiate<Texture2D>(pageTexForm);
                lastPage.Apply(false, true);
                pages.Last.Value.texture = lastPage;
                pageYOffset = 0;
            }
        }
    }
}
