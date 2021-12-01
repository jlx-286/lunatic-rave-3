using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayingTitle : BMSReader {
    private Text title;
    private bool once;
	// Use this for initialization
	void Start () {
        once = false;
	}
	
	// Update is called once per frame
	void Update () {
        if(!once && bms_head != null){
            Debug.Log(bms_head);
            once = true;
        }
        /*if (this.GetComponent<Text>() != null){
            title = this.GetComponent<Text>();
            title.text = string.Empty;
            if (bms_head["TITLE"] != null && bms_head["TITLE"].ToString().Length != 0){
                title.text = bms_head["TITLE"].ToString();
            }
        }*/
	}
}
