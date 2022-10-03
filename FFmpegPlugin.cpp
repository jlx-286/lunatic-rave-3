#define __STDC_CONSTANT_MACROS
#define __STDC_LIMIT_MACROS
#include <stdint.h>
// #include <stdio.h>
#include <stdlib.h>
#include <cmath>
// #include <math.h>
// #include <climits>
// #include <limits.h>
#include <float.h>
// #include <cfloat>
#ifdef __cplusplus
using namespace std;
extern "C" {
#endif
#include <libavcodec/avcodec.h>
#include <libavformat/avformat.h>
#include <libavutil/avutil.h>
// #include <libavfilter/avfilter.h>
// #include <libswresample/swresample.h>
// #include <libswscale/swscale.h>
#ifdef __cplusplus
};
#endif
// g++ -O3 -fPIC -shared -Wall -o FFmpegPlugin.so FFmpegPlugin.cpp -lavcodec -lavformat -lavutil
// g++ -O3 -shared -Wall -I"./include" -L"./lib" -o FFmpegPlugin.dll FFmpegPlugin.cpp -lavcodec -lavformat -lavutil rem -fPIC
bool openCodecContext(AVFormatContext* fc, int32_t* pStreamIndex, enum AVMediaType type, AVCodecContext** cc){
    if(avformat_find_stream_info(fc, NULL) < 0){ return false; }
    if(fc == NULL){ return false; }
    *pStreamIndex = av_find_best_stream(fc, type, -1, -1, NULL, 0);
    if (*pStreamIndex < 0) { return false; }
    *cc = fc->streams[*pStreamIndex]->codec;
    if(*cc == NULL){ return false; }
    AVCodec* codec = avcodec_find_decoder((*cc)->codec_id);
    if (codec == NULL) { return false; }
    if (avcodec_open2(*cc, codec, NULL) < 0) { return false; }
    return true;
}
enum AudioFormat{
    Unknown,
    Vorbis,
    Mpeg,
    Others
};
extern "C" AudioFormat GetAudioFormat(const char* path){
#ifdef _WIN32 || _WIN64
    AudioFormat result = Unknown;
#else
    AudioFormat result = AudioFormat::Unknown;
#endif
    AVFormatContext* fc = NULL;
    AVCodecContext* cc = NULL;
    int32_t audioStreamIdx = -1;
    if(avformat_open_input(&fc, path, NULL, NULL) != 0){
        // puts("avformat_open_input");
        goto cleanup;
    }
    if(!openCodecContext(fc, &audioStreamIdx, AVMEDIA_TYPE_AUDIO, &cc) || audioStreamIdx < 0){
        // puts("openCodecContext");
        goto cleanup;
    }
    // switch(fc->audio_codec_id){
    // printf("%d\n", cc->codec_id);
    switch(cc->codec_id){
        case AV_CODEC_ID_MP1:
        case AV_CODEC_ID_MP2:
        case AV_CODEC_ID_MP3:
        case AV_CODEC_ID_MP3ADU:
        case AV_CODEC_ID_MP3ON4:
        case AV_CODEC_ID_MP4ALS:
        case AV_CODEC_ID_MPEG1VIDEO:
        case AV_CODEC_ID_MPEG2TS:
        case AV_CODEC_ID_MPEG2VIDEO:
        case AV_CODEC_ID_MPEG4:
        case AV_CODEC_ID_MPEG4SYSTEMS:
        // case AV_CODEC_ID_MPL2:
        case AV_CODEC_ID_MSMPEG4V1:
        case AV_CODEC_ID_MSMPEG4V2:
        case AV_CODEC_ID_MSMPEG4V3:
        // case AV_CODEC_ID_HEVC:
        case AV_CODEC_ID_H261:
        case AV_CODEC_ID_H263:
        case AV_CODEC_ID_H263I:
        case AV_CODEC_ID_H263P:
        case AV_CODEC_ID_H264:
        case AV_CODEC_ID_H265:
#ifdef _WIN32 || _WIN64
            result = Mpeg;
#else
            result = AudioFormat::Mpeg;
#endif
            break;
        case AV_CODEC_ID_VORBIS:
        case AV_CODEC_ID_THEORA:
        case AV_CODEC_ID_VP3:
        case AV_CODEC_ID_VP4:
        case AV_CODEC_ID_VP5:
        case AV_CODEC_ID_VP6:
        case AV_CODEC_ID_VP6A:
        case AV_CODEC_ID_VP6F:
        case AV_CODEC_ID_VP7:
        case AV_CODEC_ID_VP8:
        case AV_CODEC_ID_VP9:
        // case AV_CODEC_ID_VPLAYER:
#ifdef _WIN32 || _WIN64
            result = Vorbis;
#else
            result = AudioFormat::Vorbis;
#endif
            break;
        default:
#ifdef _WIN32 || _WIN64
            result = Others;
#else
            result = AudioFormat::Others;
#endif
            break;
    }
    cleanup:
    // avformat_close_input(&fc);
    // avcodec_close(cc);
    avcodec_free_context(&cc);
    return result;
}
extern "C" bool GetVideoSize(const char* path, int32_t* width, int32_t* height){
    *width = *height = 0;
    AVFormatContext* fmtCtx = avformat_alloc_context();
    if(avformat_open_input(&fmtCtx, path, NULL, NULL) != 0){
        return false;
    }
    AVCodecContext* cc = NULL;
    int32_t videoStreamIdx = -1;
    if(openCodecContext(fmtCtx, &videoStreamIdx, AVMEDIA_TYPE_VIDEO, &cc)){
        *width = cc->width;
        *height = cc->height;
        avcodec_free_context(&cc);
    }else{
        return false;
    }
    // avformat_close_input(&fmtCtx);
    // avformat_free_context(fmtCtx);
    return true;
}
extern "C" float* GetAudioSamples(const char* path, int32_t* channels, int32_t* frequency, int32_t* length){
    float* total_samples = NULL;
    *channels = *frequency = *length = 0;
    AVFormatContext* fc = NULL;
    AVPacket* pkt = av_packet_alloc();
    AVCodecContext* cc = NULL;
    AVFrame* frame = av_frame_alloc();
    // int32_t ret = 0;
    // AVCodec* codec = NULL;
    int32_t audioStreamIdx = -1;
    int32_t offset = 0;
    int32_t plane_index, flt_sml_size = 0;
    uint8_t sml_u8 = 0; int16_t sml_s16 = 0; int32_t sml_s32 = 0; int64_t sml_s64 = 0;
    // const float FLT_1 = exp(FLT_MIN / 2); const double DBL_1 = exp(DBL_MIN / 2);
    float sml_flt = FLT_MIN / 2; double sml_dbl = DBL_MIN / 2;
    float* sample = NULL;
    if(avformat_open_input(&fc, path, NULL, NULL) != 0){
        goto cleanup;
    }
    if(!openCodecContext(fc, &audioStreamIdx, AVMEDIA_TYPE_AUDIO, &cc) || audioStreamIdx < 0){
        goto cleanup;
    }
    *channels = cc->channels;
    *frequency = cc->sample_rate;
    while(av_read_frame(fc, pkt) >= 0){
        if(pkt->data == NULL || pkt->size <= 0
            || (pkt->flags & AV_PKT_FLAG_CORRUPT) != 0
            || (pkt->flags & AV_PKT_FLAG_KEY) == 0
            || (pkt->flags & AV_PKT_FLAG_DISCARD) != 0
            || pkt->stream_index != audioStreamIdx
        ){ continue; }
        if(avcodec_send_packet(cc, pkt) != 0){
            av_packet_unref(pkt);
            continue;
        }
        while(avcodec_receive_frame(cc, frame) == 0){
        // if(avcodec_decode_audio4(cc, frame, &ret, pkt) > 0 && ret > 0){
            if(frame->key_frame != 1
                || (frame->flags & AV_FRAME_FLAG_CORRUPT) != 0
                || (frame->flags & AV_FRAME_FLAG_DISCARD) != 0
                || (frame->decode_error_flags & FF_DECODE_ERROR_INVALID_BITSTREAM) != 0
                || (frame->decode_error_flags & FF_DECODE_ERROR_MISSING_REFERENCE) != 0
                || (frame->decode_error_flags & FF_DECODE_ERROR_CONCEALMENT_ACTIVE) != 0
                || (frame->decode_error_flags & FF_DECODE_ERROR_DECODE_SLICES) != 0
                || frame->nb_samples < 1
                || frame->sample_rate < 1
                || frame->channels < 1
            ){ continue; }
            int32_t data_size = frame->linesize[0];
            if(data_size <= 0){
                // data_size = av_samples_get_buffer_size(frame->linesize, frame->channels, frame->nb_samples, cc->sample_fmt, 0);
                data_size = av_samples_get_buffer_size(frame->linesize, frame->channels, frame->nb_samples, (AVSampleFormat)frame->format, 0);
            }
            if(data_size <= 0){
                // printf("frame\n");
                continue;
            }
            sample = NULL;
            flt_sml_size = 0;
            // puts(av_get_sample_fmt_name(cc->sample_fmt));
            // switch(cc->sample_fmt){
            switch(frame->format){
                case AV_SAMPLE_FMT_U8:
                    // puts("u8");
                    flt_sml_size = data_size;
                    sample = (float*)malloc(flt_sml_size * sizeof(float));
                    for(plane_index = 0; plane_index < data_size; plane_index++){
                        sml_u8 = *(uint8_t*)(frame->data[0] + plane_index);
                        // sml_s16 = (int16_t)sml_u8 - (uint8_t)0x80;
                        // sample[plane_index] = (float)sml_s16 / (int16_t)0x7fff;
                        sample[plane_index] = (float)sml_u8 / (uint8_t)0xff;
                    }
                    break;
                case AV_SAMPLE_FMT_U8P:
                    // puts("u8p");
                    flt_sml_size = cc->channels * data_size;
                    sample = (float*)malloc(flt_sml_size * sizeof(float));
                    for(plane_index = 0; plane_index < data_size; plane_index++){
                        for(int ch = 0; ch < cc->channels; ch++){
                            sml_u8 = *(uint8_t*)(frame->extended_data[ch] + plane_index);
                            // sml_s16 = (int16_t)sml_u8 - (uint8_t)0x80;
                            // sample[cc->channels * plane_index + ch] = (float)sml_s16 / (int16_t)0x7fff;
                            sample[cc->channels * plane_index + ch] = (float)sml_u8 / (uint8_t)0xff;
                        }
                    }
                    break;
                case AV_SAMPLE_FMT_S16:
                    flt_sml_size = data_size / 2;
                    sample = (float*)malloc(flt_sml_size * sizeof(float));
                    for(plane_index = 0; plane_index < data_size; plane_index += 2){
                        sml_s16 = *(int16_t*)(frame->data[0] + plane_index);
                        sample[plane_index / 2] = (float)sml_s16 / (int16_t)0x7fff;
                    }
                    break;
                case AV_SAMPLE_FMT_S16P:
                    flt_sml_size = cc->channels * data_size / 2;
                    sample = (float*)malloc(flt_sml_size * sizeof(float));
                    for(plane_index = 0; plane_index < data_size; plane_index += 2){
                        for(int ch = 0; ch < cc->channels; ch++){
                            sml_s16 = *(int16_t*)(frame->extended_data[ch] + plane_index);
                            sample[cc->channels * plane_index / 2 + ch] = (float)sml_s16 / (int16_t)0x7fff;
                        }
                    }
                    break;
                case AV_SAMPLE_FMT_S32:
                    flt_sml_size = data_size / 4;
                    sample = (float*)malloc(flt_sml_size * sizeof(float));
                    for(plane_index = 0; plane_index < data_size; plane_index += 4){
                        sml_s32 = *(int32_t*)(frame->data[0] + plane_index);
                        sample[plane_index / 4] = (float)((double)sml_s32 / (int32_t)0x7fffffff);
                    }
                    break;
                case AV_SAMPLE_FMT_S32P:
                    flt_sml_size = cc->channels * data_size / 4;
                    sample = (float*)malloc(flt_sml_size * sizeof(float));
                    for(plane_index = 0; plane_index < data_size; plane_index += 4){
                        for(int ch = 0; ch < cc->channels; ch++){
                            sml_s32 = *(int32_t*)(frame->extended_data[ch] + plane_index);
                            sample[cc->channels * plane_index / 4 + ch] = (float)((double)sml_s32 / (int32_t)0x7fffffff);
                        }
                    }
                    break;
                case AV_SAMPLE_FMT_S64:
                    flt_sml_size = data_size / 8;
                    sample = (float*)malloc(flt_sml_size * sizeof(float));
                    for(plane_index = 0; plane_index < data_size; plane_index += 8){
                        sml_s64 = *(int64_t*)(frame->data[0] + plane_index);
                        sample[plane_index / 8] = (float)((long double)sml_s64 / (int64_t)0x7fffffffffffffff);
                    }
                    break;
                case AV_SAMPLE_FMT_S64P:
                    flt_sml_size = cc->channels * data_size / 8;
                    sample = (float*)malloc(flt_sml_size * sizeof(float));
                    for(plane_index = 0; plane_index < data_size; plane_index += 8){
                        for(int ch = 0; ch < cc->channels; ch++){
                            sml_s64 = *(int64_t*)(frame->extended_data[ch] + plane_index);
                            sample[cc->channels * plane_index / 8 + ch] = (float)((long double)sml_s64 / (int64_t)0x7fffffffffffffff);
                        }
                    }
                    break;
                case AV_SAMPLE_FMT_FLT:
                    for(plane_index = 0; plane_index < data_size; plane_index += sizeof(float)){
                        sml_flt = *(float*)(frame->data[0] + plane_index);
                        if(isnormal(sml_flt)){
                            sample = (float*)realloc(sample,
                                sizeof(float) + flt_sml_size * sizeof(float));
                            sample[flt_sml_size] = sml_flt;
                            flt_sml_size++;
                        }else{
                            goto label;
                        }
                        // if(!isnormal(sml_flt))plane_index += sizeof(float);
                        // if(plane_index < data_size)
                        //     sample[plane_index / sizeof(float)] = sml_flt;
                    }
                    break;
                case AV_SAMPLE_FMT_FLTP:
                    for(plane_index = 0; plane_index < data_size; plane_index += sizeof(float)){
                        for(int ch = 0; ch < cc->channels; ch++){
                            sml_flt = *(float*)(frame->extended_data[ch] + plane_index);
                            if(isnormal(sml_flt)){
                                sample = (float*)realloc(sample,
                                    sizeof(float) + flt_sml_size * sizeof(float));
                                sample[flt_sml_size] = sml_flt;
                                flt_sml_size++;
                            }
                            else{
                                goto label;
                            }
                            // if(!isnormal(sml_flt))plane_index += sizeof(float);
                            // if(plane_index < data_size)
                            //     sample[cc->channels * plane_index / sizeof(float) + ch] = sml_flt;
                        }
                    }
                    break;
                case AV_SAMPLE_FMT_DBL:
                    for(plane_index = 0; plane_index < data_size; plane_index += sizeof(double)){
                        sml_dbl = *(double*)(frame->data[0] + plane_index);
                        if(isnormal(sml_dbl)){
                            sample = (float*)realloc(sample,
                                sizeof(float) + flt_sml_size * sizeof(float));
                            sample[flt_sml_size] = (float)sml_dbl;
                            flt_sml_size++;
                        }else{
                            goto label;
                        }
                        // if(!isnormal(sml_flt))plane_index += sizeof(double);
                        // if(plane_index < data_size)
                        //     sample[plane_index / sizeof(double)] = (float)sml_dbl;
                    }
                    break;
                case AV_SAMPLE_FMT_DBLP:
                    for(plane_index = 0; plane_index < data_size; plane_index += sizeof(double)){
                        for(int ch = 0; ch < cc->channels; ch++){
                            sml_dbl = *(double*)(frame->extended_data[ch] + plane_index);
                            if(isnormal(sml_dbl)){
                                sample = (float*)realloc(sample,
                                    sizeof(float) + flt_sml_size * sizeof(float));
                                sample[flt_sml_size] = (float)sml_dbl;
                                flt_sml_size++;
                            }else{
                                goto label;
                            }
                            // if(!isnormal(sml_flt))plane_index += sizeof(double);
                            // if(plane_index < data_size)
                            //     sample[cc->channels * plane_index / sizeof(double) + ch] = (float)sml_dbl;
                        }
                    }
                    break;
                default:
                    // printf("unknown\n");
                    break;
            }
            label:
            av_buffer_unref(frame->extended_buf);
            av_buffer_unref(frame->buf);
            if(flt_sml_size > 0){
                total_samples = (float*)realloc(total_samples,
                    offset * sizeof(float) + flt_sml_size * sizeof(float));
                if(total_samples == NULL){
                    // exit(1);
                    av_frame_unref(frame);
                    av_packet_unref(pkt);
                    goto cleanup;
                }
                for(int32_t b = 0; b < flt_sml_size; b++){
                    total_samples[offset + b] = sample[b];
                }
                offset += flt_sml_size;
            }
            free(sample);
            av_frame_unref(frame);
            }
        // pkt->data = NULL;
        // pkt->size = 0;
        av_packet_unref(pkt);
    }
    *length = offset;
    // avformat_close_input(&fc);
    cleanup:
    av_packet_free(&pkt);
    av_frame_free(&frame);
    // avcodec_close(cc);
    avcodec_free_context(&cc);
    return total_samples;
}
