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
extern alias HarmonyCHH2040;

using static UnityEngine.Debug;
using static UnityEngine.Assertions.Assert;
using System.Reflection;
using System.Linq.Expressions;

namespace HarmonyMod
{
    using IAwareness;
    using Harmony2::HarmonyLib;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Diagnostics;
    using System.Linq;

    internal class Patcher
    {
        const string HARMONY_ID = "org.ohmi.harmony";

        bool initialized_ = false;

        readonly string id;
        readonly Harmony harmony;
        /* separate instance for patching harmony1; these patches will not be
         * removed when the HarmonyMod unloads, because it doesn't unload last yet
         * and other mods still need their 2.x access though 1.2.0.1 api
         */
        readonly string compat_id;
        readonly bool foundUnsupportedHarmonyLib = false;

        IAmAware self;

        internal static Dictionary<Version, Assembly> harmonyAssembly = new Dictionary<Version, Assembly>();
        internal static List<Type> myLegacyHarmonies = new List<Type>();
        internal static List<Assembly> myLegacyHarmonyMods = new List<Assembly>();
        Version legacyHarmonyModVersion = new Version(1, 0, 1, 0);


        internal Patcher(IAmAware selfMod, string name, bool onAwarenessCallback = false)
        {
            id = HARMONY_ID + (name != null ? "+" + name : string.Empty);
            compat_id = id + "+Compat";
#if DEBUG
            Harmony.DEBUG = true;
#endif

            self = selfMod;

            EnableHarmony();
            harmony = new Harmony(id);

            if (!Harmony.Harmony1Patched)
            {
                Harmony.Harmony1Patched = true;
                try
                {
                    ImplementAdditionalVersionSupport();
                }
                catch (HarmonyModSupportException ex)
                {
                    foundUnsupportedHarmonyLib = true;
                    DisableHarmony();
                    (self as Mod).report.ReportUnsupportedHarmony(ex);
                }
            }
        }

