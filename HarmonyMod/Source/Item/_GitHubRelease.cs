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
using System.Collections;
using System.Linq;

namespace GitHub
{
    internal static class _GitHubRelease
    {
        public const string REL_ASSETS = "assets";
        public const string REL_TAG_NAME = "tag_name";
        public const string REL_TARGET = "target_commitish";
        public const string REL_ID = "id";
        public const string REL_PUBLISHED_AT = "published_at";
        public const string REL_HTML_URL = "html_url";
        public const string REL_NAME = "name";
        public const string REL_AUTHOR = "author";

        public const string AUTHOR_ID = "id";

        /* members used in GitHub release data */
        public const string ASSET_ID = "id";
        public const string ASSET_FILENAME = "name";
        public const string ASSET_LABEL = "label";
        public const string ASSET_CONTENT_TYPE = "content_type";
        public const string ASSET_DOWNLOAD_URL = "browser_download_url";

        public static Dictionary<string, IDictionary> StoredTags = new Dictionary<string, IDictionary>() {
            { REL_ASSETS, new Dictionary<string, IDictionary> {
                { ASSET_ID, null },
                { ASSET_CONTENT_TYPE, null },
                { ASSET_DOWNLOAD_URL, null },
                { ASSET_LABEL, null },
                { ASSET_FILENAME, null }, } },
            { REL_TAG_NAME, null },
            { REL_TARGET, null },
            { REL_ID, null },
            { REL_PUBLISHED_AT, null },
            { REL_HTML_URL, null },
            { REL_NAME, null },
            { REL_AUTHOR, new Dictionary<string, IDictionary>{
                { AUTHOR_ID, null } } },
        };
        public static Hashtable Filter(Hashtable h, Dictionary<string, IDictionary> tags)
        {
            return tags.Aggregate(new Hashtable(), (acc, tag) => {

            if (h[tag.Key] is object o)
            {
                    if (tag.Value != null)
                    {
                        if (o is Hashtable t)
                            acc.Add(tag.Key, Filter(t, tag.Value as Dictionary<string, IDictionary>));
                        else if (o is ArrayList a)
                            acc.Add(tag.Key, a.ToArray().Aggregate(new ArrayList(), (oAcc, av) =>
                            {
                                if (av is Hashtable avhash)
                                    oAcc.Add(Filter(avhash, tag.Value as Dictionary<string, IDictionary>));
                                return oAcc;
                            }));
                    }
                    else 
                        acc.Add(tag.Key, o);
                }
                return acc;
            });
        }

    }

}
