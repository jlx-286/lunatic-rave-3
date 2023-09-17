#define __STDC_CONSTANT_MACROS
#define __STDC_LIMIT_MACROS
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <deque>
extern "C" {
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libavutil/avutil.h>
#include <libswresample/swresample.h>
#include <libswscale/swscale.h>
};
// g++ -O3 -fPIC -shared -Wall -I"./include" -L"./lib" -o FFmpegPlugin.so FFmpegPlugin.cpp -lavcodec -lavformat -lavutil -lswresample -lswscale
// g++ -O3 -fPIC -shared -Wall -I"./include" -L"./bin" -o FFmpegPlugin.dll FFmpegPlugin.cpp -lavcodec-58 -lavformat-58 -lavutil-56 -lswresample-3 -lswscale-5
AVCodecContext* openCodecContext(AVFormatContext* fc, int* stream, enum AVMediaType type){
    if(fc == NULL || avformat_find_stream_info(fc, NULL) < 0) return NULL;
    *stream = av_find_best_stream(fc, type, -1, -1, NULL, 0);
    if(*stream < 0) return NULL;
    AVCodecContext* cc = fc->streams[*stream]->codec;
    if(cc == NULL) return NULL;
    cc->codec = avcodec_find_decoder(cc->codec_id);
    if(cc->codec == NULL || avcodec_open2(cc, cc->codec, NULL) != 0) return NULL;
    return cc;
}
AVFormatContext* g_fc = NULL;
extern "C" bool GetVideoSize(const char* path, int* width, int* height){
    *width = *height = 0;
    int stream = -1;
    AVCodecContext* cc = NULL;
    if(avformat_open_input(&g_fc, path, NULL, NULL) != 0 || g_fc == NULL)
        goto cleanup;
    cc = openCodecContext(g_fc, &stream, AVMEDIA_TYPE_VIDEO);
    if(cc == NULL || stream < 0) goto cleanup;
    *width = cc->width;
    *height = cc->height;
    cleanup: avformat_close_input(&g_fc);
    return (*width > 0 && *height > 0);
}
SwrContext* g_swr = NULL;
AVPacket* g_pkt = NULL;
AVFrame* g_frame = NULL;
uint8_t* g_buf = NULL;
std::deque<float> AudioSamples;
extern "C" bool GetAudioInfo(const char* path, int* channels, int* frequency, size_t* length){
    *channels = *frequency = *length = 0;
    AVCodecContext* cc = NULL;
    int stream = -1;
    int got_samples, buffer_size, i;
    int64_t channel_layout;
    if(avformat_open_input(&g_fc, path, NULL, NULL) != 0 || g_fc == NULL)
        goto cleanup;
    cc = openCodecContext(g_fc, &stream, AVMEDIA_TYPE_AUDIO);
    if(cc == NULL || stream < 0) goto cleanup;
    *channels = cc->channels;
    *frequency = cc->sample_rate;
    channel_layout = av_get_default_channel_layout(*channels);
    g_pkt = av_packet_alloc();
    if(g_pkt == NULL) goto cleanup;
    g_pkt->data = NULL;
    g_pkt->size = 0;
    g_frame = av_frame_alloc();
    if(g_frame == NULL) goto cleanup;
    g_swr = swr_alloc_set_opts(NULL,
        //AV_CH_FRONT_LEFT | AV_CH_FRONT_RIGHT,
        channel_layout, AV_SAMPLE_FMT_FLT, *frequency,
        //codec_ctx->channel_layout,
        channel_layout, cc->sample_fmt, *frequency,
        0, NULL);
    if(g_swr == NULL) goto cleanup;
    swr_init(g_swr);
    while(av_read_frame(g_fc, g_pkt) == 0){
        if(g_pkt->stream_index != stream) continue;
        if(avcodec_send_packet(cc, g_pkt) == 0){
            while(avcodec_receive_frame(cc, g_frame) == 0){
                buffer_size = av_samples_get_buffer_size(
                    NULL, *channels, g_frame->nb_samples, AV_SAMPLE_FMT_FLT, 1);
                if(buffer_size < 1) goto cleanup;
                g_buf = (uint8_t*)av_malloc(buffer_size);
                // buffer = new uint8_t[buffer_size];
                if(g_buf == NULL) goto cleanup;
                memset(g_buf, 0, buffer_size);
                got_samples = swr_convert(g_swr, &g_buf, g_frame->nb_samples,
                    (const uint8_t **)g_frame->extended_data, g_frame->nb_samples);
                if(got_samples < 0) goto cleanup;
                while(got_samples > 0){
                    // buffer_size = av_samples_get_buffer_size(
                    //     NULL, *channels, got_samples, AV_SAMPLE_FMT_FLT, 1);
                    // if(buffer_size < 1) goto cleanup;
                    for(i = 0; i < buffer_size; i += sizeof(float)){
                        // temp_sml = *(float*)(buffer + i);
                        // if(!std::isnormal(temp_sml)) temp_sml = DBL_MIN / 2;
                        AudioSamples.emplace_back(*(float*)(g_buf + i));
                    }
                    // g_buf = (uint8_t*)av_malloc(buffer_size);
                    // got_samples = swr_convert(g_swr, &g_buf, got_samples, NULL, 0);
                    got_samples = swr_convert(g_swr, &g_buf, g_frame->nb_samples, NULL, 0);
                    // av_freep(&g_buf);
                    if(got_samples < 0) goto cleanup;
                }
                av_freep(&g_buf);
                av_frame_unref(g_frame);
            }
            av_frame_unref(g_frame);
        }
        av_packet_unref(g_pkt);
    }
    *length = AudioSamples.size();
    cleanup:
    av_freep(&g_buf);
    swr_free(&g_swr);
    av_frame_free(&g_frame);
    av_packet_free(&g_pkt);
    avformat_close_input(&g_fc);
    return *length > 0;
}
extern "C" void CopyAudioSamples(float* addr){
    if(addr != NULL) std::copy(AudioSamples.begin(), AudioSamples.end(), addr);
    AudioSamples.clear();
}
int picStream = -1;
extern "C" bool GetPixelsInfo(const char* url, int* width, int* height, bool* isBitmap){
    *width = *height = 0;
    *isBitmap = false;
    picStream = -1;
    AVCodecContext* cc = NULL;
    if(avformat_open_input(&g_fc, url, NULL, NULL) != 0 || g_fc == NULL)
        goto cleanup;
    cc = openCodecContext(g_fc, &picStream, AVMEDIA_TYPE_VIDEO);
    if(cc == NULL || picStream < 0) goto cleanup;
    *width = cc->width; *height = cc->height;
    if(*width < 1 || *height < 1) goto cleanup;
    *isBitmap = (cc->codec_id == AV_CODEC_ID_BMP);
    return true;
    cleanup:
    avformat_close_input(&g_fc);
    return false;
}
SwsContext* g_sws = NULL;
AVFrame* target = NULL;
extern "C" void CopyPixels(void* addr, int width, int height, bool isBitmap, bool strech = false){
    AVCodecContext* cc = NULL;
    if(g_fc == NULL || picStream < 0 || addr == NULL || width < 1 || height < 1) goto cleanup;
    cc = g_fc->streams[picStream]->codec;
    g_sws = sws_getContext(
        width, height, cc->pix_fmt,
        width, height, AV_PIX_FMT_RGBA,
        // SWS_FAST_BILINEAR
        SWS_POINT
        , NULL, NULL, NULL);
    if(g_sws == NULL || sws_init_context(g_sws, NULL, NULL) < 0)
        goto cleanup;
    g_pkt = av_packet_alloc();
    if(g_pkt == NULL) goto cleanup;
    g_pkt->data = NULL; g_pkt->size = 0;
    g_frame = av_frame_alloc();
    if(g_frame == NULL) goto cleanup;
    target = av_frame_alloc();
    if(target == NULL) goto cleanup;
    target->width = width;
    target->height = height;
    target->format = AV_PIX_FMT_RGBA;
    if(av_frame_get_buffer(target, 1) != 0) goto cleanup;
    if(av_read_frame(g_fc, g_pkt) == 0
        && g_pkt->stream_index == picStream
        && avcodec_send_packet(cc, g_pkt) == 0
        && avcodec_receive_frame(cc, g_frame) == 0
        && sws_scale(g_sws, g_frame->data, g_frame->linesize, 0,
            g_frame->height, target->data, target->linesize) > 0
    ){
        if(!strech){
            if(isBitmap || cc->codec_id == AV_CODEC_ID_PNG || cc->codec_id == AV_CODEC_ID_APNG || cc->codec_id == AV_CODEC_ID_GIF){
                if(width >= height){
                    for(int h = 0; h < height; h++){
                        for(int w = 0; w < width; w++){
                            if(*(uint8_t*)(target->data[0] + (w + h * width) * 4 + 0) > 4
                            || *(uint8_t*)(target->data[0] + (w + h * width) * 4 + 1) > 4
                            || *(uint8_t*)(target->data[0] + (w + h * width) * 4 + 2) > 4)
                                memcpy(addr + ((width - 1 - h) * width + w) * 4,
                                    target->data[0] + (h * width + w) * 4, 4);
                        }
                    }
                }
                else if(width < height){
                    for(int h = 0; h < height; h++){
                        for(int w = 0; w < width; w++){
                            if(*(uint8_t*)(target->data[0] + (w + h * width) * 4 + 0) > 4
                            || *(uint8_t*)(target->data[0] + (w + h * width) * 4 + 1) > 4
                            || *(uint8_t*)(target->data[0] + (w + h * width) * 4 + 2) > 4)
                                memcpy(((height - width) / 2 + w + (height - 1 - h) * height)
                                    * 4 + addr, target->data[0] + (w + h * width) * 4, 4);
                        }
                    }
                }
            }else{
                if(width >= height){
                    for(int h = 0; h < height; h++){
                        memcpy(addr + (width - 1 - h) * width * 4,
                            target->data[0] + h * width * 4, 4UL * width);
                    }
                }
                else if(width < height){
                    for(int h = 0; h < height; h++){
                        memcpy(addr + (h * height + (height - width) / 2) * 4,
                            target->data[0] + (height - 1 - h) * width * 4, 4UL * width);
                    }
                }
            }
        }
        else for(int h = 0; h < height; h++)
            memcpy(addr + h * width * 4, target->data[0]
                + (height - 1 - h) * width * 4, 4UL * width);
    }
    cleanup:
    sws_freeContext(g_sws);
    g_sws = NULL;
    av_frame_free(&target);
    av_frame_free(&g_frame);
    av_packet_free(&g_pkt);
    avformat_close_input(&g_fc);
}
extern "C" void CleanUp(){
    av_freep(&g_buf);
    av_frame_free(&target);
    av_frame_free(&g_frame);
    av_packet_free(&g_pkt);
    swr_free(&g_swr);
    sws_freeContext(g_sws);
    g_sws = NULL;
    avformat_close_input(&g_fc);
    AudioSamples.clear();
}