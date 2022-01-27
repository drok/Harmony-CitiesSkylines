/*
 * Harmony for Cities Skylines
 *  Copyright (C) 2021 Radu Hociung <radu.csmods@ohmi.org>
 *  
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the modified GNU General Public License as
 *  published in the root directory of the source distribution.
 *  
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  modified GNU General Public License for more details.
 *  
 *  You should have received a copy of the GNU General Public License along
 *  with this program; if not, write to the Free Software Foundation, Inc.,
 *  51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 */
extern alias Harmony2;

namespace CitiesHarmony
{
    using Harmony2::HarmonyLib;
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
