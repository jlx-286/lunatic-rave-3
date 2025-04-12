#define __STDC_CONSTANT_MACROS
#define __STDC_LIMIT_MACROS
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
extern "C" {
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libavutil/avutil.h>
#include <libswresample/swresample.h>
#include <libswscale/swscale.h>
};
#if LIBAVCODEC_VERSION_MAJOR == 61
// g++ -O3 -fPIC -shared -Wall -I"./include" -L"./lib" -o FFmpegPlugin.7.so FFmpegPlugin.cpp -lavcodec -lavformat -lavutil -lswresample -lswscale
// g++ -O3 -fPIC -shared -Wall -I"./include" -L"." -o FFmpegPlugin.7.dll FFmpegPlugin.cpp -lavcodec-61 -lavformat-61 -lavutil-59 -lswresample-5 -lswscale-8
#elif LIBAVCODEC_VERSION_MAJOR == 60
// g++ -O3 -fPIC -shared -Wall -I"./include" -L"./lib" -o FFmpegPlugin.6.so FFmpegPlugin.cpp -lavcodec -lavformat -lavutil -lswresample -lswscale
// g++ -O3 -fPIC -shared -Wall -I"./include" -L"." -o FFmpegPlugin.6.dll FFmpegPlugin.cpp -lavcodec-60 -lavformat-60 -lavutil-58 -lswresample-4 -lswscale-7
#elif LIBAVCODEC_VERSION_MAJOR == 59
// g++ -O3 -fPIC -shared -Wall -I"./include" -L"./lib" -o FFmpegPlugin.5.so FFmpegPlugin.cpp -lavcodec -lavformat -lavutil -lswresample -lswscale
// g++ -O3 -fPIC -shared -Wall -I"./include" -L"." -o FFmpegPlugin.5.dll FFmpegPlugin.cpp -lavcodec-59 -lavformat-59 -lavutil-57 -lswresample-4 -lswscale-6
#elif LIBAVCODEC_VERSION_MAJOR == 58
// g++-9 -O3 -fPIC -shared -Wall -I"./include" -L"./lib" -o FFmpegPlugin.4.so FFmpegPlugin.cpp -lavcodec -lavformat -lavutil -lswresample -lswscale
// g++ -O3 -fPIC -shared -Wall -I"./include" -L"." -o FFmpegPlugin.4.dll FFmpegPlugin.cpp -lavcodec-58 -lavformat-58 -lavutil-56 -lswresample-3 -lswscale-5
#endif
AVCodecContext* openCodecContext(AVFormatContext* fc, int* stream, enum AVMediaType type){
    if(fc == NULL || avformat_find_stream_info(fc, NULL) < 0) return NULL;
    *stream = av_find_best_stream(fc, type, -1, -1, NULL, 0);
    if(*stream < 0) return NULL;
    AVCodecContext* cc = NULL;
#if LIBAVCODEC_VERSION_MAJOR > 58
    cc = avcodec_alloc_context3(NULL);
    if(cc == NULL || avcodec_parameters_to_context(cc, fc->streams[*stream]->codecpar) < 0)
    { avcodec_free_context(&cc); return NULL; }
    cc->codec = avcodec_find_decoder(cc->codec_id);
    if(cc->codec == NULL || avcodec_open2(cc, cc->codec, NULL) != 0)
        avcodec_free_context(&cc);
    return cc;
#else
    cc = fc->streams[*stream]->codec;
    if(cc == NULL) return NULL;
    cc->codec = avcodec_find_decoder(cc->codec_id);
    if(cc->codec == NULL || avcodec_open2(cc, cc->codec, NULL) != 0) return NULL;
    return cc;
#endif
}
extern "C" void GetAudioInfo(const char* path, AVSampleFormat format, int* channels, int* frequency, uint8_t*(*add)(size_t), void(*append)()){
    *channels = *frequency = 0;
    AVFormatContext* fc = NULL; SwrContext* swr = NULL;
    AVCodecContext* cc = NULL;
    AVPacket* pkt = NULL; AVFrame* frame = NULL; uint8_t* buf = NULL;
    int stream = -1;
    int got_samples, buffer_size;
    if(add == NULL || append == NULL || avformat_open_input(&fc, path, NULL, NULL)
        != 0 || fc == NULL) goto cleanup;
    cc = openCodecContext(fc, &stream, AVMEDIA_TYPE_AUDIO);
    if(cc == NULL || stream < 0) goto cleanup;
    *frequency = cc->sample_rate;
    pkt = av_packet_alloc();
    if(pkt == NULL) goto cleanup;
    pkt->data = NULL;
    pkt->size = 0;
    frame = av_frame_alloc();
    if(frame == NULL) goto cleanup;
#if LIBAVCODEC_VERSION_MAJOR > 60
    *channels = cc->ch_layout.nb_channels;
    swr = swr_alloc();
    if(swr == NULL) goto cleanup;
    if(swr_alloc_set_opts2(&swr,
        &cc->ch_layout, format, *frequency,
        &cc->ch_layout, cc->sample_fmt, *frequency,
        0, NULL) != 0)
        goto cleanup;
#else
    *channels = cc->channels;
    cc->channel_layout = av_get_default_channel_layout(*channels);
    swr = swr_alloc_set_opts(NULL,
        //AV_CH_FRONT_LEFT | AV_CH_FRONT_RIGHT,
        cc->channel_layout, format, *frequency,
        //codec_ctx->channel_layout,
        cc->channel_layout, cc->sample_fmt, *frequency,
        0, NULL);
    if(swr == NULL) goto cleanup;
#endif
    swr_init(swr);
    while(av_read_frame(fc, pkt) == 0){
        if(pkt->stream_index == stream && avcodec_send_packet(cc, pkt) == 0){
            while(avcodec_receive_frame(cc, frame) == 0){
                buffer_size = av_samples_get_buffer_size(
                    NULL, *channels, frame->nb_samples, format, 1);
                if(buffer_size < 1) goto cleanup;
                buf = add(buffer_size);
                if(buf == NULL) goto cleanup;
                got_samples = swr_convert(swr, &buf, frame->nb_samples,
                    (const uint8_t **)frame->extended_data, frame->nb_samples);
                if(got_samples < 0) goto cleanup;
                while(got_samples > 0){
                    // buffer_size = av_samples_get_buffer_size(
                    //     NULL, *channels, got_samples, format, 1);
                    // if(buffer_size < 1) goto cleanup;
                    append();
                    // buf = (uint8_t*)av_malloc(buffer_size);
                    // got_samples = swr_convert(swr, &buf, got_samples, NULL, 0);
                    got_samples = swr_convert(swr, &buf, frame->nb_samples, NULL, 0);
                    // av_freep(&buf);
                    if(got_samples < 0) goto cleanup;
                }
                // av_freep(&buf);
                av_frame_unref(frame);
            }
            av_frame_unref(frame);
        }
        av_packet_unref(pkt);
    }
    cleanup:
    // av_freep(&buf);
    swr_free(&swr);
    av_frame_free(&frame);
    av_packet_free(&pkt);
#if LIBAVCODEC_VERSION_MAJOR > 58
    avcodec_free_context(&cc);
#endif
    avformat_close_input(&fc);
}
extern "C" void GetPixelsInfo(const char* url, int* width, int* height, void*(*load)(int,int), bool strech = false){
    *width = *height = 0;
    bool isBitmap = false;
    int picStream = -1;
    AVFormatContext* fc = NULL;
    AVCodecContext* cc = NULL;
    SwsContext* sws = NULL; AVPacket* pkt = NULL;
    AVFrame *frame = NULL, *target = NULL; void* addr = NULL;
    if(load == NULL || avformat_open_input(&fc, url, NULL, NULL) != 0 || fc == NULL)
        goto cleanup;
    cc = openCodecContext(fc, &picStream, AVMEDIA_TYPE_VIDEO);
    if(cc == NULL || picStream < 0) goto cleanup;
    *width = cc->width; *height = cc->height;
    if(*width < 1 || *height < 1) goto cleanup;
    isBitmap = (cc->codec_id == AV_CODEC_ID_BMP);
    sws = sws_getContext(
        *width, *height, cc->pix_fmt,
        *width, *height, AV_PIX_FMT_RGBA,
        // SWS_FAST_BILINEAR
        SWS_POINT
        , NULL, NULL, NULL);
    if(sws == NULL || sws_init_context(sws, NULL, NULL) < 0)
        goto cleanup;
    pkt = av_packet_alloc();
    if(pkt == NULL) goto cleanup;
    pkt->data = NULL; pkt->size = 0;
    frame = av_frame_alloc();
    if(frame == NULL) goto cleanup;
    target = av_frame_alloc();
    if(target == NULL) goto cleanup;
    target->width = *width;
    target->height = *height;
    target->format = AV_PIX_FMT_RGBA;
    if(av_frame_get_buffer(target, 1) != 0) goto cleanup;
    addr = load(*width, *height);
    if(addr == NULL) goto cleanup;
    if(av_read_frame(fc, pkt) == 0
        && pkt->stream_index == picStream
        && avcodec_send_packet(cc, pkt) == 0
        && avcodec_receive_frame(cc, frame) == 0
        && sws_scale(sws, frame->data, frame->linesize, 0,
            frame->height, target->data, target->linesize) > 0
    ){
        if(!strech){
            if(isBitmap || cc->codec_id == AV_CODEC_ID_PNG || cc->codec_id == AV_CODEC_ID_APNG || cc->codec_id == AV_CODEC_ID_GIF){
                if(*width >= *height){
                    for(int h = 0; h < *height; h++){
                        for(int w = 0; w < *width; w++){
                            if(*(uint8_t*)(target->data[0] + (w + h * *width) * 4 + 0) > 4
                            || *(uint8_t*)(target->data[0] + (w + h * *width) * 4 + 1) > 4
                            || *(uint8_t*)(target->data[0] + (w + h * *width) * 4 + 2) > 4)
                                memcpy(addr + ((*width - 1 - h) * *width + w) * 4,
                                    target->data[0] + (h * *width + w) * 4, 4);
                        }
                    }
                }
                else if(*width < *height){
                    for(int h = 0; h < *height; h++){
                        for(int w = 0; w < *width; w++){
                            if(*(uint8_t*)(target->data[0] + (w + h * *width) * 4 + 0) > 4
                            || *(uint8_t*)(target->data[0] + (w + h * *width) * 4 + 1) > 4
                            || *(uint8_t*)(target->data[0] + (w + h * *width) * 4 + 2) > 4)
                                memcpy(((*height - *width) / 2 + w + (*height - 1 - h) * *height)
                                    * 4 + addr, target->data[0] + (w + h * *width) * 4, 4);
                        }
                    }
                }
            }else{
                if(*width >= *height){
                    for(int h = 0; h < *height; h++){
                        memcpy(addr + (*width - 1 - h) * *width * 4,
                            target->data[0] + h * *width * 4, 4UL * *width);
                    }
                }
                else if(*width < *height){
                    for(int h = 0; h < *height; h++){
                        memcpy(addr + (h * *height + (*height - *width) / 2) * 4,
                            target->data[0] + (*height - 1 - h) * *width * 4, 4UL * *width);
                    }
                }
            }
        }
        else for(int h = 0; h < *height; h++)
            memcpy(addr + h * *width * 4, target->data[0]
                + (*height - 1 - h) * *width * 4, 4UL * *width);
    }
    cleanup:
    sws_freeContext(sws);
    av_frame_free(&target);
    av_frame_free(&frame);
    av_packet_free(&pkt);
#if LIBAVCODEC_VERSION_MAJOR > 58
    avcodec_free_context(&cc);
#endif
    avformat_close_input(&fc);
}