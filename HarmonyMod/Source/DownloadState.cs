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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static UnityEngine.Debug;
#if UPDATER
using UpdateFromGitHub;
#elif INSTALLER
using HarmonyInstaller;
#endif

namespace HarmonyMod
{
    class DownloadState : IReleaseInfo
    {
        public DownloadManager downloadManager { get; private set; }

        IEnumerable<string> m_fileList;
        public uint retryCount;

        public DownloadState(DownloadManager manager)
        {
            downloadManager = manager;
            m_fileList = null;
            packageInfo = new Hashtable(){
                        { "i", Versioning.ImplementationVersion }, };

        }
        public Hashtable packageInfo { get; set; }
        public IEnumerable<string> files
        {
            get { return m_fileList; }
            set
            {
                if (m_fileList == null || value == null)
                    m_fileList = value;
                else
                    m_fileList = m_fileList.Concat(value);
            }
        }

        string m_destDir;
        public string destDir
        {
            get { return m_destDir; }
            set
            {
                m_destDir = value;
            }
        }

        public void DownloadSuccess(Loaded mod, Item item, Hashtable release, DownloadState dl)
        {
            mod?.OnDownloaded(item);
            bool installed = Mod.repo.OnSuccessfulDownload(mod, item, release, dl);

            if (installed)
                mod?.OnInstalled(item);

        }

    }
}
