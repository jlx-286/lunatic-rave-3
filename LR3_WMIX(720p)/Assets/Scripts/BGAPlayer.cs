using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
public class BGAPlayer : MonoBehaviour {
    public BMSPlayer BMS_Player;
    public RawImage[] rawImages;
    [HideInInspector] public ushort bgi_num;
	private void Start(){
        // Color32[] c = new Color32[1]{new Color32(0, 0, 0, 0)};
        for(int i = 0; i < VLCPlayer.media_textures.Length; i++){
            VLCPlayer.media_textures[i] = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            // VLCPlayer.media_textures[i].SetPixels32(c);
        }
    }
	//private void FixedUpdate(){}
    private unsafe void Update(){
        if(BMS_Player.escaped) return;
        if(!BMS_Player.no_bgi){
            while(BMS_Player.bga_table_row < BMSInfo.bga_num_arr.Length){
                if(BMSInfo.bga_time_arr[BMS_Player.bga_table_row] <= BMS_Player.playingTimeAsMilliseconds){
                    bgi_num = BMSInfo.bga_num_arr[BMS_Player.bga_table_row];
                    switch(BMSInfo.bga_channel_arr[BMS_Player.bga_table_row]){
                        case "04"://base
                            if(VLCPlayer.medias.ContainsKey(bgi_num)){
                                VLCPlayer.PlayerFree(ref VLCPlayer.players[0]);
                                try{
                                    VLCPlayer.media_textures[0].Resize(
                                        VLCPlayer.media_sizes[bgi_num].width,
                                        VLCPlayer.media_sizes[bgi_num].height
                                        // ,TextureFormat.RGBA32, false
                                    );
                                    VLCPlayer.color32s[0] = new Color32[
                                        VLCPlayer.media_sizes[bgi_num].width * VLCPlayer.media_sizes[bgi_num].height];
                                    for(int i = 0; i < rawImages.Length; i += 4)
                                        rawImages[i].texture = VLCPlayer.media_textures[0];
                                    fixed(void* p = VLCPlayer.color32s[0])
                                        VLCPlayer.players[0] = VLCPlayer.PlayerNew(VLCPlayer.medias[bgi_num], "RGBA",
                                            (uint)VLCPlayer.media_sizes[bgi_num].width,
                                            (uint)VLCPlayer.media_sizes[bgi_num].height,
                                            (uint)VLCPlayer.media_sizes[bgi_num].width * 4, p
                                            // , (long)(1000 * 60 * 2 / BMS_Reader.start_bpm)
                                        );
                                }
                                catch(Exception e){
                                    Debug.LogWarning(e.Message);
                                }
                            }
                            else if(!VLCPlayer.medias.ContainsKey(bgi_num)){
                                VLCPlayer.PlayerFree(ref VLCPlayer.players[0]);
                                if(BMSInfo.textures.ContainsKey(bgi_num))
                                    for(int i = 0; i < rawImages.Length; i += 4)
                                        rawImages[i].texture = BMSInfo.textures[bgi_num];
                                else for(int i = 0; i < rawImages.Length; i += 4)
                                    rawImages[i].texture = Texture2D.blackTexture;
                            }
                            break;
                        case "07"://layer
                            if(VLCPlayer.medias.ContainsKey(bgi_num)){
                                VLCPlayer.PlayerFree(ref VLCPlayer.players[1]);
                                try{
                                    VLCPlayer.media_textures[1].Resize(
                                        VLCPlayer.media_sizes[bgi_num].width,
                                        VLCPlayer.media_sizes[bgi_num].height
                                        // ,TextureFormat.RGBA32, false
                                    );
                                    VLCPlayer.color32s[1] = new Color32[
                                        VLCPlayer.media_sizes[bgi_num].width * VLCPlayer.media_sizes[bgi_num].height];
                                    for(int i = 1; i < rawImages.Length; i += 4)
                                        rawImages[i].texture = VLCPlayer.media_textures[1];
                                    fixed(void* p = VLCPlayer.color32s[1])
                                        VLCPlayer.players[1] = VLCPlayer.PlayerNew(VLCPlayer.medias[bgi_num], "RGBA",
                                            (uint)VLCPlayer.media_sizes[bgi_num].width,
                                            (uint)VLCPlayer.media_sizes[bgi_num].height,
                                            (uint)VLCPlayer.media_sizes[bgi_num].width * 4, p
                                            // , (long)(1000 * 60 * 2 / BMS_Reader.start_bpm)
                                        );
                                }
                                catch(Exception e){
                                    Debug.LogWarning(e.Message);
                                }
                            }
                            else if(!VLCPlayer.medias.ContainsKey(bgi_num)){
                                VLCPlayer.PlayerFree(ref VLCPlayer.players[1]);
                                if(BMSInfo.textures.ContainsKey(bgi_num))
                                    for(int i = 1; i < rawImages.Length; i += 4)
                                        rawImages[i].texture = BMSInfo.textures[bgi_num];
                                else for(int i = 1; i < rawImages.Length; i += 4)
                                    rawImages[i].texture = Texture2D.blackTexture;
                            }
                            break;
                        case "0A"://layer2
                            if(VLCPlayer.medias.ContainsKey(bgi_num)){
                                VLCPlayer.PlayerFree(ref VLCPlayer.players[2]);
                                try{
                                    VLCPlayer.media_textures[2].Resize(
                                        VLCPlayer.media_sizes[bgi_num].width,
                                        VLCPlayer.media_sizes[bgi_num].height
                                        // ,TextureFormat.RGBA32, false
                                    );
                                    VLCPlayer.color32s[2] = new Color32[
                                        VLCPlayer.media_sizes[bgi_num].width * VLCPlayer.media_sizes[bgi_num].height];
                                    for(int i = 2; i < rawImages.Length; i += 4)
                                        rawImages[i].texture = VLCPlayer.media_textures[2];
                                    fixed(void* p = VLCPlayer.color32s[2])
                                        VLCPlayer.players[2] = VLCPlayer.PlayerNew(VLCPlayer.medias[bgi_num], "RGBA",
                                            (uint)VLCPlayer.media_sizes[bgi_num].width,
                                            (uint)VLCPlayer.media_sizes[bgi_num].height,
                                            (uint)VLCPlayer.media_sizes[bgi_num].width * 4, p
                                            // , (long)(1000 * 60 * 2 / BMS_Reader.start_bpm)
                                        );
                                }
                                catch(Exception e){
                                    Debug.LogWarning(e.Message);
                                }
                            }
                            else if(!VLCPlayer.medias.ContainsKey(bgi_num)){
                                VLCPlayer.PlayerFree(ref VLCPlayer.players[2]);
                                if(BMSInfo.textures.ContainsKey(bgi_num))
                                    for(int i = 2; i < rawImages.Length; i += 4)
                                        rawImages[i].texture = BMSInfo.textures[bgi_num];
                                else for(int i = 2; i < rawImages.Length; i += 4)
                                    rawImages[i].texture = Texture2D.blackTexture;
                            }
                            break;
                        /*case "06":// bad/poor
                            if(VLCPlayer.medias.ContainsKey(bgi_num)){
                                //
                            }
                            else if(!VLCPlayer.medias.ContainsKey(bgi_num)){
                                if(BMSInfo.textures.ContainsKey(bgi_num)){}
                                else{}
                            }
                            break;*/
                    }
                    BMS_Player.bga_table_row++;
                }else break;
            }
        }
    }
}
