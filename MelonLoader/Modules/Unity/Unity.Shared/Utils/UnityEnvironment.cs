﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using UnityVersion = AssetRipper.Primitives.UnityVersion;
using MelonLoader.Utils;

namespace MelonLoader.Unity.Utils
{
    public static class UnityEnvironment
    {
        private const string _defaultInfo = "UNKNOWN";
        private static bool _isInitialized;

        public static UnityVersion EngineVersion { get; private set; } = UnityVersion.MinVersion;
        public static string EngineVersionString { get; private set; } = UnityVersion.MinVersion.ToString();

        public static string GameName { get; private set; }
        public static string GameDeveloper { get; private set; }
        public static string GameVersion { get; private set; }

        public static void Initialize(string gameDataPath)
        {
            // Check if already Initialized
            if (_isInitialized)
                return;
            _isInitialized = true;

            //if (!string.IsNullOrEmpty(MelonLaunchOptions.Core.UnityVersion))
            //{
            //    try { EngineVersion = UnityVersion.Parse(MelonLaunchOptions.Core.UnityVersion); }
            //    catch (Exception ex)
            //    {
            //        EngineVersion = UnityVersion.MinVersion;
            //        if (MelonDebug.IsEnabled())
            //            MelonLogger.Error(ex);
            //    }
            //}

            AssetsManager assetsManager = new AssetsManager();
            ReadGameInfo(assetsManager, gameDataPath);
            assetsManager.UnloadAll();

            if (string.IsNullOrEmpty(GameDeveloper)
                || string.IsNullOrEmpty(GameName))
                ReadGameInfoFallback(gameDataPath);

            if (EngineVersion == UnityVersion.MinVersion)
            {
                try { EngineVersion = ReadVersionFallback(gameDataPath); }
                catch (Exception ex)
                {
                    if (MelonDebug.IsEnabled())
                        MelonLogger.Error(ex);
                }
            }

            if (string.IsNullOrEmpty(GameDeveloper))
                GameDeveloper = _defaultInfo;
            if (string.IsNullOrEmpty(GameName))
                GameName = _defaultInfo;
            if (string.IsNullOrEmpty(GameVersion))
                GameVersion = _defaultInfo;

            EngineVersionString = EngineVersion.ToString();
        }

        private static void ReadGameInfo(AssetsManager assetsManager, string gameDataPath)
        {
            AssetsFileInstance instance = null;
            try
            {
                string bundlePath = Path.Combine(gameDataPath, "globalgamemanagers");
                if (!File.Exists(bundlePath))
                    bundlePath = Path.Combine(gameDataPath, "mainData");

                if (!File.Exists(bundlePath))
                {
                    bundlePath = Path.Combine(gameDataPath, "data.unity3d");
                    if (!File.Exists(bundlePath))
                        return;

                    BundleFileInstance bundleFile = assetsManager.LoadBundleFile(bundlePath);
                    instance = assetsManager.LoadAssetsFileFromBundle(bundleFile, "globalgamemanagers");
                }
                else
                    instance = assetsManager.LoadAssetsFile(bundlePath, true);
                if (instance == null)
                    return;

                assetsManager.LoadIncludedClassPackage();

                if (!instance.file.Metadata.TypeTreeEnabled)
                    assetsManager.LoadClassDatabaseFromPackage(instance.file.Metadata.UnityVersion);

                if (EngineVersion == UnityVersion.MinVersion)
                    EngineVersion = UnityVersion.Parse(instance.file.Metadata.UnityVersion);

                List<AssetFileInfo> assetFiles = instance.file.GetAssetsOfType(AssetClassID.PlayerSettings);
                if (assetFiles.Count > 0)
                {
                    AssetFileInfo playerSettings = assetFiles.First();

                    AssetTypeValueField playerSettings_baseField = assetsManager.GetBaseField(instance, playerSettings);
                    if (playerSettings_baseField != null)
                    {
                        AssetTypeValueField bundleVersion = playerSettings_baseField.Get("bundleVersion");
                        if (bundleVersion != null)
                            GameVersion = bundleVersion.AsString;

                        AssetTypeValueField companyName = playerSettings_baseField.Get("companyName");
                        if (companyName != null)
                            GameDeveloper = companyName.AsString;

                        AssetTypeValueField productName = playerSettings_baseField.Get("productName");
                        if (productName != null)
                            GameName = productName.AsString;
                    }
                }
            }
            catch (Exception ex)
            {
                if (MelonDebug.IsEnabled())
                    MelonLogger.Error(ex);
                //MelonLogger.Error("Failed to Initialize Assets Manager!");
            }
            if (instance != null)
                instance.file.Close();
        }

