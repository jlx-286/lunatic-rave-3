using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class EffectorNum : FX_switch {
    public Sprite plusImg;
    public Sprite minusImg;
    public Sprite[] numImgs;
    public Sprite zeroImg;
    private Slider slider;
    public GameObject target;
    private Image[] digits;
    private bool isPercent;
    private List<Image> tmp_images;
    [HideInInspector] public enum FXname{
        Distortion = 1,
        HighPassCutoff = 2,
        HighPassRQ = 3,
        LowPassCutoff = 4,
        LowPassRQ = 5,
        EchoDelay = 6,
        EchoDelayRatio = 7,
        EchoWet = 8,
        EchoDry = 9,
        ChorusDry = 10,
        ChorusWet1 = 11,
        ChorusWet2 = 12,
        ChorusWet3 = 13,
        ChorusDelay = 14,
        ChorusRate = 15,
        ChorusDepth = 16,
    };
    FXname fx_name_;
	// Use this for initialization
	void Start () {
        isPercent = true;
        slider = this.GetComponent<Slider>();
        tmp_images = new List<Image>();
        tmp_images.AddRange(target.GetComponentsInChildren<Image>());
        tmp_images.RemoveAt(0);
        digits = tmp_images.ToArray();
        if (slider.minValue <= -1f){
            isPercent = false;
        }else{
            isPercent = true;
        }
        slider.onValueChanged.AddListener((value) => {
            int value_int = (int)value;
            byte value_byte = (byte)value;
            if (isPercent){
                digits[0].sprite = numImgs[value_int / 100];
                //target[0].sprite = numImgs[value_int / 100 - value_int / 1000 * 100];
                digits[1].sprite = numImgs[value_int / 10 - value_int / 100 * 10];
                digits[2].sprite = numImgs[value_int - value_int / 10 * 10];
                //target[2].sprite = numImgs[value_int / 1 - value_int / 10 * 10];
                if (value_int < 100){
                    digits[0].sprite = zeroImg;
                }
                if(value_int < 10){
                    digits[1].sprite = zeroImg;
                }
            } else{
                if(value_int < 0){
                    digits[0].sprite = minusImg;
                }else{
                    digits[0].sprite = plusImg;
                }
                value_int = Math.Abs(value_int);
                digits[1].sprite = numImgs[value_int / 10];
                digits[2].sprite = numImgs[value_int - value_int / 10 * 10];
                if(value_int < 10){
                    digits[1].sprite = zeroImg;
                }
            }
            switch (fx_name){
                case "pitch":
                    MainVars.pitch = (sbyte)value;
                    //Convert.ToSingle(Math.Pow(2d, value / 12d));
                    mixer.SetFloat("pitch", Mathf.Pow(2f, value / 12f));
                    break;
                case "freq":
                    MainVars.freq = (sbyte)value;
                    //Convert.ToSingle(Math.Pow(2d, value / 12d));
                    mixer.SetFloat("freq", Mathf.Pow(2f, value / 12f));
                    break;
                case "m_vol":
                    MainVars.master_vol = value_byte; break;
                case "bgm":
                    MainVars.bgm_vol = value_byte; break;
                case "key":
                    MainVars.key_vol = value_byte; break;
                case "reverb":
                    MainVars.FXs["reverb"]["value"] = value_byte; break;
                case "reverb_m":
                    MainVars.FXs["reverb"]["level"] = value_byte; break;
                case "lowpass":
                    MainVars.FXs["lowpass"]["value"] = value_byte; break;
                case "lowpass_m":
                    MainVars.FXs["lowpass"]["level"] = value_byte; break; ;
                case "hipass":
                    MainVars.FXs["hipass"]["value"] = value_byte; break;
                case "hipass_m":
                    MainVars.FXs["hipass"]["level"] = value_byte; break;
                case "delay":
                    MainVars.FXs["delay"]["value"] = value_byte; break;
                case "delay_m":
                    MainVars.FXs["delay"]["level"] = value_byte; break;
                case "flanger":
                    MainVars.FXs["flanger"]["value"] = value_byte; break;
                case "flanger_m":
                    MainVars.FXs["flanger"]["level"] = value_byte; break;
                case "chorus":
                    MainVars.FXs["chorus"]["value"] = value_byte; break;
                case "chorus_m":
                    MainVars.FXs["chorus"]["level"] = value_byte; break;
                case "dist":
                    MainVars.FXs["dist"]["value"] = value_byte; break;
                case "dist_m":
                    MainVars.FXs["dist"]["level"] = value_byte; break;
                default:break;
            }
        });
        switch (fx_name){
            case "pitch":
                slider.value = MainVars.pitch;
                break;
            case "freq":
                slider.value = MainVars.freq;
                break;
            case "m_vol":
                slider.value = MainVars.master_vol; break;
            case "bgm":
                slider.value = MainVars.bgm_vol; break;
            case "key":
                slider.value = MainVars.key_vol; break;
            case "reverb":
                slider.value = (byte)MainVars.FXs["reverb"]["value"]; break;
            case "reverb_m":
                slider.value = (byte)MainVars.FXs["reverb"]["level"]; break;
            case "lowpass":
                slider.value = (byte)MainVars.FXs["lowpass"]["value"]; break;
            case "lowpass_m":
                slider.value = (byte)MainVars.FXs["lowpass"]["level"]; break; ;
            case "hipass":
                slider.value = (byte)MainVars.FXs["hipass"]["value"]; break;
            case "hipass_m":
                slider.value = (byte)MainVars.FXs["hipass"]["level"]; break;
            case "delay":
                slider.value = (byte)MainVars.FXs["delay"]["value"]; break;
            case "delay_m":
                slider.value = (byte)MainVars.FXs["delay"]["level"]; break;
            case "flanger":
                slider.value = (byte)MainVars.FXs["flanger"]["value"]; break;
            case "flanger_m":
                slider.value = (byte)MainVars.FXs["flanger"]["level"]; break;
            case "chorus":
                slider.value = (byte)MainVars.FXs["chorus"]["value"]; break;
            case "chorus_m":
                slider.value = (byte)MainVars.FXs["chorus"]["level"]; break;
            case "dist":
                slider.value = (byte)MainVars.FXs["dist"]["value"]; break;
            case "dist_m":
                slider.value = (byte)MainVars.FXs["dist"]["level"]; break;
            default: break;
        }
        GC.Collect();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
