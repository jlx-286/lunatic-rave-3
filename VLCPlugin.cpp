#ifndef __linux
#include <stddef.h>
#include <vlc/vlc.h>
void* lock(void* opaque, void** planes){
    *planes = opaque;
    return NULL;
}
void unlock(void* opaque, void* picture, void* const *planes){
    opaque = static_cast<unsigned char*>(*planes);
    // opaque = *planes;
}
// void display(void* opaque, void* picture){ (void)opaque; }
extern "C" libvlc_instance_t* InstNew(int argc, const char *const *argv){
    return libvlc_new(argc, argv);
}
extern "C" libvlc_media_t* MediaNew(libvlc_instance_t* instance, const char* path){
    if(instance == NULL) return NULL;
    libvlc_media_t* media = NULL;
    media = libvlc_media_new_path(instance, path);
    // media = libvlc_media_new_location(instance, path);
    if(media != NULL) libvlc_media_parse(media);
    return media;
}
extern "C" libvlc_media_player_t* PlayerNew(libvlc_media_t* media, const char* chroma,
unsigned int width, unsigned int height, unsigned int pitch, unsigned char* data, int64_t ms = 0){
    if(media == NULL || width < 1 || height < 1 || pitch < 1) return NULL;
    libvlc_media_player_t* player = libvlc_media_player_new_from_media(media);
    if(player != NULL){
        libvlc_video_set_format(player, chroma, width, height, pitch);
        libvlc_video_set_callbacks(player, lock, unlock, NULL, data);
        // if(ms > 0) libvlc_media_player_set_time(player, ms);
        libvlc_media_player_play(player);
    }
    return player;
}
extern "C" void PlayerFree(libvlc_media_player_t** player){
    if(*player != NULL){
        libvlc_media_player_stop(*player);
        libvlc_media_player_release(*player);
        *player = NULL;
    }
}
extern "C" void MediaFree(libvlc_media_t* media){
    if(media != NULL) libvlc_media_release(media);
}
extern "C" void InstFree(libvlc_instance_t** instance){
    if(*instance != NULL){
        libvlc_release(*instance);
        *instance = NULL;
    }
}
extern "C" bool PlayerPlaying(libvlc_media_player_t* player){
    return libvlc_media_player_get_state(player) < libvlc_Paused;
    // return libvlc_media_player_get_state(player)  == libvlc_Playing;
}
// g++ -O3 -shared -Wall -I"./include" -L"./" -o VLCPlugin.dll VLCPlugin.cpp -lvlc rem -fPIC
#else
// g++ -O3 -fPIC -shared -Wall -o VLCPlugin.so VLCPlugin.cpp -lvlc
#endif