using System;
using UnityEngine;
using UnityEngine.UI;
public class BGAPlayer : MonoBehaviour {
    public BMSPlayer BMS_Player;
    public RawImage[] rawImages;
    private ushort bgi_num;
	private void Start(){
        // Color32[] c = new Color32[1]{new Color32(0, 0, 0, 0)};
        for(byte i = 0; i < VLCPlayer.media_textures.Length; i++){
            VLCPlayer.media_textures[i] = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            // VLCPlayer.media_textures[i].SetPixels32(c);
        }
    }
	//private void FixedUpdate(){}
    private void Update(){
        if(BMS_Player.escaped) return;
        if(!BMS_Player.no_bgi){
            while(BMS_Player.bga_table_row < BMSInfo.bga_list_table.Count){
                if(BMSInfo.bga_list_table[BMS_Player.bga_table_row].time <= BMS_Player.playingTimeAsMilliseconds){
                    bgi_num = BMSInfo.bga_list_table[BMS_Player.bga_table_row].bgNum;
                    switch(BMSInfo.bga_list_table[BMS_Player.bga_table_row].channel){
                        case BMSInfo.BGAChannel.Base:
                            ChannelCase(0);
                            break;
                        case BMSInfo.BGAChannel.Layer1:
                            ChannelCase(1);
                            break;
                        case BMSInfo.BGAChannel.Layer2:
                            ChannelCase(2);
                            break;
                        case BMSInfo.BGAChannel.Poor:
                            ChannelCase(3);
                            break;
                    }
                    BMS_Player.bga_table_row++;
                }else break;
            }
        }
    }
    private unsafe void ChannelCase(byte ii){
        if(VLCPlayer.medias[bgi_num] != UIntPtr.Zero){
            VLCPlayer.PlayerFree(ref VLCPlayer.players[ii]);
            try{
                if(VLCPlayer.media_sizes[bgi_num].width <= VLCPlayer.media_sizes[bgi_num].height){
                    VLCPlayer.media_textures[ii].Resize(
                        VLCPlayer.media_sizes[bgi_num].width,
                        VLCPlayer.media_sizes[bgi_num].height
                        // ,TextureFormat.RGBA32, false
                    );
                    VLCPlayer.color32s[ii] = new Color32[
                        VLCPlayer.media_sizes[bgi_num].width * VLCPlayer.media_sizes[bgi_num].height];
                    for(byte i = ii; i < rawImages.Length; i += 4)
                        rawImages[i].texture = VLCPlayer.media_textures[ii];
                    fixed(void* p = VLCPlayer.color32s[ii])
                        VLCPlayer.players[ii] = VLCPlayer.PlayerNew(VLCPlayer.medias[bgi_num], "RGBA",
                            (uint)VLCPlayer.media_sizes[bgi_num].width,
                            (uint)VLCPlayer.media_sizes[bgi_num].height,
                            (uint)VLCPlayer.media_sizes[bgi_num].width * 4, p
                            // , (long)(1000 * 60 * 2 / BMS_Reader.start_bpm)
                        );
                }
                else{
                    VLCPlayer.media_textures[ii].Resize(
                        VLCPlayer.media_sizes[bgi_num].width,
                        VLCPlayer.media_sizes[bgi_num].width
                        // ,TextureFormat.RGBA32, false
                    );
                    VLCPlayer.color32s[ii] = new Color32[
                        VLCPlayer.media_sizes[bgi_num].width * VLCPlayer.media_sizes[bgi_num].width];
                    for(byte i = ii; i < rawImages.Length; i += 4)
                        rawImages[i].texture = VLCPlayer.media_textures[ii];
                    fixed(Color32* p = VLCPlayer.color32s[ii]){
                        VLCPlayer.players[ii] = VLCPlayer.PlayerNew(VLCPlayer.medias[bgi_num], "RGBA",
                            (uint)VLCPlayer.media_sizes[bgi_num].width,
                            (uint)VLCPlayer.media_sizes[bgi_num].height,
                            (uint)VLCPlayer.media_sizes[bgi_num].width * 4,
                            p + (VLCPlayer.media_sizes[bgi_num].width - 
                            VLCPlayer.media_sizes[bgi_num].height) / 2
                            * VLCPlayer.media_sizes[bgi_num].width
                            // , (long)(1000 * 60 * 2 / BMS_Reader.start_bpm)
                        );
                    }
                }
            }
            catch(Exception e){ 
                Debug.LogWarning(e.Message);
            }
        }
        else if(VLCPlayer.medias[bgi_num] == UIntPtr.Zero){
            VLCPlayer.PlayerFree(ref VLCPlayer.players[ii]);
            if(BMSInfo.textures[bgi_num] != null)
                for(byte i = ii; i < rawImages.Length; i += 4)
                    rawImages[i].texture = BMSInfo.textures[bgi_num];
            else for(byte i = ii; i < rawImages.Length; i += 4)
                rawImages[i].texture = Texture2D.blackTexture;
        }
    }
}
