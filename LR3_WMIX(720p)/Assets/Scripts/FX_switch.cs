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
                image.sprite = value ? sprites[1] : sprites[0];
                //fx_name = this.name.ToLower();
                switch (fx_name){
                    case "reverb":
                        FXs["reverb"]["enabled"] = value;
                        break;
                    case "chorus":
                        FXs["chorus"]["enabled"] = value;
                        break;
                    case "distortion":
                        FXs["distortion"]["enabled"] = value;
                        break;
                    case "lowpass":
                        FXs["lowpass"]["enabled"] = value;
                        break;
                    case "hipass":
                        FXs["hipass"]["enabled"] = value;
                        break;
                    case "delay":
                        FXs["delay"]["enabled"] = value;
                        break;
                    case "flanger":
                        FXs["flanger"]["enabled"] = value;
                        break;
                }
            });
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
