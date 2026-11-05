/* Original bridge contributions copyright (c) 2026 keriae.
   Third-party libraries retain their own licenses; see COPYRIGHT.md. */
#include <dlfcn.h>
#include <pthread.h>
#include <signal.h>
#include <stdio.h>
#include <string.h>
#include <unistd.h>

typedef int (*start_server_fn)(const char *);

static pid_t parent_pid;

static void *watch_parent(void *context) {
    (void)context;
    for (;;) {
        sleep(1);
        if (getppid() != parent_pid) _exit(0);
    }
    return NULL;
}

/* Keep the original server and its native termination races outside Unity.
   Only public server settings enter argv; model credentials stay in Unity. */
int main(int argc, char **argv) {
    if (argc != 3) {
        fputs("ygoserver-host: invalid arguments\n", stderr);
        return 64;
    }
    parent_pid = getppid();
    if (parent_pid <= 1) {
        fputs("ygoserver-host: parent unavailable\n", stderr);
        return 71;
    }
    signal(SIGTERM, SIG_DFL);
    signal(SIGINT, SIG_DFL);
    pthread_t watcher;
    if (pthread_create(&watcher, NULL, watch_parent, NULL) != 0) {
        fputs("ygoserver-host: parent monitor unavailable\n", stderr);
        return 71;
    }
    pthread_detach(watcher);

    void *library = dlopen(argv[1], RTLD_NOW | RTLD_LOCAL);
    if (library == NULL) {
        fputs("ygoserver-host: library unavailable\n", stderr);
        return 69;
    }
    void *symbol = dlsym(library, "start_server");
    if (symbol == NULL) {
        fputs("ygoserver-host: entry point unavailable\n", stderr);
        return 69;
    }
    start_server_fn start_server;
    _Static_assert(sizeof(start_server) == sizeof(symbol), "macOS function pointer size");
    memcpy(&start_server, &symbol, sizeof(start_server));
    int result = start_server(argv[2]);
    // Avoid native unload/destructors even when the server returns on its own.
    // SIGTERM and parent death likewise terminate without calling stop_server.
    _exit(result < 0 ? 70 : 0);
}
