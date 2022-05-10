using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class BGAPlayer2 : MonoBehaviour {
    // public BGAPlayer BGAPlayer;
    //private RawImage[] rawImages;
    private RawImage rawImage;
    public byte playerNum;
	// Use this for initialization
	private void Start () {
        //BGAPlayer = this.gameObject.GetComponent<BGAPlayer>();
        //rawImages = BGAPlayer.rawImages;
        rawImage = this.gameObject.GetComponent<RawImage>();
    }

    // Update is called once per frame
    private void Update () {
        if (VLCPlayer.players[playerNum] != IntPtr.Zero && VLCPlayer.LibVLC_IsPlaying(VLCPlayer.players[playerNum])){
            VLCPlayer.media_textures[playerNum].SetPixels32(VLCPlayer.color32s[playerNum]);
            VLCPlayer.media_textures[playerNum].Apply(false);
            rawImage.texture = VLCPlayer.media_textures[playerNum];
        }
    }
    // private void LateUpdate(){
    //     if (VLCPlayer.players[playerNum] != IntPtr.Zero && VLCPlayer.LibVLC_IsPlaying(VLCPlayer.players[playerNum])){
    //     }
    // }
}
