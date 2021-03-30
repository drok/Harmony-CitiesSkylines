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

        public override string authorID { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
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
                Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - RetryWait yields {m_resume.Current.GetType()}");
                yield return m_resume.Current;
            }
            Debug.Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - RetryWait finished");
            //            release.Enqueue(m_work.destination, m_work.item, m_work.update);
        }

        public override void Install(Loaded requiredBy, bool update)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            // var delay = m_at - (Int32)DateTimeOffset.UtcNow.ToUniversalTime().Subtract(new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds;
            var delay = m_at - DateTimeOffset.UtcNow;

            return "retry " + m_work.item + " in " + delay + "s";
        }

        public static DownloadManager.Work GetRetryWork(DateTimeOffset at, DownloadManager.Work work, IEnumerator<YieldInstruction> next)
        {
            if (work.item is RetryWait again)
            {
                again.m_at = at;
                // again.m_resume = next;
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
