#include <stdint.h>
// #include <stdio.h>
#include <stdlib.h>
// #include <stddef.h>
#include <fluidsynth.h>
using namespace std;
static void __FluidCleanUp__(fluid_settings_t* settings, fluid_synth_t* synth){
    if(synth != NULL) delete_fluid_synth(synth);
    if(settings != NULL) delete_fluid_settings(settings);
}
extern "C" bool FluidInit(const char* sfpath, fluid_settings_t** settings, fluid_synth_t** synth,
	int* channels, int* audio_period_size,
	double overflow_volume, double gain
){
    if(*settings == NULL)
        *settings = new_fluid_settings();
    if(*settings == NULL) return false;
    if(*synth == NULL)
        *synth = new_fluid_synth(*settings);
    if(synth == NULL) goto cleanup;
    if(fluid_synth_sfload(*synth, sfpath, 0) == -1) goto cleanup;
    fluid_settings_getint(*settings, "synth.audio-channels", channels);
    if(*channels < 1) goto cleanup;
    fluid_settings_getint(*settings, "audio.period-size", audio_period_size);
    if(*audio_period_size < *channels) goto cleanup;
    fluid_settings_setnum(*settings, "synth.overflow.volume", overflow_volume);
    fluid_settings_setnum(*settings, "synth.gain", gain);
    return true;
    cleanup:
    __FluidCleanUp__(*settings, *synth);
    return false;
}
extern "C" void FluidCleanUp(fluid_settings_t* settings, fluid_synth_t* synth){
    __FluidCleanUp__(settings, synth);
}
extern "C" float* GetMidiSamples(const char* midipath,
    fluid_settings_t* settings, fluid_synth_t* synth,
    int32_t* lengthSamples, int channels,
    int32_t* frequency, int32_t* length,
    int audio_period_size
){
    float* total_samples = NULL;
    *lengthSamples = *frequency = *length = 0;
    fluid_player_t* player = NULL;
    int32_t offset = 0;
    float* temp = NULL;
    player = new_fluid_player(synth);
    if(player == NULL){ goto cleanup; }
    fluid_player_add(player, midipath);
    fluid_player_play(player);
    temp = new float[audio_period_size];
    while(fluid_player_get_status(player) == FLUID_PLAYER_PLAYING){
        if(fluid_synth_write_float(synth, audio_period_size, temp, 0, channels, temp, 1, channels) == 0){
            total_samples = (float*)realloc(total_samples, (offset + audio_period_size) * sizeof(float));
            if(total_samples == NULL){
                goto cleanup;
            }
            for(int i = 0; i < audio_period_size; i++){
                total_samples[offset + i] = temp[i];
            }
            offset += audio_period_size;
        }else{ break; }
    }
    *lengthSamples = offset;
    *length = offset;
    *frequency = offset / audio_period_size / sizeof(float);
    cleanup:
    delete[] temp;
    delete_fluid_player(player);
    return total_samples;
}
// g++ -O3 -fPIC -shared -Wall -o FluidPlugin.so FluidPlugin.cpp -lfluidsynth
// g++ -O3 -shared -Wall -I "./include" -L "./lib" -o FluidPlugin.dll FluidPlugin.cpp -lfluidsynth rem -fPIC
