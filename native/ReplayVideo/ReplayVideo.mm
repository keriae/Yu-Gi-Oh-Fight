#import <AVFoundation/AVFoundation.h>
#import <AppKit/AppKit.h>
#import <CoreMedia/CoreMedia.h>
#import <CoreVideo/CoreVideo.h>
#include <stdint.h>
#include <string.h>
#include <unistd.h>
#include "CaptureClock.h"

// Original bridge contributions © 2026 keriae.
@interface FightVideoWriter : NSObject
@property AVAssetWriter *writer;
@property AVAssetWriterInput *video;
@property AVAssetWriterInput *audio;
@property AVAssetWriterInputPixelBufferAdaptor *pixels;
@property int width, height, fps, sourceSampleRate;
@property int64_t frames, samples;
@property NSString *failure;
@property NSPanel *panel;
@property NSTextField *progress;
@property BOOL cancelled;
- (void)cancelExport:(id)sender;
@end
@implementation FightVideoWriter
- (void)cancelExport:(id)sender { self.cancelled = YES; self.progress.stringValue = @"Cancelling…"; }
@end
static NSString *lastFailure = @"";
static int fail(FightVideoWriter *w, NSString *reason) {
    w.failure = reason ?: w.writer.error.localizedDescription ?: @"Video encoder failed.";
    lastFailure = w.failure;
    return 0;
}
static bool ready(FightVideoWriter *w, AVAssetWriterInput *input) {
    NSTimeInterval deadline = NSProcessInfo.processInfo.systemUptime + 30.0;
    while (!input.readyForMoreMediaData && NSProcessInfo.processInfo.systemUptime < deadline) {
        if (w.cancelled || w.writer.status != AVAssetWriterStatusWriting) return false;
        // AVFoundation can complete work on the main run loop. Pump those
        // callbacks and the cancellation panel without advancing Unity frames.
        if (NSThread.isMainThread) CFRunLoopRunInMode(kCFRunLoopDefaultMode, 0.001, true);
        else usleep(1000);
    }
    return !w.cancelled && input.readyForMoreMediaData;
}
extern "C" const char *ReplayVideoError(void *handle) {
    return (handle ? ((__bridge FightVideoWriter *)handle).failure : lastFailure).UTF8String;
}
extern "C" void *ReplayVideoCreate(const char *path, int width, int height, int fps, int bitrate, int audio) {
    @autoreleasepool {
        [NSApplication sharedApplication];
        lastFailure = @"";
        if (!path || width < 320 || height < 240 || width > 7680 || height > 4320 || width % 2 || height % 2 || fps < 24 || fps > 120) {
            lastFailure = @"Invalid video settings."; return nullptr;
        }
        FightVideoWriter *w = [FightVideoWriter new];
        w.width = width; w.height = height; w.fps = fps; w.sourceSampleRate = 48000;
        NSError *error;
        w.writer = [AVAssetWriter assetWriterWithURL:[NSURL fileURLWithPath:[NSString stringWithUTF8String:path]] fileType:AVFileTypeMPEG4 error:&error];
        if (!w.writer) { lastFailure = error.localizedDescription; return nullptr; }
        NSDictionary *compression = @{AVVideoAverageBitRateKey: @(bitrate), AVVideoExpectedSourceFrameRateKey: @(fps),
            AVVideoMaxKeyFrameIntervalKey: @(fps * 2), AVVideoProfileLevelKey: AVVideoProfileLevelH264HighAutoLevel};
        w.video = [AVAssetWriterInput assetWriterInputWithMediaType:AVMediaTypeVideo outputSettings:@{
            AVVideoCodecKey: AVVideoCodecTypeH264, AVVideoWidthKey: @(width), AVVideoHeightKey: @(height), AVVideoCompressionPropertiesKey: compression}];
        // Bound encoder lookahead for synchronous video/audio pushes. All media
        // timestamps still come from exported frame and sample counts.
        w.video.expectsMediaDataInRealTime = YES;
        w.pixels = [AVAssetWriterInputPixelBufferAdaptor assetWriterInputPixelBufferAdaptorWithAssetWriterInput:w.video sourcePixelBufferAttributes:@{
            (NSString *)kCVPixelBufferPixelFormatTypeKey: @(kCVPixelFormatType_32BGRA),
            (NSString *)kCVPixelBufferWidthKey: @(width), (NSString *)kCVPixelBufferHeightKey: @(height),
            (NSString *)kCVPixelBufferIOSurfacePropertiesKey: @{}}];
        if (![w.writer canAddInput:w.video]) { fail(w, @"Video encoder settings are unavailable."); return nullptr; }
        [w.writer addInput:w.video];
        if (audio) {
            w.audio = [AVAssetWriterInput assetWriterInputWithMediaType:AVMediaTypeAudio outputSettings:@{
                AVFormatIDKey: @(kAudioFormatMPEG4AAC), AVSampleRateKey: @48000, AVNumberOfChannelsKey: @2, AVEncoderBitRateKey: @192000}];
            w.audio.expectsMediaDataInRealTime = YES;
            if (![w.writer canAddInput:w.audio]) { fail(w, @"Audio encoder settings are unavailable."); return nullptr; }
            [w.writer addInput:w.audio];
        }
        w.writer.shouldOptimizeForNetworkUse = YES;
        if (![w.writer startWriting]) { fail(w, nil); return nullptr; }
        [w.writer startSessionAtSourceTime:kCMTimeZero];
        w.panel = [[NSPanel alloc] initWithContentRect:NSMakeRect(0, 0, 360, 116) styleMask:NSWindowStyleMaskTitled backing:NSBackingStoreBuffered defer:NO];
        w.panel.title = @"Replay video export";
        w.panel.level = NSFloatingWindowLevel;
        w.panel.collectionBehavior = NSWindowCollectionBehaviorFullScreenAuxiliary | NSWindowCollectionBehaviorCanJoinAllSpaces;
        w.progress = [NSTextField labelWithString:@"Exporting rendered game frames…"];
        w.progress.frame = NSMakeRect(20, 68, 320, 26); [w.panel.contentView addSubview:w.progress];
        NSButton *cancel = [NSButton buttonWithTitle:@"Cancel export" target:w action:@selector(cancelExport:)];
        cancel.frame = NSMakeRect(110, 16, 140, 32); [w.panel.contentView addSubview:cancel];
        [w.panel center]; [w.panel orderFrontRegardless];
        return (__bridge_retained void *)w;
    }
}
extern "C" int ReplayVideoAppend(void *handle, const uint8_t *rgba, int flip, const float *stereo, int audioFrames) {
    @autoreleasepool {
        FightVideoWriter *w = (__bridge FightVideoWriter *)handle;
        if (!w || !rgba || !ready(w, w.video)) return fail(w, @"Video encoder stopped accepting frames.");
        CVPixelBufferRef buffer = nullptr;
        if (CVPixelBufferPoolCreatePixelBuffer(kCFAllocatorDefault, w.pixels.pixelBufferPool, &buffer) != kCVReturnSuccess) return fail(w, @"Could not allocate a video frame.");
        CVPixelBufferLockBaseAddress(buffer, 0);
        uint8_t *destination = (uint8_t *)CVPixelBufferGetBaseAddress(buffer);
        size_t stride = CVPixelBufferGetBytesPerRow(buffer);
        for (int y = 0; y < w.height; y++) {
            const uint8_t *source = rgba + (size_t)(flip ? w.height - 1 - y : y) * w.width * 4;
            uint8_t *row = destination + y * stride;
            for (int x = 0; x < w.width; x++) { row[x*4] = source[x*4+2]; row[x*4+1] = source[x*4+1]; row[x*4+2] = source[x*4]; row[x*4+3] = 255; }
        }
        CVPixelBufferUnlockBaseAddress(buffer, 0);
        BOOL appended = [w.pixels appendPixelBuffer:buffer withPresentationTime:CMTimeMake(w.frames, w.fps)];
        CVPixelBufferRelease(buffer);
        if (!appended) return fail(w, nil);
        w.frames++;
        if (w.frames % w.fps == 0) w.progress.stringValue = [NSString stringWithFormat:@"%lld frames · %.0f seconds · %d FPS", w.frames, (double)w.frames / w.fps, w.fps];
        if (w.audio && audioFrames > 0) {
            if (!stereo || !ready(w, w.audio)) return fail(w, @"Audio encoder stopped accepting samples.");
            AudioStreamBasicDescription description = {(Float64)w.sourceSampleRate, kAudioFormatLinearPCM, kAudioFormatFlagIsFloat | kAudioFormatFlagIsPacked, 8, 1, 8, 2, 32, 0};
            CMAudioFormatDescriptionRef format = nullptr;
            CMBlockBufferRef block = nullptr;
            CMSampleBufferRef sample = nullptr;
            size_t length = (size_t)audioFrames * 8;
            OSStatus status = CMAudioFormatDescriptionCreate(kCFAllocatorDefault, &description, 0, nullptr, 0, nullptr, nullptr, &format);
            if (!status) status = CMBlockBufferCreateWithMemoryBlock(kCFAllocatorDefault, nullptr, length, kCFAllocatorDefault, nullptr, 0, length, 0, &block);
            if (!status) status = CMBlockBufferReplaceDataBytes(stereo, block, 0, length);
            CMSampleTimingInfo timing = {CMTimeMake(1, w.sourceSampleRate), CMTimeMake(w.samples, w.sourceSampleRate), kCMTimeInvalid};
            size_t sampleSize = sizeof(float) * 2;
            if (!status) status = CMSampleBufferCreate(kCFAllocatorDefault, block, true, nullptr, nullptr, format, audioFrames, 1, &timing, 1, &sampleSize, &sample);
            BOOL wrote = !status && [w.audio appendSampleBuffer:sample];
            if (sample) CFRelease(sample); if (block) CFRelease(block); if (format) CFRelease(format);
            if (!wrote) return fail(w, w.writer.error.localizedDescription ?: @"Could not encode audio samples.");
            w.samples += audioFrames;
        }
        return 1;
    }
}
extern "C" int ReplayVideoFinish(void *handle) {
    @autoreleasepool {
        FightVideoWriter *w = (__bridge_transfer FightVideoWriter *)handle;
        if (!w || !w.frames) return fail(w, @"No video frames were exported.");
        [w.panel close];
        [w.writer endSessionAtSourceTime:CMTimeMake(w.frames, w.fps)];
        [w.video markAsFinished]; [w.audio markAsFinished];
        dispatch_semaphore_t finished = dispatch_semaphore_create(0);
        [w.writer finishWritingWithCompletionHandler:^{ dispatch_semaphore_signal(finished); }];
        if (dispatch_semaphore_wait(finished, dispatch_time(DISPATCH_TIME_NOW, 60 * NSEC_PER_SEC))) { [w.writer cancelWriting]; return fail(w, @"Finishing the video timed out."); }
        if (w.writer.status != AVAssetWriterStatusCompleted) return fail(w, nil);
        return 1;
    }
}
extern "C" void ReplayVideoCancel(void *handle) {
    @autoreleasepool { FightVideoWriter *w = (__bridge_transfer FightVideoWriter *)handle; [w.panel close]; [w.writer cancelWriting]; }
}

extern "C" int ReplayVideoCancelled(void *handle) { return handle && ((__bridge FightVideoWriter *)handle).cancelled ? 1 : 0; }

extern "C" const char *ReplayVideoCaptureClock() { return fightCaptureClock; }

extern "C" int ReplayVideoSetAudioSampleRate(void *handle, int rate) {
    FightVideoWriter *w = (__bridge FightVideoWriter *)handle;
    if (!w || w.frames || rate < 8000 || rate > 192000) return 0;
    w.sourceSampleRate = rate; return 1;
}
