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
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HarmonyMod
{
    class RetryWait : Item
    {
        DateTimeOffset m_at;
        readonly IEnumerator<YieldInstruction> m_resume;
        readonly DownloadManager.Work m_work;

        public double delay => (m_at - DateTimeOffset.UtcNow).TotalSeconds;

        public RetryWait(DateTimeOffset at, DownloadManager.Work work, IEnumerator<YieldInstruction> next)
            : base()
        {
            m_at = at;
            m_work = work;
            m_resume = next;
        }

        public override string authorName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override string typeID => throw new NotImplementedException();

        public override string repo => throw new NotImplementedException();

        public override string host => throw new NotImplementedException();

        public override string title { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override string version { get => throw new NotImplementedException(); protected set => throw new NotImplementedException(); }
        public override DateTimeOffset? updated { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override string myItemBranch => throw new NotImplementedException();
        public override string myReleaseBranch => throw new NotImplementedException();

        public override string myBranchAsDirectory => throw new NotImplementedException();

        public override IEnumerator<YieldInstruction> FetchReleaseInfo(bool update, Loaded destination, DownloadManager downloadManager)
        {
            var delay = m_at - DateTimeOffset.UtcNow;
            Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Will retry {m_work} in {delay.TotalSeconds}s m_resume={m_resume.Current.GetType()}");
            if ((int)delay.TotalSeconds > 0)
                yield return new WaitForSeconds((int)delay.TotalSeconds);

            while (m_resume.MoveNext())
            {
                yield return m_resume.Current;
            }
        }

        public override void Install(Loaded requiredBy, bool update)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            var delay = m_at - DateTimeOffset.UtcNow;

            return "retry " + m_work.item + " in " + delay + "s";
        }

        public static DownloadManager.Work GetRetryWork(DateTimeOffset at, DownloadManager.Work work, IEnumerator<YieldInstruction> next)
        {
            if (work.item is RetryWait again)
            {
                again.m_at = at;
                return work;
            }
            else
                return new DownloadManager.Work()
                {
                    item = new RetryWait(at, work, next),
                };
        }

        public override void OnDownloadFailed(Loaded requestedBy, bool update, DownloadManager downloadManager, DownloadError error)
        {
            m_work.item.OnDownloadFailed(requestedBy, update, downloadManager, error);
        }
    }
}
