//using FMOD;
using System;
using System.Collections;
using System.Collections.Generic;
//using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class FX_switch : MainVars {
    private Toggle toggle;
    public Sprite[] sprites;
    private Image image;
    public string fx_name;
    // Use this for initialization
    void Start () {
        toggle = this.gameObject.GetComponent<Toggle>();
        if(toggle != null){
            image = toggle.gameObject.GetComponent<Image>();
            image.sprite = toggle.isOn ? sprites[1] : sprites[0];
            toggle.onValueChanged.AddListener((value) => {
                byte val = Convert.ToByte(value ? 1 : 0);
                image.sprite = sprites[val];
                //fx_name = this.name.ToLower();
                switch (fx_name){
                    case "reverb":
                        MainVars.FXs["reverb"]["enabled"] = val;
                        break;
                    case "chorus":
                        MainVars.FXs["chorus"]["enabled"] = val;
                        break;
                    case "distortion":
                        MainVars.FXs["distortion"]["enabled"] = val;
                        break;
                    case "lowpass":
                        MainVars.FXs["lowpass"]["enabled"] = val;
                        break;
                    case "hipass":
                        MainVars.FXs["hipass"]["enabled"] = val;
                        break;
                    case "delay":
                        MainVars.FXs["delay"]["enabled"] = val;
                        break;
                    case "flanger":
                        MainVars.FXs["flanger"]["enabled"] = val;
                        break;
                }
            });
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
