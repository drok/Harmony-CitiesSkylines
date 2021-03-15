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
using System.Linq;

namespace HarmonyMod
{

    internal class SameAssemblyName : IEqualityComparer<AssemblyName>
    {

        readonly bool m_compareVersion;
        readonly bool m_compareToken;
        readonly bool m_compareName;
        readonly bool m_compareCulture;
        readonly bool m_satisfyStrongName;

        public SameAssemblyName()
        {
            m_compareName = true;
            m_compareToken = true;
            m_compareVersion = true;
            m_satisfyStrongName = false;
        }
        public SameAssemblyName(bool compareVersion, bool compareToken, bool compareName, bool compareCulture)
        {
            m_compareVersion = compareVersion;
            m_compareToken = compareToken;
            m_compareName = compareName;
            m_compareCulture = compareCulture;
            m_satisfyStrongName = true;
        }

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
            bool result = (!m_compareName || a.Name.Equals(b.Name)) &&
                (!m_compareVersion || a.Version.Equals(b.Version)) &&
                (!m_compareToken || (a.GetPublicKeyToken().Length == 0 && (!m_satisfyStrongName || b.GetPublicKeyToken().Length == 0)) ||
                (a.GetPublicKeyToken().SequenceEqual(b.GetPublicKeyToken()))) &&
                (!m_compareCulture || a.CultureInfo.Equals(b.CultureInfo));
            return result;
        }

    }
}
