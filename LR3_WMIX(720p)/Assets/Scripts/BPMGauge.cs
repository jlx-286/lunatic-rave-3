using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BPMGauge :BMSReader{
    public Sprite[] l_num_sprites;
    public Sprite[] s_num_sprites;
    public Transform min, now, max;
    private byte[] digits;
    // Use this for initialization
    void Start () {
        digits = new byte[3];
        //Debug.Log(GameObject.Find("top"));
        //Debug.Log(GameObject.Find("top").GetComponent<Sprite>());
        ShowCurrentBPM(130f);
        //Debug.Log(start_bpm);
    }
	
	// Update is called once per frame
	void Update () {
		
	}
    void ShowCurrentBPM(float bpm){
        string bpm_str = bpm.ToString().Trim().Split('.')[0];
        if (bpm_str[0] >= '0' && bpm_str[0] <= '9'){
            for (byte i = 0; i < digits.Length; i++){
                digits[i] = (byte)bpm_str[bpm_str.Length - 1 - i];
            }
        }
    }
    void ShowBPMRange(){
        //min_bpm;
    }
}
