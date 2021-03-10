using System.Runtime.CompilerServices;

[assembly: System.Reflection.AssemblyCompanyAttribute("Radu Hociung")]
[assembly: System.Reflection.AssemblyProductAttribute("Modding Infrastructure")]
[assembly: System.Reflection.AssemblyTitleAttribute("Harmony Mod for Cities Skylines")]

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
