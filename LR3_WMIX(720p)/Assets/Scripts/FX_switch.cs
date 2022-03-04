//using FMOD;
using System;
using System.Collections;
using System.Collections.Generic;
//using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class FX_switch : MonoBehaviour {
    private Toggle toggle;
    public Sprite[] sprites;
    private Image image;
    [HideInInspector] public enum ToggleName{
        Echo = 1,
        Delay = 1,
        LowPass = 2,
        HighPass = 3,
        Distortion = 4,
        Chorus = 5,
        Reverb = 6,
        Flanger = 7,
    }
    public ToggleName toggleName;
    // Use this for initialization
    private void Start () {
        toggle = this.gameObject.GetComponent<Toggle>();
        image = this.gameObject.GetComponentInChildren<Image>();
        switch (toggleName){
            case ToggleName.Delay:
                toggle.isOn = MainVars.echoFilter.enabled;
                break;
            case ToggleName.LowPass:
                toggle.isOn = MainVars.lowPassFilter.enabled;
                break;
            case ToggleName.HighPass:
                toggle.isOn = MainVars.highPassFilter.enabled;
                break;
            case ToggleName.Distortion:
                toggle.isOn = MainVars.distortionFilter.enabled;
                break;
            case ToggleName.Chorus:
                toggle.isOn = MainVars.chorusFilter.enabled;
                break;
            case ToggleName.Reverb:
                toggle.isOn = MainVars.reverbFilter.enabled;
                break;
        }
        image.sprite = toggle.isOn ? sprites[1] : sprites[0];
        toggle.onValueChanged.AddListener((value) => {
            byte val = Convert.ToByte(value ? 1 : 0);
            image.sprite = sprites[val];
            switch (toggleName){
                case ToggleName.Delay:
                    MainVars.echoFilter.enabled = value;
                    break;
                case ToggleName.LowPass:
                    MainVars.lowPassFilter.enabled = value;
                    break;
                case ToggleName.HighPass:
                    MainVars.highPassFilter.enabled = value;
                    break;
                case ToggleName.Distortion:
                    MainVars.distortionFilter.enabled = value;
                    break;
                case ToggleName.Chorus:
                    MainVars.chorusFilter.enabled = value;
                    break;
                case ToggleName.Reverb:
                    MainVars.reverbFilter.enabled = value;
                    break;
            }
        });
	}
	
	// Update is called once per frame
	//private void Update () {}
}
