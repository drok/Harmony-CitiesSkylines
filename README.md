# Harmony provider for Cities Skylines mods

This is a mod that provides several services to CSL mods:

 * Provides a copy of the [Harmony patching library](https://github.com/pardeike/Harmony) shared among all mods.
 * Catches Exceptions thrown by mods, and several other error conditions, analyzes them and presents a digest as a **Harmony Report** ([sample](https://gist.github.com/drok/f6097a3bc7376e2eae65830c5534bc7b))
 
While its main job is to enable mods to patch the game logic, the main contribution of this mod over the plain library is error management. The goal is to make error reports easy to present to affected mod authors, and to make the information presented, accurate and actionable, to save authors precious debugging time, in the hope that the overall health of the mod library available will increase quickly.

This packaging is fully backwards compatible with Colossal Order's own Harmony Library mod, such that mods that were written with that library in mind will work at least as well with this one, but hopefully better.

Why does this mod exist if Colossal Order has already provided a Harmony library?

1. to provide error management, which is not available with CO's library.
2. to provide a stable, **tested** library. CO's library is haphazardly released without formal testing, often resulting in widespread breakages.
3. To eliminate CO's threat to some mods like Network Extensions, which is regularly threatened to be rendered "incompatible" by future Harmony updates.

In a nutshell, Colossal Order has a vested interest in killing some comprehensive mods like Network Extensions, so they can sell consumers DLC with largely the same features as the free mod alternatives. This package is maintained by an author with no commercial interest in DLC's or other mods.

## Compatibility:

In a nutshell, if it has "Harmony" in its name, this package is most likely compatible. (if not, [submit an issue](../../issues/new))

This mod allows older mods which use 0Harmony.dll v1.x to interoperate with newer mods. This mod provides the latest available Harmony library, and a compatibility wrapper around the older, 1.x APIs, so mods that use the 1.x API are actually using a 2.x library. This allows both 1.x and 2.x libraries to coexist in the same modding environment, something which is not directly supported by the Harmony library developers themselves.

Of the older mods, wrappers are currently implemented only for select versions (others can be supported if needed, send me a Pull Request using the 1.2.0.1 wrapper as template):

 * 0Harmony.dll v1.1.0.0
 * 0Harmony.dll v1.2.0.1
 
The newer, 2.x series of libraries are all supported.

All API versions previously released by Colossal Order/boformer/Felix Schmidt for his Harmony library mod are supported. This mod is a **drop-in replacement**.

## Installation (by players)

End users will typically install it in one of the following ways:

* When a client mod that requires it is first enabled in-game, the API can install it transparently. (with or without confirmation, depending on the client mod configuration)
* By subscribing to the [workshop item](https://steamcommunity.com/sharedfiles/filedetails/?id=2399343344) manually
* By downloading a [Release file](../../releases) and installing it manually as a local mod

## Developer Documentation (for modders)

There are several [sample mods](/drok/CSL-ExampleMods) available to help you get started quickly.

The samples are:
 * Public Domain - no need for attribution, you can attach any copyright license you wish.
 * Visual Studio solutions with the API already referenced. Nuget will automatically download the API. Clone a Sample, open it with Visual Studio, start modding.
 * **.NET Framework v3.5** projects. This is a requirement of the City Skylines modding environment. [Installation instructions for Windows](https://docs.microsoft.com/en-us/dotnet/framework/install/dotnet-35-windows#enable-the-net-framework-35-in-control-panel).
 * C# classes (if you have the expertise to port them to VisualBasic or other .NET languages, please contribute them with a pull request)

### Under the hood

City Skylines mods are **.NET Framework v3.5** classes
Not all mods require Harmony patching, but if yours does, you need to reference the Nuget assembly [Harmony-CitiesSkylines](https://www.nuget.org/packages/Harmony-CitiesSkylines/). This assembly is a bit of glue logic between your mod and the Harmony Library.

The API assembly's purpose is to install the Harmony mod if the end-user's environment doesn't already have it installed.

The user can be prompted, or the installation can be silent; this is up to you.
The interface with the API consists of two functions calls:

* `DoOnHarmonyReady(Action onReady, Action onUnavailable, HarmonyCancelAction cancelAction, bool autoInstall)`
  - `onReady` - delegate to be called when the Harmony Library is installed, initialized, ready to work. This callback typically calls `HarmonyLib.PatchAll()`
  - `onUnavailable` - delegate to be called when the Harmony Library will not be available (eg, user declined installation). This typically sets a mode flag so your mod knows it's operating without patches.
  - `cancelAction` - option to automatically disable the mod when Harmony is unavailble (eg, if it cannot do anything useful without patches)
  - `autoInstall` - option to silently install Harmony when missing (true) or to prompt the user when necessary (false)
  - This function can be called multiple times, the callbacks will be enqueued. The options set last take precedence over earlier options.
* `CancelOnHarmonyReady()`
  - clears all previously queued callbacks, resets the `cancelAction` to `noop`, and recalls any auto-installation that has not yet started.
  - This function should be called when the mod is disabled, or if the game starts before Harmony becomes available (eg, slow or unavailable connectivity), and your mod is not able to insert patches mid-game (without crashing self or the game)

```csharp
        /* LoadingExtension.OnCreated() is called when the player starts an appmode
         * by loading a city, the asset editor or a map editor.
         */
        public override void OnCreated(ILoading loading)
        {
            if (loading.currentMode == AppMode.Game)
            {
                HarmonyManager.Harmony.DoOnHarmonyReady(PatchGame);
            }
            base.OnCreated(loading);
        }
```

Happy Modding
