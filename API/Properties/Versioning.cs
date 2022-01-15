using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if LABS || DEBUG
[assembly: AssemblyVersion("0.0.0.0")]
#else
[assembly: AssemblyVersion("0.0.0.0")]
[assembly: AssemblyFileVersion(HarmonyManager.Versioning.MyFileVersion)]
[assembly: AssemblyInformationalVersionAttribute("0.9.0")]
#endif

namespace HarmonyManager
{
    internal static class Versioning
    {
        public const string PACKAGE_NAME = "Harmony.API";
#if RELEASE
        public const string HarmonyGithubDistributionURL = "drok/Harmony-CitiesSkylines";
#else
        public const string HarmonyGithubDistributionURL = "drok/reltest";
#endif
        public const string ISSUES_URL = "github.com/" + HarmonyGithubDistributionURL + "/issues";
        public const string MyFileVersion = "0.9.0";
        public const string MyInformationalVersion = MyFileVersion + POSTFIX;
        public const string FULL_PACKAGE_NAME = PACKAGE_NAME + " " + MyInformationalVersion;
        public const uint MyVersionNumber = 0x00090000;
        public const string HarmonyInstallDir = "000-HarmonyModManager";
        /// <summary>
        /// Install this specific Harmony Release if necessary. This bootstrap version
        /// has been tested with this API. It will update itself as it sees fit.
        /// </summary>
        public const string HARMONY_RELEASE_TAG = "v2.1";

        public struct Obsolescence
        {
            /* Versions at which various features will be disabled.
             * They should not be removed from the source code, because
             * they serve as documentation for how older versions worked.
             * The compiler with automatically remove the dead code.
             */
        }

        public static string VersionString(uint number)
        {
            return new System.Version(
                (int)((number >> 24) & 0xff),
                (int)((number >> 16) & 0xff),
                (int)((number >> 8) & 0xff),
                (int)(number & 0xff)).ToString();
        }

        public static bool IsObsolete(uint ver, string explanation)
        {
            bool obsolete = MyVersionNumber >= ver;
            if (!obsolete)
            {
                UnityEngine.Debug.LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] WARNING '{explanation}' will be removed at release {VersionString(ver)}");
            }
            return obsolete;
        }

#if DEBUG
        const string POSTFIX = "-DEBUG";
#elif BETA
        const string POSTFIX = "-BETA";
#else
        const string POSTFIX = "";
#endif
    }
}

