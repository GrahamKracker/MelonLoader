﻿using System.IO;
using MelonLoader.Interfaces;
using MelonLoader.Utils;
using MelonLoader.Mono.Bootstrap;
using System.Collections.Generic;
using MelonLoader.Unity.Utils;
using System.Drawing;
using MelonLoader.Fixes;

namespace MelonLoader.Unity
{
    public class Bootstrap : IBootstrapModule
    {
        private static readonly string GameDataPath = $"{Path.Combine(MelonEnvironment.GameRootDirectory, MelonEnvironment.GameExecutableName)}_Data";

        private static string NativeFileExtension
        {
            get
            {
                if (MelonUtils.IsUnix || MelonUtils.IsAndroid)
                    return ".so";
                if (MelonUtils.IsMac)
                    return ".dylib";
                return ".dll";
            }
        }

        public string EngineName => "Unity";

        /// <summary>
        /// TODO: Implement this properly, read the assets/files and see if we can determine engine version, etc.
        /// </summary>
        public bool IsMyEngine
        {
            get
            {
                if (!Directory.Exists(GameDataPath))
                    return false;

                return File.Exists(Path.Combine(GameDataPath, "globalgamemanagers"))
                    || File.Exists(Path.Combine(GameDataPath, "data.unity3d"))
                    || File.Exists(Path.Combine(GameDataPath, "mainData"));
            }
        }

        public void Startup()
        {
            // Read Game Info
            UnityEnvironment.Initialize(GameDataPath);

            // Log the Information
            //BootstrapInterop.SetDefaultConsoleTitleWithGameName(GameName, GameVersion);
            MelonLogger.Msg($"Engine Version: {UnityEnvironment.EngineVersionString}");
            MelonLogger.Msg($"Game Name: {UnityEnvironment.GameName}");
            MelonLogger.Msg($"Game Developer: {UnityEnvironment.GameDeveloper}");
            MelonLogger.Msg($"Game Version: {UnityEnvironment.GameVersion}");

            // Get GameAssembly Name
            string gameAssemblyName = "GameAssembly";
            if (MelonUtils.IsAndroid)
                gameAssemblyName = "libil2cpp";
            gameAssemblyName += NativeFileExtension;

            // Check if GameAssembly exists
            string gameAssemblyPath = Path.Combine(MelonEnvironment.GameRootDirectory, gameAssemblyName);
            if (File.Exists(gameAssemblyPath))
            {
                // Start Il2Cpp Support
                MelonLogger.Msg("Runtime Variant: Il2Cpp");
                //Il2CppLoader.Startup(gameAssemblyPath); 
            }
            else
            { 
                // Android only has Il2Cpp currently
                if (MelonUtils.IsAndroid)
                {
                    MelonAssertion.ThrowInternalFailure($"Failed to find {gameAssemblyName}!");
                    return;
                }

                // Start Mono Support
                MonoRuntimeInfo runtimeInfo = GetMonoRuntimeInfo();
                if (runtimeInfo == null)
                    MelonAssertion.ThrowInternalFailure("Failed to get Mono Runtime Info!");
                else
                {
                    MelonLogger.Msg($"Runtime Variant: {runtimeInfo.VariantName}");
                    MonoLoader.Startup(runtimeInfo);
                }
            }
        }

        internal static MonoRuntimeInfo GetMonoRuntimeInfo()
        {
            //TODO: use netstandard2.1 if applicable
            string engineModulePath = Path.Combine(MelonEnvironment.ModulesDirectory, "Unity", "net35", "MelonLoader.Unity.EngineModule.dll");
            if (!File.Exists(engineModulePath))
            {
                MelonAssertion.ThrowInternalFailure($"Failed to find {engineModulePath}!");
                return null;
            }
            
            // Folders the Mono folders might be located in
            string[] directoriesToSearch = new string[]
            {
                    MelonEnvironment.GameRootDirectory,
                    GameDataPath
            };

            // Variants of Mono folders
            string[] monoFolderVariants = new string[]
            {
                    "Mono",
                    "MonoBleedingEdge"
            };

            // Get Mono variant library file name
            string monoFileNameWithoutExt = "mono";
            if (MelonUtils.IsUnix || MelonUtils.IsMac)
                monoFileNameWithoutExt = $"lib{monoFileNameWithoutExt}";

            // Get Mono Posix Helper file name
            string monoPosixFileNameWithoutExt = "MonoPosixHelper";
            if (MelonUtils.IsUnix || MelonUtils.IsMac)
                monoPosixFileNameWithoutExt = "libmonoposixhelper";

            // Get Platform Used Extension
            string monoFileExt = NativeFileExtension;

            // Iterate through Variations in Mono types
            foreach (var variant in monoFolderVariants)
            {
                // Iterate through Variations in Mono Directory Positions
                foreach (var dir in directoriesToSearch)
                {
                    // Get Directory Path
                    string dirPath = Path.Combine(dir, variant, "EmbedRuntime");
                    if (!Directory.Exists(dirPath))
                        continue;

                    // Get All Containing Files in Directory
                    string[] foundFiles = Directory.GetFiles(dirPath);
                    if (foundFiles == null
                        || foundFiles.Length <= 0)
                        continue;

                    // Get Posix Helper Path
                    string posixPath = Path.Combine(dirPath, $"{monoPosixFileNameWithoutExt}{monoFileExt}");

                    // Get Config Directory Path
                    string configPath = Path.Combine(dir, variant, "etc");

                    // Iterate through all found Files in EmbedRuntime
                    foreach (var filePath in foundFiles)
                    {
                        // Check if its a Runtime library
                        string fileName = Path.GetFileName(filePath);
                        if (!fileName.Equals($"{monoFileNameWithoutExt}{monoFileExt}")
                            && !fileName.StartsWith($"{monoFileNameWithoutExt}-") && fileName.EndsWith(monoFileExt))
                            continue;

                        // Get Variant Id
                        eMonoRuntimeVariant variantId = (variant == monoFolderVariants[0])
                            ? eMonoRuntimeVariant.Mono
                            : eMonoRuntimeVariant.MonoBleedingEdge;

                        // Get Trigger Methods
                        List<string> triggerMethods = new()
                        {
                            "Internal_ActiveSceneChanged",
                            "UnityEngine.ISerializationCallbackReceiver.OnAfterSerialize"
                        };

                        // If Old Mono then Add a few more
                        if (variantId == eMonoRuntimeVariant.Mono)
                        {
                            triggerMethods.Add("Awake");
                            triggerMethods.Add("DoSendMouseEvents");
                        }

                        // Return Information
                        return new MonoRuntimeInfo(
                            variantId,
                            filePath,
                            configPath,
                            triggerMethods.ToArray(),
                            posixPath,
                            engineModulePath
                        );
                    }
                }
            }

            // Return Nothing
            return null;
        }
    }   
}