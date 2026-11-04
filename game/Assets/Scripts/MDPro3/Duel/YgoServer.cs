using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using MDPro3.Duel.YGOSharp;
using UnityEngine;

namespace MDPro3.Net
{
    internal static unsafe class Dll
    {
        [DllImport("ygoserver", CallingConvention = CallingConvention.Cdecl)]
        public static extern int start_server([MarshalAs(UnmanagedType.LPUTF8Str)] string args);
        [DllImport("ygoserver", CallingConvention = CallingConvention.Cdecl)]
        public static extern void stop_server();
    }

    public class YgoServer
    {
        public static Thread serverThread;
        private static readonly object serverGate = new();
        private static Process serverProcess;

        [DllImport("/usr/lib/libSystem.B.dylib", EntryPoint = "kill")]
        private static extern int SendSignal(int processId, int signal);

        public static void StartServer(string args)
        {
            StopServer();

            if (Application.platform == RuntimePlatform.OSXPlayer)
            {
                StartHostedServer(args);
                return;
            }

            lock (serverGate)
            {
                serverThread = new Thread(() =>
                {
                    Dll.start_server(args);
                }) { IsBackground = true };
                serverThread.Start();
            }
        }

        private static void StartHostedServer(string args)
        {
            // Unity's macOS dataPath is the running player's Contents directory.
            // Also accept a nested Resources/Data path without searching another app.
            var contents = new DirectoryInfo(Path.GetFullPath(Application.dataPath));
            while (contents != null && !(contents.Name == "Contents"
                && contents.Parent != null && contents.Parent.Name.EndsWith(".app", StringComparison.OrdinalIgnoreCase)))
                contents = contents.Parent;
            if (contents == null)
                throw new InvalidOperationException("无法定位本地对战服务，请使用完整的游戏安装包。");
            var host = Path.Combine(contents.FullName, "Helpers", "ygoserver-host");
            var library = Path.Combine(contents.FullName, "PlugIns", "libygoserver.dylib");
            if (!File.Exists(host) || !File.Exists(library))
                throw new InvalidOperationException("本地对战服务文件缺失，请重新安装完整游戏包。");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = host,
                    Arguments = QuoteArgument(library) + " " + QuoteArgument(args),
                    // Empty WorkingDirectory inherits Unity's current game
                    // directory. The shipped Mono strips this property's setter.
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            lock (serverGate)
            {
                try
                {
                    if (!process.Start()) throw new InvalidOperationException();
                    serverProcess = process;
                }
                catch
                {
                    process.Dispose();
                    throw new InvalidOperationException("本地对战服务无法启动，请检查游戏安装包。");
                }
            }
        }

        public static void StopServer()
        {
            // Retire the isolated server before closing any client socket. The
            // publisher's in-process EndDuel can otherwise race an EOF callback.
            lock (serverGate)
            {
                if (serverProcess != null)
                {
                    var process = serverProcess;
                    if (!process.HasExited)
                    {
                        SendSignal(process.Id, 15); // Default SIGTERM, no native teardown.
                        if (!process.WaitForExit(2000))
                        {
                            SendSignal(process.Id, 9);
                            if (!process.WaitForExit(2000))
                                throw new InvalidOperationException("本地对战服务未能停止，请关闭游戏后重试。");
                        }
                    }
                    process.Dispose();
                    serverProcess = null;
                }
                if (Application.platform != RuntimePlatform.OSXPlayer && serverThread != null && serverThread.IsAlive)
                {
                    Dll.stop_server();
                    if (serverThread != Thread.CurrentThread && !serverThread.Join(5000))
                        throw new InvalidOperationException("本地对战服务未能停止，请关闭游戏后重试。");
                }
                serverThread = null;
            }
            AI.ResponsesDuelSession.Stop(false);
        }

        public static bool ServerRunning()
        {
            lock (serverGate)
                return serverProcess != null ? !serverProcess.HasExited : serverThread != null && serverThread.IsAlive;
        }

        // ProcessStartInfo parses argv directly; no shell receives these values.
        private static string QuoteArgument(string value)
            => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
