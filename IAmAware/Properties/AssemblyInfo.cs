using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("IAmAware")]
[assembly: AssemblyDescription("Internal Awareness Interface for the Harmony Mod")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Radu Hociung")]
[assembly: AssemblyProduct("IAmAware")]
[assembly: AssemblyCopyright("Copyright ©  2021")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
// [assembly: Guid("2a8bfcfe-dc32-4227-870c-addb18858608")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("0.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// MonoMod.Common uses IgnoresAccessChecksTo on its end,
// but older versions of the .NET runtime bundled with older versions of Windows
// require Harmony to expose its internals instead.
// This is only relevant for when MonoMod.Common gets merged into Harmony.
[assembly: InternalsVisibleTo("HarmonyMod.Tests, PublicKey=" +
	"0024000004800000940000000602000000240000525341310004000001000100" +
	"e9f6f326593be181e1d4fea8ba7d991fc9ff3e7adf8ee659550cd00e34673409d5e177bab53f08" +
	"4410455066e2a05864973a0b91b4fd6f827f6d0c70db0299db5f7d95429418e0e58a519838ceda" +
	"4ad16caf832a9da9feac59c8ea78a37f8e22c85058e544801972d98c1ad999e6aa09374cb69606" +
	"7a66ae7b154d0e616ca0b0")]

/* Allow HarmonyMods to communicate with each other
 */
[assembly: InternalsVisibleTo("HarmonyMod, PublicKey=" +
	"0024000004800000940000000602000000240000525341310004000001000100e9f6f326593be1" +
	"81e1d4fea8ba7d991fc9ff3e7adf8ee659550cd00e34673409d5e177bab53f084410455066e2a0" +
	"5864973a0b91b4fd6f827f6d0c70db0299db5f7d95429418e0e58a519838ceda4ad16caf832a9d" +
	"a9feac59c8ea78a37f8e22c85058e544801972d98c1ad999e6aa09374cb696067a66ae7b154d0e" +
	"616ca0b0")]

/* Allow HarmonyLib to query the Awareness interface (to find if it's first run)
 */
[assembly: InternalsVisibleTo("0Harmony, PublicKey=" +
	"0024000004800000940000000602000000240000525341310004000001000100e9f6f326593be1" +
	"81e1d4fea8ba7d991fc9ff3e7adf8ee659550cd00e34673409d5e177bab53f084410455066e2a0" +
	"5864973a0b91b4fd6f827f6d0c70db0299db5f7d95429418e0e58a519838ceda4ad16caf832a9d" +
	"a9feac59c8ea78a37f8e22c85058e544801972d98c1ad999e6aa09374cb696067a66ae7b154d0e" +
	"616ca0b0")]

/* Allow CitiesHarmony.Harmony to query the Awareness interface (to find if it's first run)
 */
[assembly: InternalsVisibleTo("CitiesHarmony.Harmony, PublicKey=" +
	"0024000004800000940000000602000000240000525341310004000001000100e9f6f326593be1" +
	"81e1d4fea8ba7d991fc9ff3e7adf8ee659550cd00e34673409d5e177bab53f084410455066e2a0" +
	"5864973a0b91b4fd6f827f6d0c70db0299db5f7d95429418e0e58a519838ceda4ad16caf832a9d" +
	"a9feac59c8ea78a37f8e22c85058e544801972d98c1ad999e6aa09374cb696067a66ae7b154d0e" +
	"616ca0b0")]

/* Allow the API to query the Awareness interface, to send the mod callbacks */
[assembly: InternalsVisibleTo("Harmony-CitiesSkylines, PublicKey=" +
	"0024000004800000940000000602000000240000525341310004000001000100" +
	"e9f6f326593be181e1d4fea8ba7d991fc9ff3e7adf8ee659550cd00e34673409d5e177bab53f08" +
	"4410455066e2a05864973a0b91b4fd6f827f6d0c70db0299db5f7d95429418e0e58a519838ceda" +
	"4ad16caf832a9da9feac59c8ea78a37f8e22c85058e544801972d98c1ad999e6aa09374cb69606" +
	"7a66ae7b154d0e616ca0b0")]
