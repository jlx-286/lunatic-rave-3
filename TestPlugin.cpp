#include <stddef.h>
#include <vlc/vlc.h>
void* lock(void* opaque, void** planes){
    *planes = opaque;
    return NULL;
}
void unlock(void* opaque, void* picture, void* const *planes){
    opaque = static_cast<unsigned char*>(*planes);
}
void display(void* opaque, void* picture){ (void)opaque; }
extern "C" void VLC_callback(libvlc_media_player_t* player, unsigned char* data){
    libvlc_video_set_callbacks(player, lock, unlock, display, data);
}
extern "C" bool LibVLC_IsPlaying(libvlc_media_player_t* player){
    return libvlc_media_player_get_state(player) < libvlc_Paused;
}
// g++-10 -O3 -fPIC -shared -Wall -o TestPlugin.so TestPlugin.cpp -lvlc
// "d:/Program Files (x86)/Dev-Cpp/MinGW64/bin/g++.exe" -O3 -shared -Wall -I "./sdk/include" -L "./" -o TestPlugin.dll TestPlugin.cpp -lvlc rem -fPIC -I "D:/Program Files (x86)/Dev-Cpp/MinGW64/x86_64-w64-mingw32/include"