using System;
using System.Reflection;
using UnityEngine;

namespace YuGiOhFight.Client.Services
{
    // Bind retained native Unity APIs without changing publisher assemblies.
    // CaptureClock.cs contains the declarations compiled into the native bridge.
    internal static class ReplayVideoClock
    {
        private static Assembly bindings;
        private static MethodInfo getter, setter;
        private static float previous;
        private static bool started;
        internal static Type Bindings(string name)
        {
            if (bindings == null)
                bindings = Assembly.Load(Convert.FromBase64String(System.Runtime.InteropServices.Marshal.PtrToStringAnsi(ReplayVideoExport.ReplayVideoCaptureClock())));
            return bindings.GetType("UnityEngine." + name, true);
        }
        internal static void Begin(int fps)
        {
            if (setter == null)
            {
                var type = Bindings("Time");
                getter = type.GetMethod("get_captureDeltaTime"); setter = type.GetMethod("set_captureDeltaTime");
            }
            previous = (float)getter.Invoke(null, null);
            started = true;
            try
            {
                setter.Invoke(null, new object[] { 1f / fps });
                if (Mathf.Abs((float)getter.Invoke(null, null) - 1f / fps) > .00001f)
                    throw new InvalidOperationException("无法设置回放导出帧时钟。");
                Debug.Log("[ReplayVideoExport] native capture delta verified: " + (float)getter.Invoke(null, null));
            }
            catch { End(); throw; }
        }
        internal static void End()
        {
            if (!started) return;
            started = false; setter.Invoke(null, new object[] { previous });
        }
    }
}
