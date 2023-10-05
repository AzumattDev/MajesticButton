using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using MajesticButton.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MajesticButton
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class MajesticButtonPlugin : BaseUnityPlugin
    {
        internal const string ModName = "MajesticButton";
        internal const string ModVersion = "1.0.3";
        internal const string Author = "Azumatt";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource MajesticButtonLogger = BepInEx.Logging.Logger.CreateLogSource(ModName);

        internal static GameObject originalButton = null!;
        internal static readonly List<GameObject> clonedButtons = new();
        private string currentScene;
        internal static GameObject buttonParent;

        public void Awake()
        {
            ButtonUrls = TextEntryConfig("1 - General", "ButtonUrls", "url1,url2,url3", "Comma-separated list of button URLs. For each URL a button will be created at the main menu next to the 'Show Player.log' button. The button will open the URL in the browser when clicked.");
            ButtonTexts = TextEntryConfig("1 - General", "ButtonTexts", "Text1,Text2,Text3", "Comma-separated list of button texts. This is the text that is on the button. Defaults to the 'Show Player.log'");

            ButtonUrls.SettingChanged += Config_OnSettingChanged;
            ButtonTexts.SettingChanged += Config_OnSettingChanged;
            SceneManager.activeSceneChanged += OnSceneChanged;

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
            CleanupButtons();
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                MajesticButtonLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                MajesticButtonLogger.LogError($"There was an issue loading your {ConfigFileName}");
                MajesticButtonLogger.LogError("Please check your config entries for spelling and format!");
            }
        }

        void Config_OnSettingChanged(object sender, EventArgs e)
        {
            // Assuming the scene is already loaded when the config changes
            if (SceneManager.GetActiveScene().name == "start")
            {
                CleanupButtons();
                Functions.UpdateButtons();
            }
        }

        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            // If we leave the start scene, clean up
            if (oldScene.name == "start")
            {
                CleanupButtons();
                if (buttonParent != null)
                {
                    Destroy(buttonParent);
                    buttonParent = null;
                }
            }


            // If we enter the start scene, setup buttons
            if (newScene.name == "start")
            {
                originalButton = GameObject.Find("GuiRoot/GUI/StartGui/Menu/showlog");
                // Add the HorizontalLayoutGroup here.
                if (originalButton != null)
                {
                    // Create a new GameObject as the parent of the buttons.
                    buttonParent = new GameObject("ButtonParent");
                    buttonParent.transform.SetParent(originalButton.transform.parent, true);
                    buttonParent.transform.localPosition = originalButton.transform.localPosition;
                    if (buttonParent.GetComponent<RectTransform>() == null)
                    {
                        var rectTransform = buttonParent.AddComponent<RectTransform>();
                        rectTransform.sizeDelta = new Vector2(10, 30);

                        // Anchor to bottom left of the screen.
                        rectTransform.anchorMin = new Vector2(0, 0); // The minimum anchor point (x, y) where (0, 0) is the bottom left.
                        rectTransform.anchorMax = new Vector2(0, 0); // The maximum anchor point (x, y) where (0, 0) is the bottom left.

                        // Offset from the anchor point
                        rectTransform.anchoredPosition = originalButton.GetComponent<RectTransform>().anchoredPosition;

                    }

                    // Move the original button to the new parent.
                    originalButton.transform.SetParent(buttonParent.transform, false);

                    // Add the HorizontalLayoutGroup to the new parent.
                    var horizontalLayoutGroup = buttonParent.AddComponent<HorizontalLayoutGroup>();
                    horizontalLayoutGroup.spacing = 150; // Set your desired spacing.
                    horizontalLayoutGroup.childForceExpandWidth = false; // Prevents the buttons from expanding to fill the horizontal space.
                    horizontalLayoutGroup.childForceExpandHeight = true;

                    Functions.UpdateButtons();
                }
            }

            currentScene = newScene.name;
        }

        private void CleanupButtons()
        {
            foreach (GameObject button in clonedButtons.Where(button => button != null))
            {
                Destroy(button);
            }

            clonedButtons.Clear();
        }


        #region ConfigOptions

        internal static ConfigEntry<string> ButtonUrls;
        internal static ConfigEntry<string> ButtonTexts;


        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description)
        {
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, description);

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description)
        {
            return config(group, name, value, new ConfigDescription(description));
        }

        internal ConfigEntry<T> TextEntryConfig<T>(string group, string name, T value, string desc,
            bool synchronizedSetting = true)
        {
            ConfigurationManagerAttributes attributes = new()
            {
                CustomDrawer = TextAreaDrawer
            };
            return config(group, name, value, new ConfigDescription(desc, null, attributes));
        }

        internal static void TextAreaDrawer(ConfigEntryBase entry)
        {
            GUILayout.ExpandHeight(true);
            GUILayout.ExpandWidth(true);
            entry.BoxedValue = GUILayout.TextArea((string)entry.BoxedValue, GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order;
            [UsedImplicitly] public bool? Browsable;
            [UsedImplicitly] public string? Category;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
        }

        #endregion
    }
}