#define __STDC_CONSTANT_MACROS
#define __STDC_LIMIT_MACROS
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
extern "C" {
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libavutil/avutil.h>
#include <libavutil/time.h>
#include <libswscale/swscale.h>
};
#if LIBAVCODEC_VERSION_MAJOR == 61
// g++ -O3 -fPIC -shared -Wall -I"./include" -L"./lib" -o FFmpegPlayer.7.so FFmpegPlayer.cpp -lavcodec -lavformat -lavutil -lswscale
// g++ -O3 -fPIC -shared -Wall -I"./include" -L"." -o FFmpegPlayer.7.dll FFmpegPlayer.cpp -lavcodec-61 -lavformat-61 -lavutil-59 -lswscale-8
#elif LIBAVCODEC_VERSION_MAJOR == 60
// g++ -O3 -fPIC -shared -Wall -I"./include" -L"./lib" -o FFmpegPlayer.6.so FFmpegPlayer.cpp -lavcodec -lavformat -lavutil -lswscale
// g++ -O3 -fPIC -shared -Wall -I"./include" -L"." -o FFmpegPlayer.6.dll FFmpegPlayer.cpp -lavcodec-60 -lavformat-60 -lavutil-58 -lswscale-7
#elif LIBAVCODEC_VERSION_MAJOR == 59
// g++ -O3 -fPIC -shared -Wall -I"./include" -L"./lib" -o FFmpegPlayer.5.so FFmpegPlayer.cpp -lavcodec -lavformat -lavutil -lswscale
// g++ -O3 -fPIC -shared -Wall -I"./include" -L"." -o FFmpegPlayer.5.dll FFmpegPlayer.cpp -lavcodec-59 -lavformat-59 -lavutil-57 -lswscale-6
#elif LIBAVCODEC_VERSION_MAJOR == 58
// g++-9 -O3 -fPIC -shared -Wall -I"./include" -L"./lib" -o FFmpegPlayer.4.so FFmpegPlayer.cpp -lavcodec -lavformat -lavutil -lswscale
// g++ -O3 -fPIC -shared -Wall -I"./include" -L"." -o FFmpegPlayer.4.dll FFmpegPlayer.cpp -lavcodec-58 -lavformat-58 -lavutil-56 -lswscale-5
#endif
AVCodecContext* openCodecContext(AVFormatContext* fc, int* stream, enum AVMediaType type){
    if(fc == NULL || avformat_find_stream_info(fc, NULL) < 0) return NULL;
    *stream = av_find_best_stream(fc, type, -1, -1, NULL, 0);
    if(*stream < 0) return NULL;
#if LIBAVCODEC_VERSION_MAJOR > 58
    AVCodecContext* cc = avcodec_alloc_context3(NULL);
    if(cc == NULL || avcodec_parameters_to_context(cc, fc->streams[*stream]->codecpar) < 0)
    { avcodec_free_context(&cc); return NULL; }
    cc->codec = avcodec_find_decoder(cc->codec_id);
    if(cc->codec == NULL || avcodec_open2(cc, cc->codec, NULL) != 0)
        avcodec_free_context(&cc);
    return cc;
#else
    AVCodecContext* cc = fc->streams[*stream]->codec;
    if(cc == NULL) return NULL;
    cc->codec = avcodec_find_decoder(cc->codec_id);
    if(cc->codec == NULL || avcodec_open2(cc, cc->codec, NULL) != 0) return NULL;
    return cc;
#endif
}
// #if _WIN32 || _WIN64
const uint8_t pixel_size = 4;
const enum AVPixelFormat pf = AV_PIX_FMT_RGBA;
// #else
// const uint8_t pixel_size = 3;
// const enum AVPixelFormat pf = AV_PIX_FMT_RGB24;
// #endif
const uint16_t ZZ = 36*36;
enum VideoState:uint8_t{
    stopped = 0,
    paused = 1,
    playing = 2,
};
static VideoState states[4] = {VideoState::stopped};
SwsContext* sws_cxts[ZZ] = {NULL};
extern "C" void Init(){
    memset(states, VideoState::stopped, 4 * sizeof(VideoState));
    memset(sws_cxts, 0, ZZ * sizeof(size_t));
}
extern "C" void CleanUp(){
    for(uint16_t i = 0; i < ZZ; i++)
        sws_freeContext(sws_cxts[i]);
    Init();
}
extern "C" bool GetVideoSize(const char* url, uint16_t num, int* width, int* height){
    *width = *height = 0;
    AVFormatContext* fc = NULL; AVCodecContext* cc = NULL; int stream = -1;
    if(avformat_open_input(&fc, url, NULL, NULL) != 0 || fc == NULL) goto cleanup;
    cc = openCodecContext(fc, &stream, AVMEDIA_TYPE_VIDEO);
    if(cc == NULL || stream < 0) goto cleanup;
    sws_cxts[num] = sws_getContext(
        cc->width, cc->height, cc->pix_fmt,
        cc->width, cc->height, pf,
        // SWS_FAST_BILINEAR
        SWS_POINT
        , NULL, NULL, NULL);
    if(sws_cxts[num] == NULL || sws_init_context(sws_cxts[num],
        NULL, NULL) < 0) goto cleanup;
    *width = cc->width; *height = cc->height;
    if(*width < 1 || *height < 1) goto cleanup;
    cleanup:
#if LIBAVCODEC_VERSION_MAJOR > 58
    avcodec_free_context(&cc);
#endif
    avformat_close_input(&fc);
    return *width > 0 && *height > 0;
}
static double speed = 1;
extern "C" void SetSpeed(double _ = 1){
    *(int64_t*)(&_) &= INT64_MAX;
    if(std::isnan(_)) speed = 1;
    else if(_ < 0.25) speed = 0.25;
    else if(_ > 4) speed = 4;
    else speed = _;
}
extern "C" void SetVideoState(uint8_t layer, VideoState _ = VideoState::playing){
    states[layer] = _;
}
extern "C" void PlayVideo(const char* url, uint8_t layer, uint16_t num, void* pixels){
    int64_t startTime, sleepTime, frameTime;
    startTime = av_gettime_relative();
    bool firstFrame = true; int ret;
    double timeBase; AVPacket* packet = NULL; AVFrame *frame = NULL, *target = NULL;
    AVFormatContext* fc = NULL; AVCodecContext* cc = NULL; int stream = -1;
    if(pixels == NULL || avformat_open_input(&fc, url, NULL, NULL)
        != 0 || fc == NULL) goto cleanup;
    cc = openCodecContext(fc, &stream, AVMEDIA_TYPE_VIDEO);
    if(cc == NULL || stream < 0 || cc->width < 1 || cc->height < 1) goto cleanup;
    frame = av_frame_alloc();
    if(frame == NULL) goto cleanup;
    target = av_frame_alloc();
    if(target == NULL) goto cleanup;
    target->width = cc->width;
    target->height = cc->height;
    target->format = pf;
    if(av_frame_get_buffer(target, 1) != 0) goto cleanup;
    timeBase = fc->streams[stream]->time_base.num == 0 ?
        AV_TIME_BASE : av_q2d(av_inv_q(fc->streams[stream]->time_base));
    packet = av_packet_alloc();
    if(packet == NULL) goto cleanup;
    packet->data = NULL; packet->size = 0;
    while(states[layer] != VideoState::stopped){
        if(states[layer] == VideoState::paused) continue;
        else if(states[layer] == VideoState::playing && av_read_frame(fc, packet) == 0){
            if(packet->stream_index == stream){
                avcodec_send_packet(cc, packet);
                ret = avcodec_receive_frame(cc, frame);
                if(ret < 0){
                    if(ret == AVERROR(EAGAIN) || (ret & (AVERROR_INPUT_CHANGED | AVERROR_OUTPUT_CHANGED)) != 0){
                        av_frame_unref(frame);
                        av_packet_unref(packet);
                        continue;
                    }
                    else goto cleanup;
                }
                if(frame->best_effort_timestamp >= 0){
                    frameTime = (int64_t)(std::round(frame->best_effort_timestamp / timeBase * AV_TIME_BASE / speed));
                    if((frame->flags & (AV_FRAME_FLAG_DISCARD | AV_FRAME_FLAG_CORRUPT)) != 0){
                        av_frame_unref(frame);
                        av_packet_unref(packet);
                        continue;
                    }else if(firstFrame){
                        firstFrame = false;
                        startTime -= frameTime;
                    }else{
                        ret = sws_scale(sws_cxts[num], frame->data, frame->linesize, 0, cc->height, target->data, target->linesize);
                        if(ret > 0 && ret <= cc->height){
                            for(int h = 0; h < cc->height; h++)
                                memcpy(pixels + (cc->height - 1 - h) * cc->width * pixel_size,
                                    target->data[0] + h * cc->width * pixel_size, cc->width * pixel_size);
                            sleepTime = frameTime - (av_gettime_relative() - startTime);
                            if(sleepTime <= 0 || sleepTime > UINT32_MAX);
                            else if(av_usleep((unsigned int)sleepTime) == 0);
                            else goto cleanup;
                        }
                        else goto cleanup;
                    }
                }
                av_frame_unref(frame);
            }
            av_packet_unref(packet);
        }else{
            break;
        }
    }
    cleanup:
    states[layer] = VideoState::stopped;
    avcodec_send_packet(cc, NULL);
    avcodec_receive_frame(cc, frame);
    av_frame_free(&target);
    av_frame_free(&frame);
    av_packet_free(&packet);
#if LIBAVCODEC_VERSION_MAJOR > 58
    avcodec_free_context(&cc);
#endif
    avformat_close_input(&fc);
    return;
}