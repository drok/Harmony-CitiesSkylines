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

using ICities;

namespace HarmonyMod
{
    class LoadingExtension : LoadingExtensionBase
    {
        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
#if TRACE
            UnityEngine.Debug.LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] INFO: LoadingExtension.OnCreated(mode={loading.currentMode}, complete={loading.loadingComplete}");
#endif

        }

        public override void OnReleased()
        {
            base.OnReleased();
#if TRACE
            UnityEngine.Debug.LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] INFO: LoadingExtension.OnReleased()");
#endif
        }
    }
}
