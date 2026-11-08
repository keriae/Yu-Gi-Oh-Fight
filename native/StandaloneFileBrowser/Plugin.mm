// Based on UnityStandaloneFileBrowser, commit 04a5d49ed2545556da8a7192e86c69bd47641f10.
// Copyright (c) 2017 Gökhan Gökçe.
// Modifications Copyright (c) 2026 keriae.
// SPDX-License-Identifier: MIT. Full permission and disclaimer: LICENSE.txt.
#include "Plugin.pch"
#import <objc/runtime.h>
#include <string>

// ARC keeps each request and filter handler alive through its callback. All
// AppKit work and managed callbacks run on the Cocoa main thread.
@interface FightFileFilterHandler : NSObject
@property(nonatomic, weak) NSSavePanel* panel;
@property(nonatomic, copy) NSArray<NSArray<NSString*>*>* extensions;
- (void)selectFormat:(NSPopUpButton*)sender;
@end

static NSArray<NSString*>* effectiveExtensions(NSArray<NSString*>* extensions) {
    if ([extensions containsObject:@"*"] || [extensions containsObject:@""]) return nil;
    return extensions.count ? extensions : nil;
}

@implementation FightFileFilterHandler
- (void)selectFormat:(NSPopUpButton*)sender {
    NSInteger index = sender.indexOfSelectedItem;
    if (index < 0 || index >= (NSInteger)self.extensions.count) return;
    NSArray<NSString*>* extensions = effectiveExtensions(self.extensions[index]);
    self.panel.allowedFileTypes = extensions;
    self.panel.allowsOtherFileTypes = extensions == nil;
    if (![self.panel isKindOfClass:NSOpenPanel.class] && extensions.count && self.panel.nameFieldStringValue.length) {
        self.panel.nameFieldStringValue =
            [self.panel.nameFieldStringValue.stringByDeletingPathExtension
             stringByAppendingPathExtension:extensions.firstObject];
    }
}
@end

static NSString* text(const char* value) {
    return value ? ([NSString stringWithUTF8String:value] ?: @"") : @"";
}

static void setFilters(NSSavePanel* panel, NSString* filters) {
    NSMutableArray<NSString*>* names = [NSMutableArray array];
    NSMutableArray<NSArray<NSString*>*>* extensions = [NSMutableArray array];
    for (NSString* filter in [filters componentsSeparatedByString:@"|"]) {
        NSRange separator = [filter rangeOfString:@";"];
        if (separator.location == NSNotFound) continue;
        NSString* name = [filter substringToIndex:separator.location];
        NSArray<NSString*>* types = [[filter substringFromIndex:separator.location + 1]
                                     componentsSeparatedByString:@","];
        NSMutableArray<NSString*>* patterns = [NSMutableArray array];
        for (NSString* type in types) {
            [patterns addObject:[type isEqualToString:@"*"] || !type.length ? @"*" :
                                [@"*." stringByAppendingString:type]];
        }
        [names addObject:name.length ?
         [NSString stringWithFormat:@"%@ (%@)", name, [patterns componentsJoinedByString:@", "]] :
         [patterns componentsJoinedByString:@", "]];
        [extensions addObject:types];
    }
    if (!names.count) return;
    FightFileFilterHandler* handler = [FightFileFilterHandler new];
    handler.panel = panel;
    handler.extensions = extensions;
    NSPopUpButton* popup = [[NSPopUpButton alloc] initWithFrame:NSMakeRect(0, 0, 300, 28)
                                                   pullsDown:NO];
    [popup addItemsWithTitles:names];
    popup.target = handler;
    popup.action = @selector(selectFormat:);
    // Filter names come from the selected client language. Avoid fixed English
    // accessory labels; the dropdown already identifies the permitted formats.
    popup.toolTip = [names componentsJoinedByString:@"\n"];
    NSView* accessory = [[NSView alloc] initWithFrame:NSMakeRect(0, 0, 300, 28)];
    [accessory addSubview:popup];
    panel.accessoryView = accessory;
    static char handlerKey;
    objc_setAssociatedObject(panel, &handlerKey, handler, OBJC_ASSOCIATION_RETAIN_NONATOMIC);
    if ([panel isKindOfClass:NSOpenPanel.class]) ((NSOpenPanel*)panel).accessoryViewDisclosed = YES;
    [handler selectFormat:popup];
}

static void configurePanel(NSSavePanel* panel, NSString* title, NSString* directory, NSString* filters) {
    if (title.length) panel.message = title;
    if (directory.length) panel.directoryURL = [NSURL fileURLWithPath:directory isDirectory:YES];
    panel.canCreateDirectories = YES;
    setFilters(panel, filters);
}

