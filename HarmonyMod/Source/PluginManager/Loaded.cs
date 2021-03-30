extern alias Harmony2;
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
using static Harmony2::HarmonyLib.GeneralExtensions;

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
