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

namespace HarmonyMod.MyPatches
{

    [HarmonyPatch(typeof(PluginHelper), "ValidatePlugins", new Type[] {})]
    internal class PluginHelper_ValidatePluginsPatch
    {
        /// <summary>
        /// This function is shunted because it is ill-conceived and does nothing but
        /// slow down the startup. It's also against the Steam Subscriber Agreement terms.
        /// 
        /// The blacklisted mods can just be re-uploaded under a different file id,
        /// and whatever compatibility issue is there would exist again.
        /// 
        /// You can also fix the offending mod right on the Workshop, the Steam
        /// Subscriber agreement gives you that ability. It doesn't permit you to remove
        /// it or disable it, but it allows you to fix it. Subscriber Agreement clause 6.B excerpt:
        ///
        /// "Notwithstanding the license described in Section 6.A., Valve will only have the right
        /// to modify or create derivative works from your Workshop Contribution in the following
        /// cases: (a) Valve may make modifications necessary to make your Contribution compatible
        /// with Steam and the Workshop functionality or user interface, and (b) Valve or the
        /// applicable developer may make modifications to Workshop Contributions that are accepted
        /// for in-Application distribution as it deems necessary or desirable to enhance gameplay."
        /// 
        /// A lawyer or judge would read this to mean, you can decline to accept it to the workshop,
        /// but once accepted, you may only fix it, but you must live with it. Disabling or removing
        /// it does does not fit the goal "to enhance gameplay". Whoever subscribes to a broken
        /// mod does it to enhance their game as promised by the subscription author.
        /// 
        /// Lawyer up, or I'll be happy to explain this to you the expensive way.
        /// </summary>
        [HarmonyPrefix]
        [UsedImplicitly]
        public static bool Prefix() => false;

        [UsedImplicitly]
        static bool Prepare(MethodBase original) { return Patcher.harmonyAssembly.Count == 0; }
    }
}