        bool EnableHarmony()
        {
            bool wasInitialized = Harmony.m_enabled.HasValue;
            Harmony.isEnabled = true;
            if (self != null)
            {
                Harmony.awarenessInstance = self;
                HarmonyCHH2040::HarmonyLib.Harmony.awarenessInstance = self;
                foreach (var h in myLegacyHarmonies)
                    h.GetField("awarenessInstance", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(null, self);
            }
            HarmonyCHH2040::HarmonyLib.Harmony.isEnabled = true;
            if (!Harmony.harmonyUsers.ContainsKey(Assembly.GetExecutingAssembly().FullName)) {
                Harmony.harmonyUsers[Assembly.GetExecutingAssembly().FullName] = new Harmony.HarmonyUser() { checkBeforeUse = true, legacyCaller = false, instancesCreated = 0, };
            }
            foreach (var h in myLegacyHarmonies)
            {
                h.GetProperty("isEnabled", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.SetProperty)?.SetValue(null, true, null);
            }

            return wasInitialized;
        }

        void DisableHarmony()
        {
            Harmony.isEnabled = false;
            HarmonyCHH2040::HarmonyLib.Harmony.isEnabled = false;
            foreach (var h in myLegacyHarmonies)
            {
                h.GetProperty("isEnabled", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.SetProperty)?.SetValue(null, false, null);
            }
        }

        internal bool Install()
        {
            IsFalse(initialized_,
                "Should not call Patcher.Install() more than once");
            try
            {
                EnableHarmony();
            }
            catch (Exception ex)
            {
                /* This happens if another mod has a copy of 0Harmony.dll with version=2.0.4.0 (same Version as my own used), which gets loaded instead of mine. */
                self.SelfReport(SelfProblemType.WrongHarmonyLib, ex);
            }

            try
            {
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                if (harmonyAssembly.Count != 0)
                {
                    /* If the previous PatchAll did the state transfer, now do the other patches */
                    harmonyAssembly.Clear();
                    harmony.PatchAll(Assembly.GetExecutingAssembly());
                }

                if (foundUnsupportedHarmonyLib)
                    DisableHarmony();

                initialized_ = true;
            }
            catch (Exception e)
            {
                Mod.SelfReport(SelfProblemType.OwnPatchInstallation, e);
            }

            return initialized_;
        }

        internal void Uninstall()
        {
            IsTrue(initialized_,
                "Should not call Patcher.Uninstall() more than once");
            IsNotNull(harmony, "HarmonyInst != null");
            try
            {
                if (foundUnsupportedHarmonyLib)
                    EnableHarmony();

                harmony.UnpatchAll(id);

                /* FIXME: When harmonymod is removed last, it's
                 * safe to also remove Harmon1 patches.
                 *
                 * compatHarmony.UnpatchAll(compat_id);
                 */
                if (foundUnsupportedHarmonyLib)
                    DisableHarmony();
            }
            catch (Exception e)
            {
                Mod.SelfReport(SelfProblemType.OwnPatchRemoval, e);
            }

            initialized_ = false;
        }

        internal void UninstallAll()
        {
            IsTrue(Harmony.isEnabled,
                "Harmony should be enabled before UninstallAll");

            /* FIXME - implement disabling Harmony should remove all dependent mods' patches */
            // harmony.UnpatchAll();
        }

        internal void ImplementAdditionalVersionSupport()
        {
            /* FIXME: Move this table to the API, so mods
             * can query for support at runtime.
             */
            Version[] harmonyVersionSupport = new Version[]
            {
                // new Version(1, 0, 9, 1), I don't have a transfer function ready for this,
                // and I don't think it is in use anywhere.
                new Version(1, 1, 0, 0),
                new Version(1, 2, 0, 1),
            };

            /* Official support cut-off. Above this version, I will implement support.
             * Below this version, you need to implement the support and submit a
             * pull request; see below
             */
            Version minSupportedVersion = new Version(2, 0, 0, 0);

            List<HarmonyModSupportException.UnsupportedAssembly> unsupportedAssemblies = new List<HarmonyModSupportException.UnsupportedAssembly>();

            /* Enable the main Library to enable state transition, but
             * disable it again if transition failed or unsupported harmony libs exist
             * 
             */
            EnableHarmony();
            int failures = 0;

            /* FIXME: This should be done in order of decreasing Version */
            AppDomain.CurrentDomain.GetAssemblies()
                .Do((assembly) =>
                {
                    var your = assembly.GetName();

                    if (your.Name == "0Harmony" &&
                        (your.GetPublicKeyToken() == null ||
                        !your.GetPublicKeyToken().SequenceEqual(Assembly.GetExecutingAssembly().GetName().GetPublicKeyToken())))
                    {
                        if (Array.Exists(harmonyVersionSupport, (supported) => supported == your.Version))
                        {
                            harmonyAssembly[your.Version] = assembly;
                        }
                        else if (your.Version < minSupportedVersion)
                        {
                            /* If you are a mod developer and came here due to the exception below,
                             * it is likely because you're trying to use a version older than 2.0.1.0
                             * which is not yet supported. You need to submit a pull-request to
                             * the HarmonyMod which implements the necesary State Transfer and
                             * runtime compatibility patch, or alternately a stub for your version.
                             */
                            unsupportedAssemblies.Add(new HarmonyModSupportException.UnsupportedAssembly(assembly, true));
                        }
                        else
                        {
                            /* Other mods are not allowed to supply their own Harmony 2 libs
                             * because it would bypass the Patching ACL mechanics, as well as
                             * the Awareness mechanic of patching v1 libs regardless of load order
                             * 
                             * TODO: Disable mods that try.
                             */
                            unsupportedAssemblies.Add(new HarmonyModSupportException.UnsupportedAssembly(assembly, false));
                        }
                    }
                    else if (your.Name == "0Harmony" || your.Name == "CitiesHarmony.Harmony")
                    {
                        if (assembly != typeof(Harmony2::HarmonyLib.Harmony).Assembly &&
                        assembly != typeof(HarmonyCHH2040::HarmonyLib.Harmony).Assembly &&
                        your.GetPublicKeyToken().SequenceEqual(Assembly.GetExecutingAssembly().GetName().GetPublicKeyToken()))
                        {
                            if (assembly.GetType("HarmonyLib.Harmony") is Type harmonyType)
                                myLegacyHarmonies.Add(harmonyType);
                        }
                    }
                    else if (your.Name == "HarmonyMod")
                    {
                        if (your.Version < legacyHarmonyModVersion &&
                            your.GetPublicKeyToken().SequenceEqual(Assembly.GetExecutingAssembly().GetName().GetPublicKeyToken()))
                        {
                            myLegacyHarmonyMods.Add(assembly);
                        }
                    }
                });

            if (failures != 0 || unsupportedAssemblies.Count != 0)
            {
                DisableHarmony();
            }

            if (unsupportedAssemblies.Count != 0)
            {
                Harmony.unsupportedException = new HarmonyModSupportException(unsupportedAssemblies);
                throw Harmony.unsupportedException;
            }
        }
        internal static bool isHarmonyUserException(Exception e)
        {
            return e is HarmonyUserException ||
                e is HarmonyCHH2040::HarmonyLib.HarmonyUserException;
        }
        internal static Harmony2::HarmonyLib.Harmony CreateClientHarmony(string harmonyId)
        {
            var stack = new StackTrace();
            var lastCaller = stack.GetFrame(0).GetMethod();
            MethodBase caller = lastCaller;
            int assemblyDepth = 0;
            SameAssemblyName assemblyComparator = new SameAssemblyName(SameAssemblyName.VersionComparison.Exact, false, true, true);
            /* Search in the stack for the assembly that called
             * my caller(0Harmony 1.x)
             */
            for (int i = 1; i < stack.FrameCount && assemblyDepth < 2; ++i)
            {
                caller = stack.GetFrame(i).GetMethod();
                if (!assemblyComparator.Equals(lastCaller.DeclaringType.Assembly.GetName(), caller.DeclaringType.Assembly.GetName()))
                {
                    lastCaller = caller;
                    ++assemblyDepth;
                }
            }

            if (!Harmony.harmonyUsers.TryGetValue(caller.DeclaringType.Assembly.FullName, out var userStatus))
            {
                Harmony.harmonyUsers[caller.DeclaringType.Assembly.FullName] = new Harmony.HarmonyUser() { checkBeforeUse = false, legacyCaller = true, instancesCreated = 0, };
            }
            return new Harmony(harmonyId, caller);
        }
        internal static Harmony CreateClientHarmony(object oldHarmony)
        {
            return CreateClientHarmony(oldHarmony.GetType().GetField("id", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(oldHarmony) as string);
        }

    }
}
