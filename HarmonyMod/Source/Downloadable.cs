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
using UnityEngine;
using static UnityEngine.Debug;

namespace HarmonyMod
{
    public class Redirect : YieldInstruction
    {
        readonly string m_location;
        public string location => m_location;
        public Redirect(string loc) { m_location = loc; }
        public override string ToString()
        {
            return m_location;
        }
    }
    public class DownloadError : YieldInstruction
    {
        readonly string explanation;
        public DownloadError(string exp) { explanation = exp; }
        public override string ToString()
        {
            return $"'{explanation}'";
        }
    }

    internal class TemporaryError : YieldInstruction
    {
        DateTimeOffset m_waitTill;
        readonly string m_explanation;
        public TemporaryError(string explain, int z)
        {
            m_waitTill = Epoch.ToDateTimeOffset(z, (Int32)TimeSpan.Zero.TotalMinutes);
            m_explanation = explain;
        }

        public TemporaryError(string explain, TimeSpan span)
        {
            m_waitTill = DateTimeOffset.UtcNow.Add(span);
            m_explanation = explain;
        }
        public DateTimeOffset waitTill { get { return m_waitTill; } }

        public override string ToString()
        {
            return $"Temporary error '{m_explanation}' till {m_waitTill}";
        }
    }

    internal class Downloadable
    {
        public static string ACCEPT_JSON = "application/json";
        public static string ACCEPT_ANY = null;

        protected IEnumerator<YieldInstruction> DoNothing(string reason)
        {
#if HEAVY_TRACE
            Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - {this} Skipping {reason}");
#endif
            yield break;
        }

        // static public YieldInstruction temporaryError = new YieldInstruction();

        public virtual IEnumerator<YieldInstruction> FetchReleaseInfo(bool update, Loaded destination, DownloadManager downloadManager)
        {
            throw new NotImplementedException("Fetching release info for type " + GetType());
        }
    }
}
