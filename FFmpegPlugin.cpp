#define __STDC_CONSTANT_MACROS
#define __STDC_LIMIT_MACROS
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <deque>
// #include <cmath>
// #include <float.h>
extern "C" {
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libavutil/avutil.h>
// #include <libavfilter/avfilter.h>
#include <libswresample/swresample.h>
#include <libswscale/swscale.h>
};
// g++ -O3 -fPIC -shared -Wall -o FFmpegPlugin.so FFmpegPlugin.cpp -lavcodec -lavformat -lavutil -lswresample -lswscale
// g++ -O3 -shared -Wall -I"./include" -L"./lib" -o FFmpegPlugin.dll FFmpegPlugin.cpp -lavcodec -lavformat -lavutil -lswresample -lswscale rem -fPIC
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
std::deque<float> AudioSamples;
extern "C" bool GetAudioInfo(const char* path, int32_t* channels, int32_t* frequency, size_t* length){
    *channels = *frequency = *length = 0;
    AVFormatContext* fc = NULL;
    AVCodecContext* cc = NULL;
    AVPacket* pkt = NULL;
    AVFrame* frame = NULL;
    SwrContext* swr_ctx = NULL;
    uint8_t* buffer = NULL;
    int audioStream = -1;
    int got_samples, buffer_size, i;
    int64_t channel_layout;
    if(avformat_open_input(&fc, path, NULL, NULL) != 0 || fc == NULL)
        goto cleanup;
    cc = openCodecContext(fc, &audioStream, AVMEDIA_TYPE_AUDIO);
    if(cc == NULL || audioStream < 0) goto cleanup;
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
        if(pkt->stream_index != audioStream) continue;
        if(avcodec_send_packet(cc, pkt) == 0){
            while(avcodec_receive_frame(cc, frame) == 0){
                buffer_size = av_samples_get_buffer_size(
                    NULL, *channels, frame->nb_samples, AV_SAMPLE_FMT_FLT, 1);
                if(buffer_size < 1) goto cleanup;
                buffer = (uint8_t*)av_malloc(buffer_size);
                // buffer = new uint8_t[buffer_size];
                if(buffer == NULL) goto cleanup;
                memset(buffer, 0, buffer_size);
                got_samples = swr_convert(swr_ctx, &buffer, frame->nb_samples,
                    (const uint8_t **)frame->extended_data, frame->nb_samples);
                if (got_samples < 0) goto cleanup;
                while (got_samples > 0) {
                    // buffer_size = av_samples_get_buffer_size(
                    //     NULL, *channels, got_samples, AV_SAMPLE_FMT_FLT, 1);
                    // if(buffer_size < 1) goto cleanup;
                    for(i = 0; i < buffer_size; i += sizeof(float)){
                        // temp_sml = *(float*)(buffer + i);
                        // if(!std::isnormal(temp_sml)) temp_sml = DBL_MIN / 2;
                        AudioSamples.emplace_back(*(float*)(buffer + i));
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
    *length = AudioSamples.size();
    cleanup:
    // if(buffer != NULL)
    // delete[] buffer;
    // av_free(buffer);
    av_freep(&buffer);
    if(swr_ctx != NULL) swr_free(&swr_ctx);
    if(frame != NULL) av_frame_free(&frame);
    if(pkt != NULL) av_packet_free(&pkt);
    if(fc != NULL) avformat_close_input(&fc);
    return *length > 0;
}
extern "C" void CopyAudioSamples(float* addr){
    // if(addr == NULL || AudioSamples.empty() || AudioSamples.size() < 1) return;
    if(addr != NULL) std::copy(AudioSamples.begin(), AudioSamples.end(), addr);
    AudioSamples.clear();
}
AVFormatContext* pfc = NULL;
AVCodecContext* pcc = NULL;
int picStream = -1;
extern "C" bool GetPixelsInfo(const char* url, int* width, int* height, bool* isBitmap){
    *width = *height = 0;
    *isBitmap = false;
    picStream = -1;
    pfc = NULL;
    pcc = NULL;
    if(avformat_open_input(&pfc, url, NULL, NULL) != 0 || pfc == NULL)
        goto cleanup;
    pcc = openCodecContext(pfc, &picStream, AVMEDIA_TYPE_VIDEO);
    if(pcc == NULL || picStream < 0) goto cleanup;
    *width = pcc->width;
    *height = pcc->height;
    if(*width < 1 && *height < 1) goto cleanup;
    *isBitmap = (pcc->codec_id == AV_CODEC_ID_BMP);
    return true;
    cleanup:
    if(pfc != NULL) avformat_close_input(&pfc);
    pcc = NULL;
    return false;
}
extern "C" void CopyPixels(void* addr, int width, int height, bool isBitmap){
    if(pfc == NULL || pcc == NULL || addr == NULL) return;
    AVFrame* frame = NULL;
    AVPacket* pkt = NULL;
    struct SwsContext* sws = NULL;
    AVFrame* target = NULL;
    uint8_t r,g,b,a;
    sws = sws_getContext(
        width, height, pcc->pix_fmt,
        width, height, AV_PIX_FMT_RGBA,
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
    target->width = width;
    target->height = height;
    target->format = AV_PIX_FMT_RGBA;
    avpicture_alloc((AVPicture*)target, (AVPixelFormat)target->format, width, height);
    if(av_read_frame(pfc, pkt) == 0
        && pkt->stream_index == picStream
        && avcodec_send_packet(pcc, pkt) == 0
        && avcodec_receive_frame(pcc, frame) == 0
        && sws_scale(sws, frame->data, frame->linesize, 0, frame->height,
        target->data, target->linesize) > 0
    ){
        if(isBitmap){
            if(width >= height){
                for(int h = 0; h < height; h++){
                    for(int w = 0; w < width; w++){
                        r = *(uint8_t*)(target->data[0] + (w + h * width) * 4 + 0);
                        g = *(uint8_t*)(target->data[0] + (w + h * width) * 4 + 1);
                        b = *(uint8_t*)(target->data[0] + (w + h * width) * 4 + 2);
                        if(r > 4 || g > 4 || b > 4)
                            memcpy(addr + ((width - 1 - h) * width + w) * 4,
                                target->data[0] + (h * width + w) * 4, 4);
                            // memcpy(addr + (h * width + w) * 4,
                            //     target->data[0] + ((height - 1 - h) * width + w) * 4, 4);
                    }
                }
            }
            else if(width < height){
                for(int h = 0; h < height; h++){
                    for(int w = 0; w < width; w++){
                        r = *(uint8_t*)(target->data[0] + (w + h * width) * 4 + 0);
                        g = *(uint8_t*)(target->data[0] + (w + h * width) * 4 + 1);
                        b = *(uint8_t*)(target->data[0] + (w + h * width) * 4 + 2);
                        if(r > 4 || g > 4 || b > 4)
                            memcpy(addr + (h * height + (height - width) / 2 + w) * 4,
                                target->data[0] + ((height - 1 - h) * width + w) * 4, 4);
                    }
                }
            }
        }else{
            if(width >= height){
                for(int h = 0; h < height; h++){
                    memcpy(addr + (width - 1 - h - (width - height) / 2) * width * 4,
                        target->data[0] + h * width * 4, 4UL * width);
                    // memcpy(addr + h * width * 4,
                    //     target->data[0] + (height - 1 - h) * width * 4, 4UL * width);
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
    cleanup:
    if(sws != NULL) sws_freeContext(sws);
    if(target != NULL) av_frame_free(&target);
    if(frame != NULL) av_frame_free(&frame);
    if(pkt != NULL) av_packet_free(&pkt);
    if(pfc != NULL) avformat_close_input(&pfc);
    pcc = NULL;
}