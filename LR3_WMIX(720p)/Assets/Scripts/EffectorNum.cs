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
            int val = (int)value;
            if (isPercent){
                digits[0].sprite = numImgs[val / 100];
                //target[0].sprite = numImgs[val / 100 - val / 1000 * 100];
                digits[1].sprite = numImgs[val / 10 - val / 100 * 10];
                digits[2].sprite = numImgs[val - val / 10 * 10];
                //target[2].sprite = numImgs[val / 1 - val / 10 * 10];
                if(val < 100){
                    digits[0].sprite = zeroImg;
                }
                if(val < 10){
                    digits[1].sprite = zeroImg;
                }
            } else{
                if(val < 0){
                    digits[0].sprite = minusImg;
                }else{
                    digits[0].sprite = plusImg;
                }
                val = Math.Abs(val);
                digits[1].sprite = numImgs[val / 10];
                digits[2].sprite = numImgs[val - val / 10 * 10];
                if(val < 10){
                    digits[1].sprite = zeroImg;
                }
            }
            switch (fx_name){
                case "pitch":
                    pitch = (sbyte)value;
                    //Convert.ToSingle(Math.Pow(2d, value / 12d));
                    mixer.SetFloat("pitch", Mathf.Pow(2f, value / 12f));
                    break;
                case "freq":
                    freq = (sbyte)value;
                    //Convert.ToSingle(Math.Pow(2d, value / 12d));
                    mixer.SetFloat("freq", Mathf.Pow(2f, value / 12f));
                    break;
                case "m_vol":
                    master_vol = (byte)value; break;
                case "bgm":
                    bgm_vol = (byte)value; break;
                case "key":
                    key_vol = (byte)value; break;
                case "reverb":
                    FXs["reverb"]["value"] = (byte)value; break;
                case "reverb_m":
                    FXs["reverb"]["level"] = (byte)value; break;
                case "lowpass":
                    FXs["lowpass"]["value"] = (byte)value; break;
                case "lowpass_m":
                    FXs["lowpass"]["level"] = (byte)value; break; ;
                case "hipass":
                    FXs["hipass"]["value"] = (byte)value; break;
                case "hipass_m":
                    FXs["hipass"]["level"] = (byte)value; break;
                case "delay":
                    FXs["delay"]["value"] = (byte)value; break;
                case "delay_m":
                    FXs["delay"]["level"] = (byte)value; break;
                case "flanger":
                    FXs["flanger"]["value"] = (byte)value; break;
                case "flanger_m":
                    FXs["flanger"]["level"] = (byte)value; break;
                case "chorus":
                    FXs["chorus"]["value"] = (byte)value; break;
                case "chorus_m":
                    FXs["chorus"]["level"] = (byte)value; break;
                case "dist":
                    FXs["dist"]["value"] = (byte)value; break;
                case "dist_m":
                    FXs["dist"]["level"] = (byte)value; break;
                default:break;
            }
        });
        switch (fx_name){
            case "pitch":
                slider.value = pitch;
                break;
            case "freq":
                slider.value = freq;
                break;
            case "m_vol":
                slider.value = master_vol; break;
            case "bgm":
                slider.value = bgm_vol; break;
            case "key":
                slider.value = key_vol; break;
            case "reverb":
                slider.value = (byte)FXs["reverb"]["value"]; break;
            case "reverb_m":
                slider.value = (byte)FXs["reverb"]["level"]; break;
            case "lowpass":
                slider.value = (byte)FXs["lowpass"]["value"]; break;
            case "lowpass_m":
                slider.value = (byte)FXs["lowpass"]["level"]; break; ;
            case "hipass":
                slider.value = (byte)FXs["hipass"]["value"]; break;
            case "hipass_m":
                slider.value = (byte)FXs["hipass"]["level"]; break;
            case "delay":
                slider.value = (byte)FXs["delay"]["value"]; break;
            case "delay_m":
                slider.value = (byte)FXs["delay"]["level"]; break;
            case "flanger":
                slider.value = (byte)FXs["flanger"]["value"]; break;
            case "flanger_m":
                slider.value = (byte)FXs["flanger"]["level"]; break;
            case "chorus":
                slider.value = (byte)FXs["chorus"]["value"]; break;
            case "chorus_m":
                slider.value = (byte)FXs["chorus"]["level"]; break;
            case "dist":
                slider.value = (byte)FXs["dist"]["value"]; break;
            case "dist_m":
                slider.value = (byte)FXs["dist"]["level"]; break;
            default: break;
        }
        GC.Collect();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
