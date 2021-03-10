using CitiesHarmony.API;
using ICities;
// Make sure that "using HarmonyLib;" does not appear here!
// Only reference HarmonyLib in code that runs when Harmony is ready (DoOnHarmonyReady, IsHarmonyInstalled)

namespace BadMods {
    public class PatchTooEarlyMod : IUserMod {        
        // You can add Harmony 2.0.0.9 as a dependency, but make sure that 0Harmony.dll is not copied to the output directory!
        // (0Harmony.dll is provided by CitiesHarmony workshop item)

        // Also make sure that HarmonyLib is not referenced in any way in your IUserMod implementation!
        // Instead, apply your patches from a separate static patcher class!
        // (otherwise it will fail to instantiate the type when CitiesHarmony is not installed)

        public string Name => $"{GetType().Namespace}: {GetType().Name}";
        public string Description => "Patches SimulationManager.CreateRelay and LoadingManager.MetaDataLoaded";

        Patcher patcher = new Patcher();
        public void OnEnabled() {
            HarmonyHelper.DoOnHarmonyReady(() => patcher.PatchAll());
        }

        public void OnDisabled() {
            if (HarmonyHelper.IsHarmonyInstalled) patcher.UnpatchAll();
        }
    }
}

