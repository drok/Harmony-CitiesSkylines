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
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;

namespace HarmonyMod
{
    internal class Zip
    {
        byte[] zipFile;
        string zipName;

        public Zip(byte[] content, string name)
        {
            zipFile = content;
            zipName = name;
        }

        public IEnumerable<string> UnzipTo(string destDir)
        {
            bool zippedDirectory = false;
            string zippedDirectoryName = null;
            var fileList = new List<string>();

            using (var zipData = new MemoryStream(zipFile))
            {

                using (var inputStream = new ZipInputStream(zipData))
                {

                    /* Find out if all entries in the zip are under one subdir,
                        * so it can be removed from the hierarchy
                        */
                    while (inputStream.GetNextEntry() is ZipEntry zipEntry)
                    {
                        if (zipEntry.IsDirectory && !zippedDirectory)
                        {
                            zippedDirectory = true;
                            zippedDirectoryName = zipEntry.Name;
                        }
                        else
                        {
                            if (!zippedDirectory)
                            {
                                break;
                            }
                            else
                            {
                                if (!zipEntry.Name.StartsWith(zippedDirectoryName))
                                {
                                    zippedDirectory = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            using (var zipData = new MemoryStream(zipFile))
            {
                var buffer = new byte[4096];
                using (var inputStream = new ZipInputStream(zipData))
                {
                    while (inputStream.GetNextEntry() is ZipEntry zipEntry)
                    {
                        string dest;

                        if (zippedDirectory)
                        {
                            if (zipEntry.Name == zippedDirectoryName)
                            {
                                continue;
                            }
                            dest = Path.Combine(destDir, zipEntry.Name.Remove(0, zippedDirectoryName.Length));
                        }
                        else
                        {
                            dest = Path.Combine(destDir, zipEntry.Name);
                        }
                        if (zipEntry.IsDirectory)
                        {
                            Directory.CreateDirectory(dest);
                        }
                        else
                        {
                            fileList.Add(dest);
                            using (FileStream streamWriter = File.Create(dest))
                            {
                                StreamUtils.Copy(inputStream, streamWriter, buffer);
                            }
                            File.SetCreationTime(dest, zipEntry.DateTime);
                        }
                    }
                }
            }
            return fileList;
        }
    }
}
