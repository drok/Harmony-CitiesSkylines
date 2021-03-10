using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyVersion(HarmonyMod.Versioning.MyAssemblyVersion)]
[assembly: AssemblyFileVersion(HarmonyMod.Versioning.MyFileVersion)]
[assembly: AssemblyInformationalVersionAttribute(HarmonyMod.Versioning.MyInformationalVersion)]

namespace HarmonyMod
{
    public static class Versioning
    {
        public const string ReleaseStr = "0";
        public const uint ImplementationVersion = 0x00090000;
        public const string MyAssemblyVersion = "1.0.0." + ReleaseStr + ReleaseTypeStr;
        public const string MyFileVersion = "0.9";
        public const string PACKAGE_NAME = "Harmony";

        public struct Obsolescence
        {
            /* Versions at which various features will be disabled.
             * They should not be removed from the source code, because
             * they serve as documentation for how older versions worked.
             * The compiler with automatically remove the dead code.
             */
            public const uint AUTO_MOD_ENABLE = 0x02000000;

            /* Auto-helper-install (one time) is actually a good feature,
             * should probably never be obsoleted
             */
            public const uint AUTO_INSTALL_HELPER = 0x03000000;
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
            bool obsolete = ImplementationVersion >= ver;
            if (!obsolete)
            {
                UnityEngine.Debug.LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] WARNING '{explanation}' will be removed at release {VersionString(ver)}");
            }
            return obsolete;
        }

        public const string MyInformationalVersion = MyFileVersion + POSTFIX;
        public const string FULL_PACKAGE_NAME = PACKAGE_NAME + " " + MyInformationalVersion;

#if DEBUG
        public const string ReleaseTypeStr = "5";
#elif DEVELOPER_UPDATER
        /* Note the DEVELOPER_UPDATER is also a DEVELOPER
         * so its conditional must be first
         */
        public const string ReleaseTypeStr = "3";
#elif DEVELOPER
        public const string ReleaseTypeStr = "4";
#elif BETA
        public const string ReleaseTypeStr = "2";
#elif RELEASE
        public const string ReleaseTypeStr = "1";
#else
        public const string ReleaseTypeStr = "0";
#endif

#if DEBUG
        const string POSTFIX = "-DEBUG";
#elif DEVELOPER
        /* The Developer edition includes auto-installing the HELPER, and
         * tracking if it loads first. This is needed to install the
         * Exception handler early to capture *all* errors.
         * It also includes more exception analysis than the other editions.
         */
        const string POSTFIX = "-DEV";
#elif DEVELOPER_UPDATER
        /* The Developer Updater is the same as Developer,
         * but it has a lower version number.
         * When the Workshop Developer is updated, this
         * edition will be the active one instead of the local
         * Developer, and will update the local Developer.
         * Then, the local Developer will be higher versioned and
         * have precedence.
         */
        const string POSTFIX = "-DEV";
#elif BETA
        const string POSTFIX = "-BETA";
#else
        const string POSTFIX = "";
#endif
    }
}

