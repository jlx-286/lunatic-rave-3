#ifndef __cplusplus
#error "not __cplusplus"
#endif
#define __STDC_CONSTANT_MACROS
#define __STDC_LIMIT_MACROS
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
// #include <float.h>
#include <deque>
#include <cmath>
extern "C" {
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libavutil/avutil.h>
// #include <libavfilter/avfilter.h>
#include <libswresample/swresample.h>
// #include <libswscale/swscale.h>
};
// g++ -O3 -fPIC -shared -Wall -o FFmpegPlugin.so FFmpegPlugin.cpp -lavcodec -lavformat -lavutil -lswresample
// g++ -O3 -shared -Wall -I"./include" -L"./lib" -o FFmpegPlugin.dll FFmpegPlugin.cpp -lavcodec -lavformat -lavutil -lswresample rem -fPIC
AVCodecContext* openCodecContext(AVFormatContext* fc, int32_t* pStreamIndex, enum AVMediaType type){
    if(fc == NULL) return NULL;
    if(avformat_find_stream_info(fc, NULL) < 0 || fc == NULL)
        return NULL;
    *pStreamIndex = av_find_best_stream(fc, type, -1, -1, NULL, 0);
    if (*pStreamIndex < 0) return NULL;
    AVCodecContext* cc = fc->streams[*pStreamIndex]->codec;
    if(cc == NULL) return NULL;
    AVCodec* codec = avcodec_find_decoder(cc->codec_id);
    if (codec == NULL) return NULL;
    if (avcodec_open2(cc, codec, NULL) < 0) return NULL;
    return cc;
}
extern "C" bool GetVideoSize(const char* path, int32_t* width, int32_t* height){
    *width = *height = 0;
    AVFormatContext* fmtCtx = NULL;//avformat_alloc_context();
    if(avformat_open_input(&fmtCtx, path, NULL, NULL) != 0 || fmtCtx == NULL)
        return false;
    int32_t videoStreamIdx = -1;
    AVCodecContext* cc = openCodecContext(fmtCtx, &videoStreamIdx, AVMEDIA_TYPE_VIDEO);
    if(cc == NULL || videoStreamIdx < 0){
        avformat_close_input(&fmtCtx);
        return false;
    }
    *width = cc->width;
    *height = cc->height;
    avformat_close_input(&fmtCtx);
    // avformat_free_context(fmtCtx);
    return true;
}
extern "C" float* GetAudioSamples(const char* path, int32_t* channels, int32_t* frequency, size_t* length){
    *channels = *frequency = *length = 0;
    AVFormatContext* fc = NULL;
    AVCodecContext* cc = NULL;
    AVPacket* pkt = NULL;
    AVFrame* frame = NULL;
    SwrContext* swr_ctx = NULL;
    uint8_t* buffer = NULL;
    float* total_samples = NULL;
    std::deque<float> total_samples_deque;
    // float temp_sml = DBL_MIN / 2;
    int32_t audioStreamIdx = -1;
    int got_samples, buffer_size, i;
    int64_t channel_layout;
    if(avformat_open_input(&fc, path, NULL, NULL) != 0 || fc == NULL)
        goto cleanup;
    cc = openCodecContext(fc, &audioStreamIdx, AVMEDIA_TYPE_AUDIO);
    if(cc == NULL || audioStreamIdx < 0) goto cleanup;
    *channels = cc->channels;
    *frequency = cc->sample_rate;
    channel_layout = av_get_default_channel_layout(*channels);
    pkt = av_packet_alloc();
    if(pkt == NULL) goto cleanup;
    pkt->data = NULL;
    pkt->size = 0;
    frame = av_frame_alloc();
    if(frame == NULL) goto cleanup;
    swr_ctx = swr_alloc_set_opts(NULL,
        //AV_CH_FRONT_LEFT | AV_CH_FRONT_RIGHT,
        channel_layout, AV_SAMPLE_FMT_FLT, *frequency,
        //codec_ctx->channel_layout,
        channel_layout, cc->sample_fmt, *frequency,
        0, NULL);
    if(swr_ctx == NULL) goto cleanup;
    swr_init(swr_ctx);
    while(av_read_frame(fc, pkt) == 0){
        if(pkt->stream_index != audioStreamIdx) continue;
        if(avcodec_send_packet(cc, pkt) == 0){
            while(avcodec_receive_frame(cc, frame) == 0){
                buffer_size = av_samples_get_buffer_size(
                    NULL, *channels, frame->nb_samples, AV_SAMPLE_FMT_FLT, 1);
                if(buffer_size < 1) goto cleanup;
                buffer = (uint8_t*)av_malloc(buffer_size);
                // buffer = new uint8_t[buffer_size];
                if(buffer == NULL) goto cleanup;
                memset(buffer, 0, buffer_size);
                got_samples = swr_convert(swr_ctx, &buffer, frame->nb_samples, (const uint8_t **)frame->extended_data, frame->nb_samples);
                if (got_samples < 0) goto cleanup;
                while (got_samples > 0) {
                    // buffer_size = av_samples_get_buffer_size(
                    //     NULL, *channels, got_samples, AV_SAMPLE_FMT_FLT, 1);
                    // if(buffer_size < 1) goto cleanup;
                    for(i = 0; i < buffer_size; i += sizeof(float)){
                        // temp_sml = *(float*)(buffer + i);
                        // if(!std::isnormal(temp_sml)) temp_sml = DBL_MIN / 2;
                        // total_samples_deque.emplace_back(temp_sml);
                        total_samples_deque.emplace_back(*(float*)(buffer + i));
                    }
                    // buffer = (uint8_t*)av_malloc(buffer_size);
                    // got_samples = swr_convert(swr_ctx, &buffer, got_samples, NULL, 0);
                    got_samples = swr_convert(swr_ctx, &buffer, frame->nb_samples, NULL, 0);
                    // av_freep(buffer);
                    if (got_samples < 0) goto cleanup;
                }
                // if(buffer != NULL)
                // delete[] buffer;
                // av_free(buffer);
                av_freep(&buffer);
                // swr_free(&swr_ctx);
            }
        }
        av_packet_unref(pkt);
    }
    *length = total_samples_deque.size();
    if(*length > INT32_MAX) goto cleanup;
    // total_samples = new float[*length];
    total_samples = (float*)malloc(*length * sizeof(float));
    std::copy(total_samples_deque.begin(), total_samples_deque.end(), total_samples);
    // for(i = 0; i < *length; i++) total_samples[i] = total_samples_deque[i];
    cleanup:
    if(*length > 0) total_samples_deque.clear();
    // if(buffer != NULL)
    // delete[] buffer;
    // av_free(buffer);
    av_freep(&buffer);
    if(swr_ctx != NULL) swr_free(&swr_ctx);
    if(frame != NULL) av_frame_free(&frame);
    if(pkt != NULL) av_packet_free(&pkt);
    if(fc != NULL) avformat_close_input(&fc);
    return total_samples;
}
extern "C" void FreeAudioSamples(float* total_samples){
    if(total_samples != NULL) free(total_samples);// delete[] total_samples;
}