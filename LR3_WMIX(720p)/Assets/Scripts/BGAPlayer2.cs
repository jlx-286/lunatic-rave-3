using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
public class BGAPlayer2 : MonoBehaviour {
    private RawImage rawImage;
    public byte playerNum;
	private void Start () {;
        rawImage = this.gameObject.GetComponent<RawImage>();
    }
    private void Update () {
        if (VLCPlayer.players[playerNum] != IntPtr.Zero && VLCPlayer.PlayerPlaying(VLCPlayer.players[playerNum])){
            VLCPlayer.media_textures[playerNum].SetPixels32(VLCPlayer.color32s[playerNum]);
            VLCPlayer.media_textures[playerNum].Apply(false);
        }
    }
}
