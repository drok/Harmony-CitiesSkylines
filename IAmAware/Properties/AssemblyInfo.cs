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
[assembly: AssemblyVersion("0.0.1.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

/* Allow access to previous versions for interface upgrades */
[assembly: InternalsVisibleTo("IAmAware, PublicKey=" +
	"0024000004800000940000000602000000240000525341310004000001000100" +
	"e9f6f326593be181e1d4fea8ba7d991fc9ff3e7adf8ee659550cd00e34673409d5e177bab53f08" +
	"4410455066e2a05864973a0b91b4fd6f827f6d0c70db0299db5f7d95429418e0e58a519838ceda" +
	"4ad16caf832a9da9feac59c8ea78a37f8e22c85058e544801972d98c1ad999e6aa09374cb69606" +
	"7a66ae7b154d0e616ca0b0")]

/* Allow integration tests unrestricted access for testing */
[assembly: InternalsVisibleTo("Test.Harmony, PublicKey=" +
	"00240000048000009400000006020000002400005253413100040000010001009d0f13cde5b126" +
	"c67d0c94873430cc171f8919863c6218a5bc1788a91caf6c197a851fdd4e5df5fe68726b5ca92a" +
	"cd2a47770cde3eb1538693a427a6c7591878b59dacc8fd24339f0e77f923ada3f80133f3a5b182" +
	"d7d04b16fb7bd02abff840b4b4ed9114463fef35c3437385205ebed7906a29ce6bd16a84e50129" +
	"8c8224ba")]

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
