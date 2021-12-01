using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetNumImg : MonoBehaviour{
    public Sprite[] nums;
    public Sprite _0;
    //public Sprite point;
    public Sprite eImg;
    public Image[] target;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    public void SetNum(int num, bool filled){
        num = HalveNum(num);
    }
    private int HalveNum(int num){
        byte length = 1;
        int temp = 1;
        while (temp < num){
            temp *= 10;
            if (temp > num) break;
            length++;
        }
        if(num < 0) {
            return Math.Max(num,1 - (int)Math.Pow(10, length - 1));
        }
        else if (num > 0){
            return Math.Min(num, (int)Math.Pow(10, length - 1) - 1);
        }
        return 0;
    }
}
