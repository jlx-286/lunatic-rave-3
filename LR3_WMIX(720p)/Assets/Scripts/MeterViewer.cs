using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class MeterViewer : MonoBehaviour{
    public NoteViewer viewer;
    public RectTransform[] positions;
    private LinkedList<Image>[] lines;
    private ushort m_count = 0;
    private BMSPlayer BMS_Player;
    private const uint ns_per_ms = 1000000u;
    private void OnEnable(){
        BMS_Player = viewer.notePlayer.BMS_Player;
        lines = new LinkedList<Image>[positions.Length];
        for(int i = 0; i < lines.Length; i++)
            lines[i] = new LinkedList<Image>();
        for(int i = 0; i < lines.Length; i++){
            lines[i].AddLast(Instantiate<Image>(MainVars.MeterForm, positions[i], false));
            lines[i].First.Value.rectTransform.Translate(0, -viewer.maskHeight, 0);
        }
        while(m_count <= BMSInfo.max_tracks && BMSInfo.track_end_time_as_ns[m_count] <= viewer.offset){
            for(int i = 0; i < lines.Length; i++){
                lines[i].AddLast(Instantiate<Image>(MainVars.MeterForm, positions[i], false));
                lines[i].First.Value.rectTransform.Translate(0,
                    (viewer.offset - BMSInfo.track_end_time_as_ns[m_count]) / ns_per_ms / (ushort)MainVars.GreenNumber * -viewer.maskHeight, 0);
            }
            m_count++;
        }
    }
    private void FixedUpdate(){
        if(BMS_Player.escaped) return;
        while(m_count <= BMSInfo.max_tracks && BMSInfo.track_end_time_as_ns[m_count] <= BMS_Player.playingTimeAsNanoseconds + viewer.offset){
            for(int i = 0; i < lines.Length; i++)
                lines[i].AddLast(Instantiate<Image>(MainVars.MeterForm, positions[i], false));
            m_count++;
        }
        for(int i = 0; i < lines.Length; i++){
            foreach(var line in lines[i])
                line.rectTransform.Translate(0, viewer.yTime * viewer.downSpeed, 0);
            while(lines[i].First != null && lines[i].First.Value.rectTransform.anchoredPosition.y < -viewer.maskHeight){
                DestroyImmediate(lines[i].First.Value.gameObject);
                lines[i].RemoveFirst();
            }
        }
    }
}
