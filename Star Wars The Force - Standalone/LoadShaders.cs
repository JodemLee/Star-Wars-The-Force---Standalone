using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;

namespace TheForce_Standalone
{
    [StaticConstructorOnStartup]
    public static class ForceContentDatabaseStandalone
    {
        private static AssetBundle _bundleInt;
        private static Dictionary<string, Shader> _lookupShaders;
        private const string _rootPathUnlit = "Assets/Data/lee.theforce.standalone/Materials/Shaders/";

        // thing specific (ideally lol)
        public static readonly Shader ForceVoidShader = LoadShader(Path.Combine(_rootPathUnlit, "ForceVoidShader.shader"));
        public static readonly Shader GhostVoidShader = LoadShader(Path.Combine(_rootPathUnlit, "GhostShader.shader"));
        public static AssetBundle ForceBundle
        {
            get
            {
                if (_bundleInt != null) return _bundleInt;
                try
                {
                    _bundleInt = TheForce_Standalone.TheForce_Mod.Force_Mod.MainBundle;
                    if (_bundleInt == null)
                    {
                        throw new Exception("MainBundle is null.");
                    }
                    return _bundleInt;
                }
                catch (Exception ex)
                {
                    Log.Warning($"Failed to load AssetBundle. " +
                                  $"Exception: {ex.Message}");
                    return null;
                }
            }
        }

        private static Shader LoadShader(string shaderName)
        {
            _lookupShaders ??= new Dictionary<string, Shader>();
            if (!_lookupShaders.ContainsKey(shaderName))
            {
                _lookupShaders[shaderName] = ForceBundle.LoadAsset<Shader>(shaderName);
            }

            Shader shader = _lookupShaders[shaderName];
            if (shader == null)
            {
                throw new Exception($"Shader '{shaderName}' " +
                                    $"is null after loading.");
            }
            return shader;
        }
    }


    // Keep your existing ForceShaderDef class
    public class ForceShaderDef : ShaderTypeDef { }

}