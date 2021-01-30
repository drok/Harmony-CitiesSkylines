using Mono.Cecil;
using Mono.Cecil.Cil;
using Patch.API;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CitiesHarmony {
    public class CMPatch : IPatch {
        public int PatchOrderAsc { get; } = 1000;
        public AssemblyToPatch PatchTarget { get; } = new AssemblyToPatch("ColossalManaged", new Version());
        private ILogger logger_;
        private string workingPath_;
        private static Version MIN_HARMONY1_VERSION = new Version(1, 1, 0, 0);
        private static Version MIN_HARMONY2_VERSION = new Version(2, 0, 0, 8);
        private static Version V2 = new Version(2, 0);
        private static string HARMONY_NAME = "0Harmony";

        public AssemblyDefinition Execute(AssemblyDefinition assemblyDefinition, ILogger logger, string patcherWorkingPath) {
            logger_ = logger;
            workingPath_ = patcherWorkingPath;

            //LoadDLL(Path.Combine(workingPath_, "0Harmony.dll"));
            //var citiesHarmony = LoadDLL(Path.Combine(workingPath_, "CitiesHarmony.dll"));
            //citiesHarmony.GetType(nameof(Installer)).GetMethod(nameof(Installer.Run)).Invoke(null,null);
            Installer.Run();

            LoadPluginPatch(assemblyDefinition);
            return assemblyDefinition;
        }

        public Assembly LoadDLL(string dllPath) {
            logger_.Info("Loading " + dllPath);
            Assembly assembly = Assembly.Load(File.ReadAllBytes(dllPath));
            if (assembly != null) {
                logger_.Info("Assembly " + assembly.FullName + " loaded.\n");
            } else {
                logger_.Error("Assembly at " + dllPath + " failed to load.\n");
            }
            return assembly;
        }

        /// <summary>
        /// scan for direcy harmony usage and put error log on screen.
        /// </summary>
        void LoadPluginPatch(AssemblyDefinition CM) {
            logger_.Info("LoadPluginPatch() called ...");
            var module = CM.MainModule;
            //private Assembly ColossalFramework.Plugins.PluginManager.LoadPlugin(string dllPath)
            var type = module.GetType("ColossalFramework.Packaging.PackageManager");
            var mLoadPlugin = type.Methods.Single(_m => _m.Name.StartsWith("LoadPlugin"));
            ILProcessor ilProcessor = mLoadPlugin.Body.GetILProcessor();
            var instructions = mLoadPlugin.Body.Instructions;

            Instruction first = instructions.First(); // first instruction of the original method
            Instruction loadDllPath = Instruction.Create(OpCodes.Ldarg_1);
            MethodInfo mVerifyDll = typeof(AssemblyScanner).GetMethod(nameof(AssemblyScanner.VerifyDll));
            Instruction callVerifyDll = Instruction.Create(OpCodes.Call, module.ImportReference(mVerifyDll));
            Instruction branchToFirst = Instruction.Create(OpCodes.Brfalse, first);
            Instruction loadNull = Instruction.Create(OpCodes.Ldnull);
            Instruction ret = Instruction.Create(OpCodes.Ret);


            // VerifyDll(dllPath)
            ilProcessor.InsertBefore(first, loadDllPath);
            ilProcessor.InsertAfter(loadDllPath, callVerifyDll);

            logger_.Info("LoadPluginPatch() succeeded!");
        }

    }
}
