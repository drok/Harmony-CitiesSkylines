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

using CitiesHarmony.API;
using ICities;
// Make sure that "using HarmonyLib;" does not appear here!
// Only reference HarmonyLib in code that runs when Harmony is ready (DoOnHarmonyReady, IsHarmonyInstalled)

namespace BadMods {
    public class PatchTooEarlyMod : IUserMod {        
        // You can add Harmony 2.0.0.9 as a dependency, but make sure that 0Harmony.dll is not copied to the output directory!
        // (0Harmony.dll is provided by CitiesHarmony workshop item)

        // Also make sure that HarmonyLib is not referenced in any way in your IUserMod implementation!
        // Instead, apply your patches from a separate static patcher class!
        // (otherwise it will fail to instantiate the type when CitiesHarmony is not installed)

        public string Name => $"{GetType().Namespace}: {GetType().Name}";
        public string Description => "Patches SimulationManager.CreateRelay and LoadingManager.MetaDataLoaded";

        Patcher patcher = new Patcher();
        public void OnEnabled() {
            HarmonyHelper.DoOnHarmonyReady(() => patcher.PatchAll());
        }

        public void OnDisabled() {
            if (HarmonyHelper.IsHarmonyInstalled) patcher.UnpatchAll();
        }
    }
}