static NSString* openDialog(NSString* title, NSString* directory, NSString* filters,
                            bool multiselect, bool folders) {
    [NSApplication sharedApplication];
    NSOpenPanel* panel = [NSOpenPanel openPanel];
    configurePanel(panel, title, directory, folders ? @"" : filters);
    panel.canChooseFiles = !folders;
    panel.canChooseDirectories = folders;
    panel.allowsMultipleSelection = multiselect;
    if ([panel runModal] != NSModalResponseOK) return @"";
    NSMutableArray<NSString*>* paths = [NSMutableArray array];
    for (NSURL* url in panel.URLs) if (url.fileURL) [paths addObject:url.path];
    return [paths componentsJoinedByString:[NSString stringWithFormat:@"%c", 28]];
}

static NSString* saveDialog(NSString* title, NSString* directory, NSString* defaultName, NSString* filters) {
    [NSApplication sharedApplication];
    NSSavePanel* panel = [NSSavePanel savePanel];
    panel.nameFieldStringValue = defaultName;
    configurePanel(panel, title, directory, filters);
    return [panel runModal] == NSModalResponseOK ? (panel.URL.path ?: @"") : @"";
}

static NSMutableArray<dispatch_block_t>* pendingDialogs;
static bool panelActive;

static void runNextDialog() {
    if (panelActive || !pendingDialogs.count) return;
    dispatch_block_t request = pendingDialogs.firstObject;
    [pendingDialogs removeObjectAtIndex:0];
    panelActive = true;
    @try { request(); }
    @finally {
        panelActive = false;
        // A modal AppKit loop pumps the main queue. Start the next request only
        // after this panel and its callback have completely unwound.
        dispatch_async(dispatch_get_main_queue(), ^{ runNextDialog(); });
    }
}

static const char* synchronously(NSString* (^dialog)(void)) {
    // The C# caller copies this pointer after the native function returns.
    // Thread-local storage keeps it valid independently of ARC/autorelease pools.
    static thread_local std::string result;
    __block NSString* selected = @"";
    void (^run)(void) = ^{
        // The managed wrapper rejects overlapping synchronous requests. The C
        // ABI also avoids nesting a panel if an independent caller bypasses it.
        if (panelActive) return;
        panelActive = true;
        @autoreleasepool {
            @try { selected = dialog() ?: @""; }
            @catch (NSException*) { selected = @""; }
        }
        panelActive = false;
        dispatch_async(dispatch_get_main_queue(), ^{ runNextDialog(); });
    };
    if (NSThread.isMainThread) run();
    else dispatch_sync(dispatch_get_main_queue(), run);
    result = selected.UTF8String ?: "";
    return result.c_str();
}

static void asynchronously(callbackFunc callback, NSString* (^dialog)(void)) {
    if (!callback) return;
    dispatch_async(dispatch_get_main_queue(), ^{
        if (!pendingDialogs) pendingDialogs = [NSMutableArray array];
        [pendingDialogs addObject:[^{
          @autoreleasepool {
            NSString* selected = @"";
            @try { selected = dialog() ?: @""; }
            @catch (NSException*) { selected = @""; }
            // Each request captures its own callback, including cancellation.
            // The UTF-8 bytes remain valid for the complete managed invocation.
            callback(selected.UTF8String ?: "");
          }
        } copy]];
        runNextDialog();
    });
}

const char* DialogOpenFilePanel(const char* title, const char* directory, const char* filters, bool multiselect) {
    NSString *heading = text(title), *folder = text(directory), *types = text(filters);
    return synchronously(^{ return openDialog(heading, folder, types, multiselect, false); });
}
void DialogOpenFilePanelAsync(const char* title, const char* directory, const char* filters,
                              bool multiselect, callbackFunc callback) {
    NSString *heading = text(title), *folder = text(directory), *types = text(filters);
    asynchronously(callback, ^{ return openDialog(heading, folder, types, multiselect, false); });
}
const char* DialogOpenFolderPanel(const char* title, const char* directory, bool multiselect) {
    NSString *heading = text(title), *folder = text(directory);
    return synchronously(^{ return openDialog(heading, folder, @"", multiselect, true); });
}
void DialogOpenFolderPanelAsync(const char* title, const char* directory, bool multiselect, callbackFunc callback) {
    NSString *heading = text(title), *folder = text(directory);
    asynchronously(callback, ^{ return openDialog(heading, folder, @"", multiselect, true); });
}
const char* DialogSaveFilePanel(const char* title, const char* directory, const char* defaultName, const char* filters) {
    NSString *heading = text(title), *folder = text(directory), *name = text(defaultName), *types = text(filters);
    return synchronously(^{ return saveDialog(heading, folder, name, types); });
}
void DialogSaveFilePanelAsync(const char* title, const char* directory, const char* defaultName,
                              const char* filters, callbackFunc callback) {
    NSString *heading = text(title), *folder = text(directory), *name = text(defaultName), *types = text(filters);
    asynchronously(callback, ^{ return saveDialog(heading, folder, name, types); });
}
