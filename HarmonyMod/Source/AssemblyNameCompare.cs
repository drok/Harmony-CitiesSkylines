/*
 * Harmony for Cities Skylines
 *  Copyright (C) 2021 Radu Hociung <radu.csmods@ohmi.org>
 *  
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *  
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *  
 *  You should have received a copy of the GNU General Public License along
 *  with this program; if not, write to the Free Software Foundation, Inc.,
 *  51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System.Reflection;
using System.Collections.Generic;

namespace HarmonyMod
{

    class SameAssemblyName : IEqualityComparer<AssemblyName>
    {

        public int GetHashCode(AssemblyName n)
        {
            int hashcode = n.Name.GetHashCode();
            int keyhash = n.GetPublicKeyToken() == null ? 0 : n.GetPublicKeyToken().ToString().GetHashCode();
            hashcode += keyhash;
            hashcode += n.Version.GetHashCode();
            return hashcode;
        }
        public bool Equals(AssemblyName a, AssemblyName b)
        {
            bool result = a.Name.Equals(b.Name) &&
                a.Version.Equals(b.Version) &&
                ((a.GetPublicKeyToken() == null && b.GetPublicKeyToken() == null) ||
                a.GetPublicKeyToken().ToString() == b.GetPublicKeyToken().ToString()) &&
                a.CultureInfo.Equals(b.CultureInfo);
            return result;
        }

    }
}
