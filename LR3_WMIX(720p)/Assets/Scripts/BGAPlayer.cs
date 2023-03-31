using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
public class BGAPlayer : MonoBehaviour {
    public BMSPlayer BMS_Player;
    public RawImage[] rawImages;
    private ushort bgi_num;
    public GameObject[] poors;
    private Image[] images;
    private Timer timer;
    private bool toDisable = false;
#if !(UNITY_EDITOR_WIN || UNITY_EDITOR_WIN)
    private Texture2D t2d;
    private readonly ushort[] bgi_nums = new ushort[]{0,0,0,0};
#endif
	private void Start(){
        // Color32[] c = new Color32[1]{new Color32(0, 0, 0, 0)};
        // for(byte i = 0; i < VLCPlayer.media_textures.Length; i++){
        //     VLCPlayer.media_textures[i] = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        //     // VLCPlayer.media_textures[i].SetPixels32(c);
        // }
        timer = new Timer(obj => {
            toDisable = true;
            timer.Change(Timeout.Infinite, Timeout.Infinite);
        }, null, Timeout.Infinite, Timeout.Infinite);
        images = new Image[poors.Length];
        for(byte i = 0; i < poors.Length; i++)
            images[i] = poors[i].GetComponent<Image>();
    }
	//private void FixedUpdate(){}
    private void Update(){
        if(BMS_Player.escaped) return;
        if(!BMS_Player.no_bgi){
            while(BMS_Player.bga_table_row < BMSInfo.bga_list_table.Count){
                if(BMSInfo.bga_list_table[BMS_Player.bga_table_row].time <= BMS_Player.playingTimeAsNanoseconds){
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
            if(toDisable){
                toDisable = false;
                for(byte i = 0; i < poors.Length; i++)
                    poors[i].SetActive(false);
            }
        }
    }
    private unsafe void LateUpdate(){
        for(byte num = 0; num < VLCPlayer.players.Length; num++){
            if(VLCPlayer.players[num] != UIntPtr.Zero && VLCPlayer.PlayerPlaying(VLCPlayer.players[num])){
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                VLCPlayer.media_textures[num].SetPixels32(VLCPlayer.color32s[num]);
                VLCPlayer.media_textures[num].Apply(false);
#else
                GL_libs.BindTexture(VLCPlayer.texture_names[num]);
                fixed(void* p = VLCPlayer.color32s[num])
                    GL_libs.TexSubImage2D(VLCPlayer.media_sizes[bgi_nums[num]].width,
                        VLCPlayer.media_sizes[bgi_nums[num]].height, p);
#endif
            }
        }
    }
    private void OnDestroy(){
        timer.Dispose();
        timer = null;
    }
    private unsafe void ChannelCase(byte ii){
        // if(ii == 3){
        //     for(byte i = 0; i < poors.Length; i++)
        //         poors[i].SetActive(true);
        //     timer.Change(1000, 0);
        // }
        if(VLCPlayer.medias[bgi_num] != UIntPtr.Zero){
            VLCPlayer.PlayerFree(ref VLCPlayer.players[ii]);
            try{
#if !(UNITY_EDITOR_WIN || UNITY_EDITOR_WIN)
                bgi_nums[ii] = bgi_num;
                fixed(uint* p = VLCPlayer.texture_names){
                    GL_libs.glDeleteTextures(1, p + ii);
                    GL_libs.glGenTextures(1, p + ii);
                }
                GL_libs.BindTexture(VLCPlayer.texture_names[ii]);
#endif
                if(VLCPlayer.media_sizes[bgi_num].width <= VLCPlayer.media_sizes[bgi_num].height){
                    VLCPlayer.color32s[ii] = new Color32[
                        VLCPlayer.media_sizes[bgi_num].width * VLCPlayer.media_sizes[bgi_num].height];
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                    VLCPlayer.media_textures[ii] = new Texture2D(
                        VLCPlayer.media_sizes[bgi_num].width,
                        VLCPlayer.media_sizes[bgi_num].height,
                        TextureFormat.RGBA32, false){
                        filterMode = FilterMode.Point };
#else
                    fixed(Color32* p = VLCPlayer.color32s[ii])
                        GL_libs.TexImage2D(VLCPlayer.media_sizes[bgi_num].width,
                            VLCPlayer.media_sizes[bgi_num].height, p);
                    t2d = Texture2D.CreateExternalTexture(
                        VLCPlayer.media_sizes[bgi_num].width,
                        VLCPlayer.media_sizes[bgi_num].height,
                        TextureFormat.RGBA32, false, false,
                        (IntPtr)VLCPlayer.texture_names[ii]);
#endif
                    for(byte i = ii; i < rawImages.Length; i += 4)
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                        rawImages[i].texture = VLCPlayer.media_textures[ii];
#else
                        rawImages[i].texture = t2d;
#endif
                    fixed(void* p = VLCPlayer.color32s[ii])
                        VLCPlayer.players[ii] = VLCPlayer.PlayerNew(VLCPlayer.medias[bgi_num],
                            (uint)VLCPlayer.media_sizes[bgi_num].width,
                            (uint)VLCPlayer.media_sizes[bgi_num].height, p
                            // , (long)(1000 * 60 * 2 / BMS_Reader.start_bpm)
                        );
                }
                else{
                    VLCPlayer.color32s[ii] = new Color32[
                        VLCPlayer.media_sizes[bgi_num].width * VLCPlayer.media_sizes[bgi_num].width];
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                    VLCPlayer.media_textures[ii] = new Texture2D(
                        VLCPlayer.media_sizes[bgi_num].width,
                        VLCPlayer.media_sizes[bgi_num].width,
                        TextureFormat.RGBA32, false){
                        filterMode = FilterMode.Point };
#else
                    fixed(Color32* p = VLCPlayer.color32s[ii])
                        GL_libs.TexImage2D(VLCPlayer.media_sizes[bgi_num].width,
                            VLCPlayer.media_sizes[bgi_num].width, p);
                    t2d = Texture2D.CreateExternalTexture(
                        VLCPlayer.media_sizes[bgi_num].width,
                        VLCPlayer.media_sizes[bgi_num].width,
                        TextureFormat.RGBA32, false, false,
                        (IntPtr)VLCPlayer.texture_names[ii]);
#endif
                    for(byte i = ii; i < rawImages.Length; i += 4)
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                        rawImages[i].texture = VLCPlayer.media_textures[ii];
#else
                        rawImages[i].texture = t2d;
#endif
                    fixed(Color32* p = VLCPlayer.color32s[ii]){
                        VLCPlayer.players[ii] = VLCPlayer.PlayerNew(VLCPlayer.medias[bgi_num],
                            (uint)VLCPlayer.media_sizes[bgi_num].width,
                            (uint)VLCPlayer.media_sizes[bgi_num].height,
                            p + (VLCPlayer.media_sizes[bgi_num].width - 
                            VLCPlayer.media_sizes[bgi_num].height) / 2
                            * VLCPlayer.media_sizes[bgi_num].width
                            // , (long)(1000 * 60 * 2 / BMS_Reader.start_bpm)
                        );
                    }
                    VLCPlayer.media_sizes[bgi_num].height = VLCPlayer.media_sizes[bgi_num].width;
                }
            }
            catch(Exception e){ 
                Debug.LogWarning(e.Message);
            }
        }
        else if(VLCPlayer.medias[bgi_num] == UIntPtr.Zero){
            VLCPlayer.PlayerFree(ref VLCPlayer.players[ii]);
            if(BMSInfo.textures[bgi_num] != null){
                if(ii == 3) for(byte i = 0; i < poors.Length; i++)
                    images[i].enabled = true;
                for(byte i = ii; i < rawImages.Length; i += 4)
                    rawImages[i].texture = BMSInfo.textures[bgi_num];
            }else{
                if(ii == 3) for(byte i = 0; i < poors.Length; i++)
                    images[i].enabled = false;
                for(byte i = ii; i < rawImages.Length; i += 4)
                    rawImages[i].texture = Texture2D.blackTexture;
            }
        }
    }
}
