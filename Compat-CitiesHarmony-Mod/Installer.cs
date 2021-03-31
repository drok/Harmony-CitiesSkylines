namespace CitiesHarmony.API
{
    using HarmonyLib;
    using System.Diagnostics;
    using System.Reflection;

    public static class Installer {

        /* Installer.Run() is a legacy static method called by boformer's CitiesHarmony.API.HarmonyHelper,
         * various versions, so it must not be removed, for backward compatibility
         * If this method exists, it understands that the CitiesHarmony Mod is installed
         * */
        public static void Run() {
            var stack = new StackTrace();
            var lastCaller = stack.GetFrame(0).GetMethod();
            MethodBase caller = lastCaller;
            int assemblyDepth = 0;
            /* Search in the stack for the assembly that called
             * my caller(CitiesHarmony.API)
             */
            for (int i = 1; i < stack.FrameCount && assemblyDepth < 3; ++i)
            {
                caller = stack.GetFrame(i).GetMethod();
                if (lastCaller.DeclaringType.Assembly.FullName != caller.DeclaringType.Assembly.FullName)
                {
                    lastCaller = caller;
                    ++assemblyDepth;
                }
            }

            if (!Harmony.harmonyUsers.TryGetValue(caller.DeclaringType.Assembly.FullName, out var userStatus)) {
                Harmony.harmonyUsers[caller.DeclaringType.Assembly.FullName] = new Harmony.HarmonyUser() { instancesCreated = 0, checkBeforeUse = true, };
            }
        }

    }
}
