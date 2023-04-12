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
    private Timer timer;
    private bool toDisable = false;
#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
    private Texture2D t2d;
#endif
    private readonly ushort[] bgi_nums = new ushort[]{0,0,0,0};
    private readonly bool[] playing = Enumerable.Repeat(false, 4).ToArray();
	private void Start(){
#if UNITY_EDITOR
        UnityEditor.EditorApplication.pauseStateChanged += _ => {
            int do_pause = (int)_ ^ 1;
            for(byte num = 0; num < bgi_nums.Length; num++)
                VLCPlayer.PlayerSetPause(bgi_nums[num], do_pause);
        };
#endif
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
        for(byte num = 0; num < bgi_nums.Length; num++){
            // if(VLCPlayer.players[bgi_nums[num]] != UIntPtr.Zero && VLCPlayer.PlayerPlaying(bgi_nums[num])){
            if(playing[num]){
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                VLCPlayer.media_textures[num].SetPixels32(VLCPlayer.color32s[bgi_nums[num]]);
                VLCPlayer.media_textures[num].Apply(false);
#else
                GL_libs.BindTexture(VLCPlayer.texture_names[num]);
                fixed(void* p = VLCPlayer.color32s[bgi_nums[num]])
                    GL_libs.TexSubImage2D(VLCPlayer.media_sizes[bgi_nums[num]].width,
                        VLCPlayer.media_sizes[bgi_nums[num]].height, p);
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
        VLCPlayer.PlayerStop(bgi_nums[ii]);
        playing[ii] = false;
        bgi_nums[ii] = bgi_num;
        if(VLCPlayer.players[bgi_num] != UIntPtr.Zero){
            // try{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                VLCPlayer.media_textures[ii] = new Texture2D(
                    VLCPlayer.media_sizes[bgi_num].width,
                    VLCPlayer.media_sizes[bgi_num].height,
                    TextureFormat.RGBA32, false){
                    filterMode = FilterMode.Point };
#else
                fixed(uint* p = VLCPlayer.texture_names){
                    GL_libs.glDeleteTextures(1, p + ii);
                    GL_libs.glGenTextures(1, p + ii);
                }
                GL_libs.BindTexture(VLCPlayer.texture_names[ii]);
                fixed(Color32* p = VLCPlayer.color32s[bgi_num])
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
                VLCPlayer.PlayerPlay(bgi_num);
                while(!VLCPlayer.PlayerPlaying(bgi_num));
                playing[ii] = true;
            // }
            // catch(Exception e){ 
            //     Debug.LogWarning(e.Message);
            // }
        }
        else if(VLCPlayer.players[bgi_num] == UIntPtr.Zero){
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
