﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrooveGauge : MonoBehaviour {
    public Sprite[] digits;
    List<Transform> gauge_bars;
    enum GaugeType{
        AssistedEasy = 10,
        Easy = 11,
        Normal = 12,
        Off = 12,
        Groove = 12,
        Hard = 13,
        EXHard = 14,
        Hazard = 15,
        PAttack = 16,
        GAttack = 17,
        Grade = 20,
        EX_Grade = 21,
        EXHARD_Grade = 22
    }
    enum NoteJudge{
        HCN = 8,
        Landmine = 7,
        ExcessivePoor = 6,
        空Poor = 6,
        空P = 6,
        Perfect = 5,
        PG = 5,
        PGreat = 5,
        GR = 4,
        Great = 4,
        GD = 3,
        Good = 3,
        BD = 2,
        Bad = 2,
        PR = 1,
        Poor = 1,
    }
    GaugeType gaugeType;
	// Use this for initialization
	void Start () {
        gauge_bars  =  new List<Transform>(50);
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