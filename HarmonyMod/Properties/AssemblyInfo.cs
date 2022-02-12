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
using System.Runtime.CompilerServices;

[assembly: System.Reflection.AssemblyCompanyAttribute("Radu Hociung")]
[assembly: System.Reflection.AssemblyProductAttribute("Modding Infrastructure")]
[assembly: System.Reflection.AssemblyTitleAttribute(HarmonyMod.Versioning.PACKAGE_NAME)]

/* Allow integration tests unrestricted access for testing */
[assembly: InternalsVisibleTo("Test.Harmony, PublicKey=" +
	"00240000048000009400000006020000002400005253413100040000010001009d0f13cde5b126" +
	"c67d0c94873430cc171f8919863c6218a5bc1788a91caf6c197a851fdd4e5df5fe68726b5ca92a" +
	"cd2a47770cde3eb1538693a427a6c7591878b59dacc8fd24339f0e77f923ada3f80133f3a5b182" +
	"d7d04b16fb7bd02abff840b4b4ed9114463fef35c3437385205ebed7906a29ce6bd16a84e50129" +
	"8c8224ba")]
