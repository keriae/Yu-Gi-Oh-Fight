#include <dlfcn.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <string.h>

static void catalog_memcpy(void *destination, const void *source, int64_t size) {
    memcpy(destination, source, (size_t)size);
}

static void *catalog_mono_library;
static void catalog_log_exception(void *exception, void *context) {
    (void)context;
    void *(*format)(void *, void **) = dlsym(catalog_mono_library, "mono_object_to_string");
    char *(*utf8)(void *) = dlsym(catalog_mono_library, "mono_string_to_utf8");
    void *format_error = NULL;
    void *message = format(exception, &format_error);
    if (message && !format_error) fprintf(stderr, "Catalog reader exception: %s\n", utf8(message));
    exit(1); /* Never accept a graph after a swallowed reader exception. */
}


/* Execute a managed diagnostic with the publisher's Mono runtime. No game files
   are modified. The assembly directory and native Frameworks are explicit. */
int main(int argc, char **argv) {
    if (argc < 5) {
        fprintf(stderr, "Usage: managed-tool-runner <frameworks> <managed> <tool.exe> [arguments...]\n");
        return 2;
    }
    char runtime[4096], native[4096];
    snprintf(runtime, sizeof(runtime), "%s/libmonobdwgc-2.0.dylib", argv[1]);
    snprintf(native, sizeof(native), "%s/libmono-native.dylib", argv[1]);
    void *library = dlopen(runtime, RTLD_NOW | RTLD_GLOBAL);
    if (!library) { fprintf(stderr, "%s\n", dlerror()); return 1; }
    catalog_mono_library = library;
#define API(name, result, parameters) result (*name) parameters = dlsym(library, #name)
    API(mono_set_assemblies_path, void, (const char *));
    API(mono_set_signal_chaining, void, (int));
    API(mono_jit_init_version, void *, (const char *, const char *));
    API(mono_dllmap_insert, void, (void *, const char *, const char *, const char *, const char *));
    API(mono_domain_assembly_open, void *, (void *, const char *));
    API(mono_jit_exec, int, (void *, void *, int, char **));
    API(mono_jit_cleanup, void, (void *));
    API(mono_add_internal_call, void, (const char *, const void *));
    mono_set_assemblies_path(argv[2]);
    mono_set_signal_chaining(1);
    void *domain = mono_jit_init_version("MDPro3 diagnostic", "v4.0.30319");
    if (!domain) return 1;
    mono_dllmap_insert(NULL, "System.Native", NULL, native, NULL);
    /* BinaryStorageBuffer's reader calls Unity's memcpy intrinsic. Supply its
       standard libc operation for diagnostics outside the Unity player. */
    mono_add_internal_call("Unity.Collections.LowLevel.Unsafe.UnsafeUtility::MemCpy", catalog_memcpy);
    mono_add_internal_call("UnityEngine.DebugLogHandler::Internal_LogException_Injected", catalog_log_exception);
    void *assembly = mono_domain_assembly_open(domain, argv[3]);
    if (!assembly) return 1;
    int result = mono_jit_exec(domain, assembly, argc - 3, argv + 3);
    mono_jit_cleanup(domain);
    return result;
}
