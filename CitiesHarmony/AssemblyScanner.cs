using Mono.Cecil;
using System;
using System.Reflection;
using ColossalFramework.UI;
using UnityEngine;
using ColossalFramework.Plugins;
using System.Collections.Generic;
namespace CitiesHarmony {
    public class IlligalDirectHarmonyUsageException : Exception {
        public IlligalDirectHarmonyUsageException(string dll) :
            base("Direct refrence to harmony found. please ask modder to remove it:" + dll) { }
    }

    public static class AssemblyScanner {
        private static string HARMONY_NAME = "0Harmony";

        public static void VerifyAllROAssemblies() {
            Dictionary<Assembly, string> m_AssemblyLocations = 
                typeof(PluginManager)
                .GetField("m_AssemblyLocations", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(PluginManager.instance)
                as Dictionary<Assembly, string>;
            foreach(string dllPath in m_AssemblyLocations.Values) {
                VerifyDll(dllPath);
            }
        }


        public static void VerifyDll(string dllPath) {
            var asm = AssemblyDefinition.ReadAssembly(dllPath);
            if (asm.Name.Name == HARMONY_NAME)
                return; // no need to verify harmony dll.

            void Verify(TypeReference _type) {
                if (IsHarmony2Type(_type)) {
                    var ex = new IlligalDirectHarmonyUsageException(dllPath);
                    Debug.LogException(ex);
                    UIView.ForwardException(ex);
                }
            }

            // log error if there is any direct reference to harmony.
            foreach (var type in asm.MainModule.Types) {
                Verify(type);
                foreach (var field in type.Fields)
                    Verify(field.FieldType);
                foreach (var property in type.Properties) {
                    Verify(property.PropertyType);
                    foreach (var parameter in property.Parameters)
                        Verify(parameter.ParameterType);
                }
                foreach (var method in type.Methods) {
                    Verify(method.ReturnType);
                    foreach (var parameter in method.Parameters)
                        Verify(parameter.ParameterType);
                }
                foreach (var e in type.Events) {
                    Verify(e.EventType);
                    var method = e.InvokeMethod;
                    Verify(method.ReturnType);
                    foreach (var parameter in method.Parameters)
                        Verify(parameter.ParameterType);
                }
            }
        }

        public static bool IsHarmony2Type(TypeReference type) {
            var asmName = type.Module.Assembly.Name;
            return asmName.Name == HARMONY_NAME && asmName.Version >= new Version(2, 0);
        }
    }
}
