using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class GrooveGauge : MonoBehaviour {
    public RawImage[] digits;
    Transform[] gauge_bars = new Transform[50];
    private GaugeType gaugeType;
    private void Start(){
        switch(gaugeType){
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
    //private void Update(){}
}
