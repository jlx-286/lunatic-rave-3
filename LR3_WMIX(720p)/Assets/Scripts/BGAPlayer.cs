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
            int do_pause = (int)_ ^ 1;
            for(byte num = 0; num < bgi_nums.Length; num++)
                VLCPlayer.PlayerSetPause(bgi_nums[num], do_pause);
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
        for(byte num = 0; num < bgi_nums.Length; num++){
            if(VLCPlayer.playing[bgi_nums[num]]){
                if(VLCPlayer.toStop[bgi_nums[num]]){
                    VLCPlayer.ClearPixels(bgi_nums[num]);
                    VLCPlayer.toStop[bgi_nums[num]] = false;
                    VLCPlayer.playing[bgi_nums[num]] = false;
                }
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                BMSInfo.textures[bgi_nums[num]].Apply(false);
#else
                GL_libs.BindTexture(BMSInfo.texture_names[bgi_nums[num]]);
                GL_libs.TexSubImageRGB((int)VLCPlayer.offsetYs[bgi_nums[num]], VLCPlayer.media_sizes[bgi_nums[num]].width,
                    VLCPlayer.media_sizes[bgi_nums[num]].height, VLCPlayer.addrs[bgi_nums[num]]);
#endif
            }
        }
    }
#if !UNITY_EDITOR
    private void OnApplicationPause(bool pauseStatus){
        int do_pause = pauseStatus ? 1 : 0;
        for(byte num = 0; num < bgi_nums.Length; num++)
            VLCPlayer.PlayerSetPause(bgi_nums[num], do_pause);
    }
#endif
    private void ChannelCase(byte ii){
        VLCPlayer.PlayerStop(bgi_nums[ii]);
        bgi_nums[ii] = bgi_num;
        VLCPlayer.PlayerPlay(bgi_num);
        if(BMSInfo.textures[bgi_num] == null)
            BMSInfo.textures[bgi_num] = Texture2D.blackTexture;
        for(byte i = ii; i < rawImages.Length; i += 4)
            rawImages[i].texture = BMSInfo.textures[bgi_num];
    }
}
