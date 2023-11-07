using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
public class BGAPlayer : MonoBehaviour {
    public BMSPlayer BMS_Player;
    public RawImage[] rawImages;
    private ushort bgi_num;
    public GameObject[] poors;
    private Image[] images;
    private int bga_table_row = 0;
    private readonly ushort[] bgi_nums = new ushort[]{0,0,0,0};
	private void Awake(){
#if UNITY_EDITOR
        UnityEditor.EditorApplication.pauseStateChanged += _ => {
            for(byte layer = 0; layer < bgi_nums.Length; layer++)
                FFmpegVideoPlayer.PlayerSetPause(layer, _);
        };
#endif
        images = new Image[poors.Length];
        for(byte i = 0; i < poors.Length; i++)
            images[i] = poors[i].GetComponent<Image>();
        if(BMSInfo.bga_list_table.Any(v => v.channel == BGAChannel.Poor)){
            for(byte i = 0; i < poors.Length; i++)
                images[i].enabled = true;
        }else{
            for(byte i = 0; i < poors.Length; i++){
                images[i].enabled = false;
                poors[i].GetComponentInChildren<RawImage>().texture = Texture2D.blackTexture;
            }
        }
    }
	//private void FixedUpdate(){}
    private void Update(){
        if(BMS_Player.escaped) return;
        if(BMS_Player.playingTimeAsNanoseconds <= BMSInfo.totalTimeAsNanoseconds){
            while(bga_table_row < BMSInfo.bga_list_table.Count){
                if(BMSInfo.bga_list_table[bga_table_row].time <= BMS_Player.playingTimeAsNanoseconds){
                    bgi_num = BMSInfo.bga_list_table[bga_table_row].bgNum;
                    switch(BMSInfo.bga_list_table[bga_table_row].channel){
                        case BGAChannel.Base:
                            ChannelCase(0);
                            break;
                        case BGAChannel.Layer1:
                            ChannelCase(1);
                            break;
                        case BGAChannel.Layer2:
                            ChannelCase(2);
                            break;
                        case BGAChannel.Poor:
                            ChannelCase(3);
                            break;
                    }
                    bga_table_row++;
                }else break;
            }
        }
    }
    private unsafe void LateUpdate(){
        for(byte layer = 0; layer < bgi_nums.Length; layer++){
            if(FFmpegVideoPlayer.playing[layer]){
                if(FFmpegVideoPlayer.toStop[layer]){
                    FFmpegVideoPlayer.ClearPixels(layer, bgi_nums[layer]);
                    FFmpegVideoPlayer.toStop[layer] = false;
                    FFmpegVideoPlayer.playing[layer] = false;
                }
                FFmpegVideoPlayer.textures[layer].Apply(false);
            }
        }
    }
#if !UNITY_EDITOR
    private void OnApplicationPause(bool pauseStatus){
        for(byte layer = 0; layer < bgi_nums.Length; layer++)
            FFmpegVideoPlayer.PlayerSetPause(layer, pauseStatus);
    }
#endif
    private void ChannelCase(byte layer){
        FFmpegVideoPlayer.PlayerStop(layer);
        bgi_nums[layer] = bgi_num;
        if(FFmpegVideoPlayer.media_sizes[bgi_num].width > 0){
            FFmpegVideoPlayer.PlayerPlay(layer, bgi_num);
            for(byte i = layer; i < rawImages.Length; i += 4)
                rawImages[i].texture = FFmpegVideoPlayer.textures[layer];
        }else{
            if(BMSInfo.textures[bgi_num] == null)
                BMSInfo.textures[bgi_num] = Texture2D.blackTexture;
            for(byte i = layer; i < rawImages.Length; i += 4)
                rawImages[i].texture = BMSInfo.textures[bgi_num];
        }
    }
}