        private static void ReadGameInfoFallback(string gameDataPath)
        {
            try
            {
                string appInfoFilePath = Path.Combine(gameDataPath, "app.info");
                if (!File.Exists(appInfoFilePath))
                    return;

                string[] filestr = File.ReadAllLines(appInfoFilePath);
                if ((filestr == null) || (filestr.Length < 2))
                    return;

                if (string.IsNullOrEmpty(GameDeveloper) && !string.IsNullOrEmpty(filestr[0]))
                    GameDeveloper = filestr[0];

                if (string.IsNullOrEmpty(GameName) && !string.IsNullOrEmpty(filestr[1]))
                    GameName = filestr[1];

            }
            catch (Exception ex)
            {
                if (MelonDebug.IsEnabled())
                    MelonLogger.Error(ex);
            }
        }

        private static UnityVersion ReadVersionFallback(string gameDataPath)
        {

            try
            {
                var globalgamemanagersPath = Path.Combine(gameDataPath, "globalgamemanagers");
                if (File.Exists(globalgamemanagersPath))
                    return GetVersionFromGlobalGameManagers(File.ReadAllBytes(globalgamemanagersPath));
            }
            catch (Exception ex)
            {
                if (MelonDebug.IsEnabled())
                    MelonLogger.Error(ex);
            }

            try
            {
                var dataPath = Path.Combine(gameDataPath, "data.unity3d");
                if (File.Exists(dataPath))
                    return GetVersionFromDataUnity3D(File.OpenRead(dataPath));
            }
            catch (Exception ex)
            {
                if (MelonDebug.IsEnabled())
                    MelonLogger.Error(ex);
            }

            if (MelonUtils.IsWindows)
            {
                string unityPlayerPath = Path.Combine(MelonEnvironment.GameRootDirectory, "UnityPlayer.dll");
                if (!File.Exists(unityPlayerPath))
                    unityPlayerPath = MelonEnvironment.GameExecutablePath;

                var unityVer = FileVersionInfo.GetVersionInfo(unityPlayerPath);
                return new UnityVersion((ushort)unityVer.FileMajorPart, (ushort)unityVer.FileMinorPart, (ushort)unityVer.FileBuildPart);
            }

            return default;
        }

        private static UnityVersion GetVersionFromGlobalGameManagers(byte[] ggmBytes)
        {
            var verString = new StringBuilder();
            var idx = 0x14;
            while (ggmBytes[idx] != 0)
            {
                verString.Append(Convert.ToChar(ggmBytes[idx]));
                idx++;
            }

            Regex UnityVersionRegex = new Regex(@"^[0-9]+\.[0-9]+\.[0-9]+[abcfx][0-9]+$", RegexOptions.Compiled);
            string unityVer = verString.ToString();
            if (!UnityVersionRegex.IsMatch(unityVer))
            {
                idx = 0x30;
                verString = new StringBuilder();
                while (ggmBytes[idx] != 0)
                {
                    verString.Append(Convert.ToChar(ggmBytes[idx]));
                    idx++;
                }

                unityVer = verString.ToString().Trim();
            }

            return UnityVersion.Parse(unityVer);
        }

        private static UnityVersion GetVersionFromDataUnity3D(Stream fileStream)
        {
            var verString = new StringBuilder();

            if (fileStream.CanSeek)
                fileStream.Seek(0x12, SeekOrigin.Begin);
            else
            {
                if (fileStream.Read(new byte[0x12], 0, 0x12) != 0x12)
                    throw new("Failed to seek to 0x12 in data.unity3d");
            }

            while (true)
            {
                var read = fileStream.ReadByte();
                if (read == 0)
                    break;
                verString.Append(Convert.ToChar(read));
            }

            return UnityVersion.Parse(verString.ToString().Trim());
        }
    }
}
