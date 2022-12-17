using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
public class EffectorNum : MonoBehaviour {
    public Sprite plusImg;
    public Sprite minusImg;
    public Sprite[] numImgs;
    public Sprite zeroImg;
    private Slider slider;
    public GameObject target;
    private Image[] digits;
    private bool isPercent;
    private List<Image> tmp_images = new List<Image>();
    public AudioMixer mixer;
    [HideInInspector] public enum FXname : byte{
        MasterVolume = 1,
        KeyVolume = 2,
        BGMVolume = 3,
        EQ62 = 4,
        EQ160 = 5,
        EQ400 = 6,
        EQ1000 = 7,
        EQ2500 = 8,
        EQ6300 = 9,
        EQ16000 = 10,
        Frequency = 11,
        Pitch = 12,
        EchoDelay = 13,
        EchoDecayRatio = 14,
        LowPassCutoff = 15,
        LowPassRQ = 16,
        HighPassCutoff = 17,
        HighPassRQ = 18,
        Distortion = 19,
        ChorusDelay = 20,
        ChorusRate = 21,
        ChorusDepth = 22,
        ReverbDecayTime = 23,
        ReverbLevel = 24,
        Flanger = 25,
    };
    public FXname fx_name;
	private void Start () {
        isPercent = true;
        slider = this.GetComponent<Slider>();
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
            sbyte valsb = (sbyte)value;
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
            }
            else {
                if(value_int < 0){
                    digits[0].sprite = minusImg;
                }
                else {
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
                case FXname.Pitch:
                    MainVars.pitch = valsb;
                    mixer.SetFloat("pitch", Mathf.Pow(2f, value / 12f));
                    break;
                case FXname.Frequency:
                    MainVars.freq = valsb;
                    mixer.SetFloat("freq", Mathf.Pow(2f, value / 12f));
                    break;
                case FXname.EQ62:
                    MainVars.eq_62 = valsb;
                    mixer.SetFloat("eq62", Mathf.Pow(1.4f, value / 12f));
                    break;
                case FXname.EQ160:
                    MainVars.eq_160 = valsb;
                    mixer.SetFloat("eq160", Mathf.Pow(1.4f, value / 12f));
                    break;
                case FXname.EQ400:
                    MainVars.eq_400 = valsb;
                    mixer.SetFloat("eq400", Mathf.Pow(1.4f, value / 12f));
                    break;
                case FXname.EQ1000:
                    MainVars.eq_1000 = valsb;
                    mixer.SetFloat("eq1000", Mathf.Pow(1.4f, value / 12f));
                    break;
                case FXname.EQ2500:
                    MainVars.eq_2500 = valsb;
                    mixer.SetFloat("eq2500", Mathf.Pow(1.4f, value / 12f));
                    break;
                case FXname.EQ6300:
                    MainVars.eq_6300 = valsb;
                    mixer.SetFloat("eq6300", Mathf.Pow(1.4f, value / 12f));
                    break;
                case FXname.EQ16000:
                    MainVars.eq_16000 = valsb;
                    mixer.SetFloat("eq16000", Mathf.Pow(1.4f, value / 12f));
                    break;
                case FXname.MasterVolume:
                    MainVars.master_vol = value_byte; break;
                case FXname.BGMVolume:
                    MainVars.bgm_vol = value_byte; break;
                case FXname.KeyVolume:
                    MainVars.key_vol = value_byte; break;
                case FXname.EchoDelay:
                    MainVars.delay_d = value_byte;
                    MainVars.echoFilter.delay = Math.Max(value * 50, 10f);
                    break;
                case FXname.EchoDecayRatio:
                    MainVars.decay_r = value_byte;
                    MainVars.echoFilter.decayRatio = value / 100;
                    break;
                case FXname.LowPassCutoff:
                    MainVars.lowpass_c = value_byte;
                    MainVars.lowPassFilter.cutoffFrequency = Math.Max(value * 220, 10f);
                    break;
                case FXname.LowPassRQ:
                    MainVars.lowpass_Q = value_byte;
                    MainVars.lowPassFilter.lowpassResonanceQ = 0.09f * value_byte + 1f;
                    break;
                case FXname.HighPassCutoff:
                    MainVars.hipass_c = value_byte;
                    MainVars.highPassFilter.cutoffFrequency = Math.Max(value * 220, 10f);
                    break;
                case FXname.HighPassRQ:
                    MainVars.hipass_Q = value_byte;
                    MainVars.highPassFilter.highpassResonanceQ = 0.09f * value_byte + 1f;
                    break;
                case FXname.Distortion:
                    MainVars.dist = value_byte;
                    MainVars.distortionFilter.distortionLevel = value / 100;
                    break;
                case FXname.ChorusDelay:
                    MainVars.chorus_del = value_byte;
                    MainVars.chorusFilter.delay = Math.Max(0.1f, value_byte);
                    break;
                case FXname.ChorusRate:
                    MainVars.chorus_r = value_byte;
                    MainVars.chorusFilter.rate = value_byte * 0.2f;
                    break;
                case FXname.ChorusDepth:
                    MainVars.chorus_dep = value_byte;
                    MainVars.chorusFilter.depth = value / 100;
                    break;
                case FXname.ReverbDecayTime:
                    MainVars.reverb_dt = value_byte;
                    MainVars.reverbFilter.decayTime = Math.Max(0.1f, 0.2f * value_byte);
                    break;
                case FXname.ReverbLevel:
                    MainVars.reverb_level = value_byte;
                    MainVars.reverbFilter.reverbLevel = -2000f + value_byte * 20;
                    break;
            }
        });
        switch (fx_name){
            case FXname.Pitch:slider.value = MainVars.pitch; break;
            case FXname.Frequency:slider.value = MainVars.freq; break;
            case FXname.MasterVolume:slider.value = MainVars.master_vol; break;
            case FXname.BGMVolume:slider.value = MainVars.bgm_vol; break;
            case FXname.KeyVolume:slider.value = MainVars.key_vol; break;
            case FXname.EQ62:slider.value = MainVars.eq_62; break;
            case FXname.EQ160:slider.value = MainVars.eq_160; break;
            case FXname.EQ400:slider.value = MainVars.eq_400; break;
            case FXname.EQ1000:slider.value = MainVars.eq_1000; break;
            case FXname.EQ2500:slider.value = MainVars.eq_2500;break;
            case FXname.EQ6300:slider.value = MainVars.eq_6300; break;
            case FXname.EQ16000:slider.value = MainVars.eq_16000; break;
            case FXname.EchoDelay:slider.value = MainVars.delay_d; break;
            case FXname.EchoDecayRatio: slider.value = MainVars.decay_r; break;
            case FXname.LowPassCutoff:slider.value = MainVars.lowpass_c; break;
            case FXname.LowPassRQ:slider.value = MainVars.lowpass_Q; break;
            case FXname.HighPassCutoff:slider.value = MainVars.hipass_c; break;
            case FXname.HighPassRQ:slider.value = MainVars.hipass_Q; break;
            case FXname.Distortion:slider.value = MainVars.dist; break;
            case FXname.ChorusDelay:slider.value = MainVars.chorus_del; break;
            case FXname.ChorusDepth:slider.value = MainVars.chorus_dep; break;
            case FXname.ChorusRate:slider.value = MainVars.chorus_r; break;
            case FXname.ReverbDecayTime:slider.value = MainVars.reverb_dt; break;
            case FXname.ReverbLevel:slider.value = MainVars.reverb_level; break;
        }
	}
	//private void Update () {}
}
