# Harmony lib for Cities Skylines

/*
MIT License

Copyright (c) 2017 Felix Schmidt

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

/* **************************************************************************
 * 
 * 
 * IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT
 * 
 * This file contains leftover code from the initial fork from Felix Schmidt's
 * repository https://github.com/boformer/CitiesHarmony
 * 
 * It contains known bad code, which is either not used at all in my implementation,
 * or it is in the course of being re-written. If I am rewriting it, I only included
 * it because an initial release of my project was needed urgently to address
 * a broken modding eco-system in Cities Skylines, and I considered it will do no
 * further harm over what has already been done by Felix Schmidt's code.
 * 
 * I would recommend you do not copy or rely on this code. A near future update will
 * remove this and replace it with proper code I would be proud to put my name on.
 * The examples given in this leftover README are very bad, and they are main causes
 * for the malaise in the Cities Skylines modding environment. It is because of this
 * bad code that I got involved to try to clean up the mess for the player community.
 * 
 * Until then, the copyright notice above was expressely requested by Felix Schmidt,
 * by means of a DMCA complaint at GitHub and Steam.
 * 
 * There is no code between the end of this comment and he "END-OF-Felix Schmidt-COPYRIGHT"
 * line if there is one, or the end of the file, that I, Radu Hociung, claim any copyright
 * on. The rest of the content, outside of these delimiters, is my copyright, and
 * you may copy it in accordance to the modified GPL license at the root or the repo
 * (LICENSE file)
 * 
 * FUTHER, if you base your code on a copy of the example mod from Felix Schmidt's
 * repository, which does not include his copyright notice, you will likely also
 * be a victim of DMCA complaint from him.
 */



[![NuGet Badge](https://buildstats.info/nuget/Harmony-CitiesSkylines)](https://www.nuget.org/packages/Harmony-CitiesSkylines/)

This C:SL mod provides Andreas Pardeike's [Harmony patching library](https://github.com/pardeike/Harmony) (version 2.0.4) to all mods that require it.

It hotpatches older Harmony versions (1.2.0.1 and 1.1.0.0) and adds limited cross-compatibility for Harmony 1.0.9.1. All of those versions are still used by various mods. The patching is necessary because Harmony 1.x is incompatible with 2.x.

All API versions previously released by boformer for his CitiesHarmony mod are supported. This mod is a **drop-in replacement**.

**By using auto-subscription, it is possible to migrate existing mods to Harmony 2.x without causing disruptions for users!**

## Documentation for Mod Developers

### API Package Installation

To use Harmony 2.x in your mod, add the [CitiesHarmony.API](https://www.nuget.org/packages/CitiesHarmony.API/) nuget package to your project. The package includes the latest version of Harmony as well as the `HarmonyHelper` that is used to access it.

Make sure that when you build your mod:

* The `CitiesHarmony.API.dll` is copied to the AppData mod directory
* The `CitiesHarmony.Harmony.dll` is **not** copied to the AppData mod directory (it is provided by the central Harmony mod)

Depending on the version of Visual Studio, the project style and the post-build script you are using, there are different ways to achieve that:

* If you are manually copying the assemblies to AppData mod directory, make sure to copy only the DLL of your mod and the `CitiesHarmony.API.dll`, **not** the `CitiesHarmony.Harmony.dll`.
* If you are using the old Visual Studio project style (packages.config) with a post-build script, set "Local Copy" to true for `CitiesHarmony.API` reference and to **false** for `CitiesHarmony.Harmony` reference.
* If you are using the new Visual Studio project style (like the example mod in this repository) with a post-build target, no action is required after installing the Nuget package! It is configured so that `CitiesHarmony.Harmony` is not copied to the target directory.

### API Usage

**Make sure that there are no references to `HarmonyLib` in your `IUserMod` implementation.**
Otherwise the mod could not be loaded if CitiesHarmony is not subscribed. Instead, it is recommended to keep `HarmonyLib`-related code (such as calls to `PatchAll` and `UnpatchAll`) in a separate static `Patcher` class.

Before making calls to harmony in your code, you need to query `CitiesHarmony.API.HarmonyHelper` to see if it is available. There are 3 different hooks for that purpose:

* `void HarmonyHelper.DoOnHarmonyReady(Action)`: Will invoke the passed action when Harmony 2.x is ready to use. This hook should be called from `IUserMod.OnEnabled`. If the Harmony mod is not installed, this hook will attempt to auto-subscribe to it.
* `void HarmonyHelper.EnsureHarmonyInstalled()`: If you don't want to apply your patches in `IUserMod.OnEnabled`, call this method instead. It will perform the same auto-subscription as `DoOnHarmonyReady`
* `bool HarmonyHelper.IsHarmonyInstalled`: Returns `true` is Harmony is ready to be used. When queried, this hook will *not* attempt to auto-subscribe to the Harmony workshop item. Use this hook for all kinds of unpatching, applying patches in the `LoadingExtension` or while the simulation is running.

Take a look at the example mod in this repository for further inspiration!

It is recommened to add this mod as a dependency to your workshop item for transparency reasons.

#### Alternative A: Applying your patches in `OnEnabled`/`OnDisabled`

```c#
public class Mod : IUserMod {
    // ...

    public void OnEnabled() {
        HarmonyHelper.DoOnHarmonyReady(() => Patcher.PatchAll());
    }

    public void OnDisabled() {
        if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
    }
}
```

#### Alternative B: Applying your patches in `LoadingExtensionBase`

```c#
public class Mod : LoadingExtensionBase, IUserMod {
    // ...

    public void OnEnabled() {
        HarmonyHelper.EnsureHarmonyInstalled();
    }

    public override void OnCreated(ILoading loading) {
        if (HarmonyHelper.IsHarmonyInstalled) Patcher.PatchAll();
    }

    public override void OnReleased() {
        if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
    }
}
```

#### Example Patcher Class

```c#
public static class Patcher {
    private const string HarmonyId = "yourname.YourModName";
    private static bool patched = false;

    public static void PatchAll() {
        if (patched) return;

        patched = true;
        var harmony = new Harmony(HarmonyId);
        harmony.PatchAll(typeof(Patcher).GetType().Assembly); // you can also do manual patching here!
    }

    public static void UnpatchAll() {
        if (!patched) return;

        var harmony = new Harmony(HarmonyId);
        harmony.UnpatchAll(HarmonyId);
        patched = false;
    }
}
```
