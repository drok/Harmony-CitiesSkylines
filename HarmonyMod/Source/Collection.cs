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
using System.Collections;
using System.IO;
using System.Globalization;
using ColossalFramework;
using ColossalFramework.IO;
using ColossalFramework.PlatformServices;
using static Json.NETMF.JsonSerializer;
using static Json.NETMF.JsonParser;
using GitHub;
#if UPDATER
using UpdateFromGitHub;
#elif INSTALLER
using HarmonyInstaller;
#endif
#if TRACE || INSTALLER
using static UnityEngine.Debug;
#endif
#if !INSTALLER
using IAwareness;
#endif

namespace HarmonyMod
{
	internal class Collection
	{
		public string workingDir { get; private set; }
		public string tempDir { get; private set; }

		public Collection()
		{
			workingDir = DataLocation.modsPath;
			tempDir = DataLocation.tempFolder;
		}

		public enum CheckoutResult
		{
			OK = 0,

			/* Release is known, assets not downloaded */
			NeedAssets,

			/* Release is unknown. Should refresh releases */
			UnknownRelease,
		}

		/// <summary>
		/// Checks if the item is available in the local repo, and if not, find the Etag and release info needed to
		/// a. Check if newer releases were published (Etag), or,
		/// b. Download the assets for a known release (release)
		/// </summary>
		/// <param name="item">Which item to look up in the repo?</param>
		/// <param name="release">Output release information if a match was found in the local repo</param>
		/// <param name="update">If true, it always returns UnknownRelease and Etag if known. If false, can also return OK or NeedAssets when appropriate</param>
		/// <param name="Etag">The HTTP Etag returned by the server the last time releases were checked. Needed to avoid rate-limiting</param>
		/// <returns>UnknownRelease if the requested version is not in the repo, or if update was requested
		/// OK if a matching version was found.
		/// NeedAssets if the requested release is known, but it's missing the assets</returns>
		public CheckoutResult PrepareToDownload(Loaded mod, GitHubRelease item, bool update, out Hashtable release)
		{
			if (!update && item.Latest.value != string.Empty)
			{
				SavedString savedRelease = new SavedString(item.myReleaseBranch + "/release", Settings.userGameState, string.Empty, true);

				var data = JsonDecode(savedRelease.value) as Hashtable;
				/* FIXME:
				 * The stored data needs to be checked for integrity, reliably
				 */
				if (savedRelease.exists)
				{
					if ((long)data[_Item.RELEASE_DATA_ID_VERSION] != Versioning.StorageVersion)
					{
						savedRelease.Delete();
						release = null;
					}
					else
					{
						release = data[_Item.RELEASE_DATA_ID_RELEASE] as Hashtable;
						string installedFlag = Path.Combine(mod.orig.modPath, $".ghrel-{release[_GitHubRelease.REL_ID]}");

						return File.Exists(installedFlag) ? CheckoutResult.OK : CheckoutResult.NeedAssets;
					}
				}
				else
					release = null;
			} else
			{
				release = null;
			}
			return CheckoutResult.UnknownRelease;
		}
		void Touch(string fileName)
        {
			if (!File.Exists(fileName))
				File.Create(fileName).Close();

			File.SetLastWriteTimeUtc(fileName, DateTime.UtcNow);
		}

