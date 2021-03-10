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

/* Borrowed from original CitiesHarmony mod by boformer - thank you! */
using System;
using System.Reflection;

namespace HarmonyMod {
    public static class TypeExtensions {
        public static FieldInfo GetFieldOrThrow(this Type type, string name) {
            return type?.GetField(name) ?? throw new Exception($"{name} field not found");
        }

        public static FieldInfo GetFieldOrThrow(this Type type, string name, BindingFlags flags) {
            return type?.GetField(name, flags) ?? throw new Exception($"{name} field not found");
        }

        public static MethodInfo GetMethodOrThrow(this Type type, string name) {
            return type?.GetMethod(name) ?? throw new Exception($"{name} method not found");
        }

        public static MethodInfo GetMethodOrThrow(this Type type, string name, BindingFlags flags) {
            return type?.GetMethod(name, flags) ?? throw new Exception($"{name} method not found");
        }

        public static MethodInfo GetMethodOrThrow(this Type type, string name, Type[] types) {
            return type?.GetMethod(name, types) ?? throw new Exception($"{name} method not found");
        }

        public static Version GetAssemblyVersion(this Type type) {
            return type.Assembly.GetName().Version;
        }

        public static string Max(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength-1) + 'Û';
        }
    }
}
