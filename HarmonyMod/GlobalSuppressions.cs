// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Underscore prefix is personal preference for static associated structs", Scope = "type", Target = "~T:HarmonyMod._DownloadManager")]
[assembly: SuppressMessage("Style", "IDE0090:Use 'new(...)'", Justification = "new() is only available in C# 9.0+", Scope = "member", Target = "~F:HarmonyMod.DownloadManager.saveError")]
[assembly: SuppressMessage("Style", "IDE0090:Use 'new(...)'", Justification = "new() is only available in C# 9.0+", Scope = "member", Target = "~F:HarmonyMod.DownloadManager.downloadError")]
[assembly: SuppressMessage("Style", "IDE0090:Use 'new(...)'", Justification = "<Pending>", Scope = "member", Target = "~F:HarmonyMod.Downloadable.steamSubscription")]
[assembly: SuppressMessage("Style", "IDE0090:Use 'new(...)'", Justification = "<Pending>", Scope = "member", Target = "~F:HarmonyMod.DownloadManager.notModified")]
[assembly: SuppressMessage("Style", "IDE0090:Use 'new(...)'", Justification = "<Pending>", Scope = "member", Target = "~F:HarmonyMod.DownloadManager.releaseDoesNotExist")]
[assembly: SuppressMessage("Style", "IDE0063:Use simple 'using' statement", Justification = "<Pending>", Scope = "member", Target = "~M:HarmonyMod.DownloadManager.HttpHead(System.String)~System.Collections.Generic.IEnumerator{UnityEngine.YieldInstruction}")]
