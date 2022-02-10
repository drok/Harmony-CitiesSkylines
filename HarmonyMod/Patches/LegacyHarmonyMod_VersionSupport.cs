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
using Harmony2::HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using IAwareness;

namespace HarmonyMod.MyPatches
{
    [HarmonyPatch]
    class LegacyHarmonyMod_VersionSupport
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var h in Patcher.myLegacyHarmonyMods)
                yield return h.GetType("HarmonyMod.Patcher").GetMethod("ImplementAdditionalVersionSupport", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        [HarmonyPrefix]
        [UsedImplicitly]
        public static bool Prefix() => false;

        [UsedImplicitly]
        static bool Prepare(MethodBase original) { return Patcher.harmonyAssembly.Count == 0 && Patcher.myLegacyHarmonyMods.Any(); }

    }
}