		public bool StoreGithubReleaseInfo(IEnumerable releases, GitHubRelease item)
		{
			{
				bool complete = false;
				SavedString repoUpdated = new SavedString(item.myItemBranch + "/updated", Settings.userGameState, string.Empty, true);
				DateTimeOffset lastRepoUpdate = (repoUpdated.value != string.Empty) ?
					DateTimeOffset.Parse(repoUpdated.value) : default(DateTimeOffset);

				foreach (Hashtable release in releases)
				{
					var branch = release[_GitHubRelease.REL_TARGET] as string;
					var releaseName = item.myItemBranch + "/" + branch;
					if (item.Latest.exists && item.Latest.value == (release[_GitHubRelease.REL_HTML_URL] as string))
					{
						complete = true;
						break;
					}

					SavedString branchUpdated = new SavedString(releaseName + "/updated", Settings.userGameState, string.Empty, true);
					DateTimeOffset lastBranchUpdate = (branchUpdated.value != string.Empty) ?
						DateTimeOffset.Parse(branchUpdated.value) : default(DateTimeOffset);
					var published_at = DateTimeOffset.Parse(release[_GitHubRelease.REL_PUBLISHED_AT] as string);

					if (!repoUpdated.exists || lastRepoUpdate < published_at)
					{
						lastRepoUpdate = published_at;
						repoUpdated.value = published_at.ToString(CultureInfo.InvariantCulture);
					}

					if (!branchUpdated.exists || lastBranchUpdate < published_at)
					{
						lastBranchUpdate = published_at;

						/* Create empty commit with release details, and no assets */
						SavedString latestRelease = new SavedString(releaseName + "/release", Settings.userGameState, string.Empty, true);
						var archivedRelease = new Hashtable()
						{
							{ _Item.RELEASE_DATA_ID_VERSION, Versioning.StorageVersion },
							{ _Item.RELEASE_DATA_ID_RELEASE, _GitHubRelease.Filter(release, _GitHubRelease.StoredTags)},
						};
						latestRelease.value = SerializeObject(archivedRelease);

						SavedString latestUpdate = new SavedString(releaseName + "/updated", Settings.userGameState, string.Empty, true);
						latestUpdate.value = published_at.ToString(CultureInfo.InvariantCulture);
					}
					else
					{
						complete = true;
						break;
					}
				}

				return complete;
			}
		}

		public bool OnSuccessfulDownload(Loaded mod, Item item, Hashtable release, DownloadState dl)
		{
			var modHome = mod.orig.modPath;
			bool needsBackup = Directory.Exists(modHome);
			bool failed = false;
			string bakdir;
			if (mod.orig.publishedFileID != PublishedFileId.invalid)
				bakdir = modHome + ".bak";
			else
				bakdir = dl.destDir + ".bak";

			if (needsBackup)
			{
#if INSTALLER
				if (mod.orig == Mod.mainMod)
                {
					try
					{
						if (Directory.Exists(bakdir))
							Directory.Delete(bakdir, true);
					} catch (Exception ex)
                    {
						LogError($"[{Versioning.FULL_PACKAGE_NAME}] Failed to reserve backup dir '{bakdir}': {ex.Message}");
					}
				}
#endif
				LogError($"[{Versioning.FULL_PACKAGE_NAME}] Moving '{modHome}' to '{bakdir}'");
				try { Directory.Move(modHome, bakdir); }
				catch (Exception ex) {
#if INSTALLER
					LogError($"[{Versioning.FULL_PACKAGE_NAME}] Failed to backup {modHome} to {bakdir}: {ex.Message}");
#else
					Mod.SelfReport(SelfProblemType.FailedToInitialize, ex);
#endif
					failed = true;
				}
			}

			if (!failed)
			{
				try {
					Directory.Move(dl.destDir, modHome);
					Touch(Path.Combine(modHome, $".ghrel-{release[_GitHubRelease.REL_ID]}"));
				}
				catch (Exception ex) {
#if INSTALLER
					LogError($"[{Versioning.FULL_PACKAGE_NAME}] Failed to move downloaded directory '{dl.destDir}' to destination '{modHome}': {ex.Message}");
#else
					Mod.SelfReport(SelfProblemType.FailedToInitialize, ex);
#endif
					failed = true;
				}

				if (needsBackup)
				{
					try
					{
						if (failed)
						{
							Directory.Move(bakdir, modHome);
						}
						else
						{
							Directory.Delete(bakdir, true);
						}
					}
					catch (Exception ex) {
#if INSTALLER
						LogError($"[{Versioning.FULL_PACKAGE_NAME}] Failed to cleanup after {(failed ? "failed" : "successful")} install: {ex.Message}");
#else
						Mod.SelfReport(SelfProblemType.FailedToInitialize, ex);
#endif
					}
				}
			}

			return !failed;
		}

#if DEBUG
		public void Cleanup()
        {
			LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] Collection Cleanup() not implemented");
		}
#endif
	}
}
