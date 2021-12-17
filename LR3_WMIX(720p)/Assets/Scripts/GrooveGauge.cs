using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrooveGauge : MonoBehaviour {
    public Sprite[] digits;
    List<Transform> gauge_bars;
    enum GaugeType{
        AssistedEasy=10,
        Easy=11,
        Normal=12,
        Off=12,
        Groove=12,
        Hard=13,
        EXHard=14,
        Hazard=15,
        PAttack=16,
        GAttack=17,
        DAN=20,
        EX_DAN=21,
        EXHARD_DAN=22
    }
    enum NoteJudge{
        PG=4,
        GR=3,
        GD=2,
        BD=1,
        PR=0,
        Empty=-1
    }
    GaugeType gaugeType;
	// Use this for initialization
	void Start () {
        gauge_bars = new List<Transform>(50);
        switch (gaugeType){
            case GaugeType.AssistedEasy:
                break;
            case GaugeType.Easy:
                break;
            case GaugeType.Normal:
                break;
            case GaugeType.Hard:
                break;
            case GaugeType.EXHard:
                break;
            case GaugeType.Hazard:
                break;
            case GaugeType.PAttack:
                break;
            case GaugeType.GAttack:
                break;
            default:
                break;
        }
	}
	
	// Update is called once per frame
	void Update () {
	}
}
