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
using static Json.NETMF.JsonSerializer;
using static Json.NETMF.JsonParser;
#if TRACE
using static UnityEngine.Debug;
#endif
using IAwareness;

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

			/* Repo is unknown. Should fetch releases */
			// UnknownRepo,
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
		public CheckoutResult PrepareToDownload(GitHubRelease item, bool update, out Hashtable release)
		{
#if HEAVY_TRACE
			Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Collection Preparing to {(update ? "update" : "download")} {item} to {workingDir} Latest={item.Latest.value}");
#endif

			// SavedString savedLatest = new SavedString(item.myItemBranch + "[latest]", Settings.userGameState);
			// latest = savedLatest.exists ? savedLatest.value : null;
#if TRACE
			Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Prepare item.Latest={item.Latest}");
#endif

			if (!update && item.Latest.value != string.Empty)
			{
				SavedString savedRelease = new SavedString(item.myReleaseBranch + "[release]", Settings.userGameState, string.Empty, true);

				var data = JsonDecode(savedRelease.value) as Hashtable;
#if TRACE
				Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Prepare savedRelease({savedRelease.name}).exists={savedRelease.exists}");
#endif
				if (savedRelease.exists)
				{
					release = data[_Item.RELEASE_DATA_ID_RELEASE] as Hashtable;

					SavedString installed = new SavedString(item.myItemBranch + "[installed]", Settings.userGameState, string.Empty, true);
					return (installed.value == item.myReleaseBranch + "/" + (release["tag_name"] as string)) ? CheckoutResult.OK :
						CheckoutResult.NeedAssets;
				}
				else
					release = null;
			} else
			{
				release = null;
			}
			return CheckoutResult.UnknownRelease;
		}

		public bool StoreGithubReleaseInfo(IEnumerable releases, GitHubRelease item)
		{
			{
				bool complete = false;
				SavedString repoUpdated = new SavedString(item.myItemBranch + "[updated]", Settings.userGameState, string.Empty, true);
				DateTimeOffset lastRepoUpdate = (repoUpdated.value != string.Empty) ?
					DateTimeOffset.Parse(repoUpdated.value) : default(DateTimeOffset);

#if TRACE
				Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - repoUpdated={repoUpdated}");
#endif
				foreach (Hashtable release in releases)
				{
					var branch = release["target_commitish"] as string;
					var releaseName = item.myItemBranch + "/" + branch;
#if TRACE
					Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Collection Handling Release {release["name"] as string} @ {branch} ({release["published_at"] as string}) to {this}. Item last updated {item.updated}");
#endif
					if (item.Latest.exists && item.Latest.value == (release["html_url"] as string))
					{
						complete = true;
						break;
					}

					SavedString branchUpdated = new SavedString(releaseName + "[updated]", Settings.userGameState, string.Empty, true);
					DateTimeOffset lastBranchUpdate = (branchUpdated.value != string.Empty) ?
						DateTimeOffset.Parse(branchUpdated.value) : default(DateTimeOffset);
					var published_at = DateTimeOffset.Parse(release["published_at"] as string);
#if TRACE
					Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - branchUpdated={branchUpdated}");
#endif

					if (!repoUpdated.exists || lastRepoUpdate < published_at)
					{
						lastRepoUpdate = published_at;
						repoUpdated.value = published_at.ToString(CultureInfo.InvariantCulture);
					}

					if (!branchUpdated.exists || lastBranchUpdate < published_at)
					{

						// updated.value = published_at.ToString(CultureInfo.InvariantCulture);
						lastBranchUpdate = published_at;

						/* Create empty commit with release details, and no assets */
						Hashtable releaseContainer = new Hashtable();
						// releaseContainer.Add("v", Versioning.ImplementationVersion);
						releaseContainer.Add("release", release);
						SavedString latestRelease = new SavedString(releaseName + "[release]", Settings.userGameState, string.Empty, true);
						latestRelease.value = SerializeObject(releaseContainer);
#if TRACE
						Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - set release({latestRelease.name})");
#endif

						SavedString latestUpdate = new SavedString(releaseName + "[updated]", Settings.userGameState, string.Empty, true);
						latestUpdate.value = published_at.ToString(CultureInfo.InvariantCulture);
#if TRACE
						Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - set updated({latestUpdate.name})");
#endif
#if TRACE
						Log($"[{Versioning.FULL_PACKAGE_NAME}] INFO - Select {release["tag_name"] as string} @ {release["target_commitish"] as string} ({release["published_at"] as string}) as latest release for {this}. Item last updated {item.updated}");
#endif
					}
					else
					{
						complete = true;
						break;
					}

				}

				/* Update the Etag */
				// SavedString relLatest = new SavedString(item.myItemBranch + "[latest]", Settings.userGameState);
				// relLatest.value = item.Latest.value;

				return complete;
			}
		}
	
		public void OnSuccessfulDownload(Loaded mod, Item item, Hashtable release, DownloadState dl)
		{
			SavedString installed = new SavedString(item.myItemBranch + "[installed]", Settings.userGameState, string.Empty, true);
			installed.value = item.myReleaseBranch + "/" + (release["tag_name"] as string);

			var modHome = mod.orig.modPath;
			bool needsBackup = Directory.Exists(modHome);
			bool failed = false;
			var bakdir = dl.destDir + ".bak";
			if (needsBackup)
			{
				try { Directory.Move(modHome, bakdir); }
				catch (Exception ex) { Mod.SelfReport(SelfProblemType.FailedToInitialize, ex); failed = true; }
			}

			if (!failed)
			{
				try { Directory.Move(dl.destDir, modHome); }
				catch (Exception ex) { Mod.SelfReport(SelfProblemType.FailedToInitialize, ex); failed = true; }

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
					catch (Exception ex) { Mod.SelfReport(SelfProblemType.FailedToInitialize, ex); }
				}
			}

		}

#if DEBUG
		public void Cleanup()
        {
			LogWarning($"[{Versioning.FULL_PACKAGE_NAME}] Collection Cleanup() not implemented");
			// throw new NotImplementedException();
		}
#endif
	}
}
