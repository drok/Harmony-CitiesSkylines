/*
 * Harmony for Cities Skylines
 *  Copyright (C) 2021 Radu Hociung <radu.csmods@ohmi.org>
 *  
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *  
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *  
 *  You should have received a copy of the GNU General Public License along
 *  with this program; if not, write to the Free Software Foundation, Inc.,
 *  51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using static ColossalFramework.Plugins.PluginManager;

namespace IAwareness
{
    enum SelfProblemType
    {
        HarmonyLibNotFunctional = 0,
        WrongHarmonyLib,
        ManifestMismatch,
        FailedToInitialize,
        CompatibilityPatchesFailed,
        NotLoadedFirst,
        FailedToUninstall,
        FailedToYieldToMain,
        FailedToReceiveReports,
        FailedToGetAssemblyLocations,
        OwnPatchInstallation,
        OwnPatchRemoval,
        HelperInstallationFailed,
        FailedToReport,
        Last,
    }

    internal interface IAmAware
    {
        /* On Enabled is used by one instance to tell the others
         * of its presence
         */
        void OnMainModChanged(IAmAware mainMod, bool enabled);

        /* Used to transfer state from a helper instance when
         * another instance becomes main mod.
         */
        void PutModReports(Dictionary<PluginInfo, ModReportBase> reports);

        /* Used to report various well-defined conditions */
        void SelfReport(SelfProblemType problem, Exception e);

        /* Used to report whether the Harmony Mod is present and
         * not just installed. When first subscribed, the Harmony Mod
         * is enabled dead last, but the mods that require patching
         * must not get the "Harmony is disabled" error.
         * HarmonyLib will query this to learn if the Mod has been
         * enabled from the beginning.
         */
        bool IsFullyAware();

        /* Used to signal that the first Harmony2 access has started
         * while the Harmony2 was neigther enabled nor disabled
         * (ie, before awareness)
         */
        void OnHarmonyAccessBeforeAwareness(bool needHarmon1StateTransfer);

        /* The API calls this to queue callbacks to its callers
         */
        bool DoOnHarmonyReady(List<ClientCallback> callbacks);

        /* The API calls this to cancel callbacks to its callers
         */
        void CancelHarmonyReadyCallback(List<ClientCallback> callbacks);

        /// <summary>Check permission to Unpatch. Non-patch manager can be prohibited
        /// from removing a manager installed patch.
        /// </summary>
        /// <param name="original">Target method which is to be patched</param>
        /// <param name="caller">Method originating the unpatch request</param>
        /// <param name="patchMethod">Patch to be removed</param>
        /// <returns>true if unpatch is allowed, false if it should be skipped</returns>
        /// <remarks>Added at 0.0.1.0</remarks>
        /// <exception cref="HarmonyModACLException">Thrown if access is blocked</exception>
        bool UnpatchACL(MethodBase original, MethodBase caller, MethodInfo patchMethod);

        /// <summary>
        /// Check permission to install a patch. User mods can be prohibited
        /// from patching the manager or other game methods
        /// </summary>
        /// <param name="original">Target method which is to be patched</param>
        /// <param name="caller">Method originating the unpatch request</param>
        /// <param name="patchMethod">Patch to be removed</param>
        /// <param name="patchType">Prefix, Postfix, etc</param>
        /// <returns>true if patch is allowed, false if it should be skipped</returns>
        /// <remarks>Added at 0.0.1.0</remarks>
        /// <exception cref="HarmonyModACLException">Thrown if access is blocked</exception>
        bool PatchACL(MethodBase original, MethodBase caller, object patchMethod, Enum patchType);
    }

    internal struct ClientCallback
    {
        public Action action;
        public System.Diagnostics.StackTrace callStack;
    }

}
