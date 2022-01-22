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
#if API_1_0_3 || API_1_0_4 || API_1_0_5 || API_1_0_6 || API_2_0_0
using CitiesHarmony.API;
#else
using HarmonyManager;
#endif
using ICities;
using System;

namespace Test.ReflectionTypeLoadException
{
    public class ReflectionTypeLoadExceptionTest : IUserMod
    {
        public string Name => "Reflection Type Load Exception Test";
        public string Description => "Implement bug https://github.com/drok/Harmony-CitiesSkylines/issues/9";

        /* The problem is, the type HarmonyLib.Harmony must be resolved
         * when this assembly is ReflectionLoad'ed, and it's not yet
         * available.
         * That's why this symbol must be enclosed in a private class/method
         * that would not trigger ReflectionLoad resolution.
         */
        HarmonyLib.Harmony harmony = null;

        public void OnEnabled()
        {
            UnityEngine.Assertions.Assert.raiseExceptions = true;

            /* This would pass, except the mod will only load successfully if Harmony is loaded first */
            UnityEngine.Assertions.Assert.IsNull(harmony, "harmony instance is not assigned, should be null");

            throw new Exception("HarmonyLib is already loaded. This test did not test anything.");
        }
    }
}
