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

/* Borrowed from Traffic Manager - thank you! */
namespace HarmonyMod
{
    using System.IO;
    using System.Reflection;
    using System;
    using UnityEngine;

    public static class TextureResources {
        static TextureResources() {
        }

        internal static Texture2D LoadDllResource(string resourceName, int width, int height)
        {
            try {
#if DEBUG
            UnityEngine.Debug.LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] Loading DllResource {resourceName})");
#endif
                var myAssembly = Assembly.GetExecutingAssembly();
                var fullname = typeof(TextureResources).Namespace + ".Resources." + resourceName;
                var myStream = myAssembly.GetManifestResourceStream(fullname);
                if (myStream == null)
                    throw new Exception($"{fullname} not found!");

                var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);

                texture.LoadImage(ReadToEnd(myStream));

                return texture;
            } catch (Exception e) {
                UnityEngine.Debug.LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] Failed Loading DllResource: {Report.ExMessage(e, true)}");
                return null;
            }
        }

        static byte[] ReadToEnd(Stream stream)
        {
            var originalPosition = stream.Position;
            stream.Position = 0;

            try
            {
                var readBuffer = new byte[4096];

                var totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead != readBuffer.Length)
                        continue;

                    var nextByte = stream.ReadByte();
                    if (nextByte == -1)
                        continue;

                    var temp = new byte[readBuffer.Length * 2];
                    Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                    Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                    readBuffer = temp;
                    totalBytesRead++;
                }

                var buffer = readBuffer;
                if (readBuffer.Length == totalBytesRead)
                    return buffer;

                buffer = new byte[totalBytesRead];
                Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                return buffer;
            }
            catch (Exception e) {
                UnityEngine.Debug.LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] Failed Reading stream: {Report.ExMessage(e, true)}");
                return null;
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }
    }
}