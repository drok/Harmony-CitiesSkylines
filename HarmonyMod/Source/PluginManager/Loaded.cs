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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Reflection;
using System.Configuration;
using System.Collections;
using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.Plugins;
using ColossalFramework.Threading;
using ColossalFramework.PlatformServices;
using static ColossalFramework.Plugins.PluginManager;
using ColossalFramework.Packaging;
using UnityEngine.Assertions;
using static UnityEngine.Debug;
using static UnityEngine.Assertions.Assert;

namespace HarmonyMod
{
    // [Serializable]
    internal class Loaded
    {
        public PluginInfo orig { get; private set; }
        public Loaded(PluginInfo p) { orig = p; }

        public void OnDownloaded(Item item)
        {
        }
        public void OnInstalled(Item item)
        {
            /* Reload workshop items manually. local items are uploaded automatically
             */
            if (orig.publishedFileID != PublishedFileId.invalid)
            {
                var LoadPluginAtPath = typeof(PluginManager).GetMethod("LoadPluginAtPath", BindingFlags.NonPublic | BindingFlags.Instance);
                LoadPluginAtPath?.Invoke(Singleton<PluginManager>.instance, new object[] { orig.modPath, false, orig.publishedFileID });
            }
        }

        public override string ToString()
        {
#if DEBUG
            return orig.name + " (" + orig.modPath + ")";
#else
            return orig.name;
#endif
        }
    }
}
