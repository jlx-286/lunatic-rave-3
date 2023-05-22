using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class MeterViewer : MonoBehaviour{
    public NoteViewer viewer;
    public RectTransform position;
    private LinkedList<Image> lines = new LinkedList<Image>();
    private Image meter_form;
    private ushort m_count = 0;
    private BMSPlayer BMS_Player;
    private const uint ns_per_ms = 1000000u;
    private void Awake(){
        BMS_Player = viewer.notePlayer.BMS_Player;
        meter_form = Instantiate<Image>(MainVars.MeterForm, this.GetComponent<RectTransform>());
        meter_form.rectTransform.sizeDelta = new Vector2(position.rect.width,
            meter_form.rectTransform.sizeDelta.y);
        lines.AddLast(Instantiate<Image>(meter_form, position, false));
        lines.First.Value.rectTransform.Translate(0, -viewer.maskHeight, 0);
        while(m_count <= BMSInfo.max_tracks && BMSInfo.track_end_time_as_ns[m_count] <= viewer.offset){
            lines.AddLast(Instantiate<Image>(meter_form, position, false));
            lines.First.Value.rectTransform.Translate(0,
                (viewer.offset - BMSInfo.track_end_time_as_ns[m_count]) / ns_per_ms
                / (ushort)MainVars.GreenNumber * -viewer.maskHeight, 0);
            m_count++;
        }
    }
    private void FixedUpdate(){
        if(BMS_Player.escaped) return;
        while(m_count <= BMSInfo.max_tracks && BMSInfo.track_end_time_as_ns[m_count] <= BMS_Player.playingTimeAsNanoseconds + viewer.offset){
            lines.AddLast(Instantiate<Image>(meter_form, position, false));
            m_count++;
        }
        foreach(var line in lines)
            line.rectTransform.Translate(0, viewer.yTime * viewer.downSpeed, 0);
        while(lines.First != null && lines.First.Value.rectTransform.anchoredPosition.y < -viewer.maskHeight){
            DestroyImmediate(lines.First.Value.gameObject);
            lines.RemoveFirst();
        }
    }
    private void OnDestroy(){
        lines.Clear();
    }
}
