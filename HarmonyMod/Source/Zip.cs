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
#if TRACE
            Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] Unzipping {zipName} to {destDir}");
#endif
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
#if TRACE
                        Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] Found in zip: {zipEntry.Name}");
#endif
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
#if TRACE
                        Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] Calculated Path: {dest} (zippedDirectory={zippedDirectory} zippedDirectoryName={zippedDirectoryName}");
#endif
                        if (zipEntry.IsDirectory)
                        {
#if TRACE
                            Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] Should create Dir {dest}");
#endif
                            Directory.CreateDirectory(dest);
                        }
                        else
                        {
#if TRACE
                            Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] Should unzip {zipName} to {destDir} => {dest}");
#endif
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
