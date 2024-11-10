using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Steamworks.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// TODO List:
// - Either turn into a config option or have temporary and permanent block lists
// - Config option to choose if reload after each block
// - Config option to choose if blocking by name or by Steam ID

// Inspired by https://github.com/1A3Dev/LC-LobbyImprovements
namespace LobbyListCleaner
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin? Instance { get; private set; }
        private readonly Harmony harmony = new(MyPluginInfo.PLUGIN_GUID);
        internal static ManualLogSource? MikseLogger { get; private set; }
        private static bool initialized;
        internal static ConfigFile? MikseConfig { get; private set; }
        public ConfigEntry<bool>? ReloadAfterEachBlock { get; private set; }
        public ConfigEntry<bool>? BlockByNameOrSteamID { get; private set; }
        // public static ConfigEntry<string> filteredLobbyNames;
        // public static ConfigEntry<string> filteredSteamIDs;
        public static string[]? blockedLobbyNames;
        public static string[]? activeFilter;

        private void Awake()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            Instance = this;
            MikseLogger = Logger;
            MikseConfig = Config;

            ReloadAfterEachBlock = Config.Bind("General", "Reload after filtering", true, "Reload the lobby list each time a lobby is added to the filter list.");

            BlockByNameOrSteamID = Config.Bind("General", "Block by name or Steam ID", true, "Choose if you want to block lobbies by name or by Steam ID. true = name, false = Steam ID");

            // filteredLobbyNames = MyConfig.Bind("Lobby Names", "Filter", "", "Lobby names to filter out of the lobby list. Separate multiple names with a comma.");
            // filteredLobbyNames.SettingChanged += (sender, args) =>
            // {
            //     FilterListAndUpdate();
            // };

            // FilterListAndUpdate();

            Assembly assembly = Assembly.GetExecutingAssembly();
            harmony.PatchAll(assembly);


            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }

        // private static void FilterListAndUpdate()
        // {
        //     string[] names = filteredLobbyNames.Value.Split(',');
        //     filteredLobbyNamesParsed = new string[names.Length];
        //     for (int i = 0; i < names.Length; i++)
        //     {
        //         filteredLobbyNamesParsed[i] = names[i].Trim();
        //     }

        //     SteamLobbyManager lobbyManager = UnityEngine.Object.FindObjectOfType<SteamLobbyManager>();
        //     if (lobbyManager != null)
        //     {
        //         filteredLobbyNames.Value = "example1,example2,example3";
        //     }
        // }
    }

    [HarmonyPatch]
    internal class Patches
    {
        // Filter lobbylist by blocked names
        [HarmonyPatch(typeof(SteamLobbyManager), "loadLobbyListAndFilter")]
        [HarmonyPrefix]
        private static void Prefix(ref Lobby[] lobbyList)
        {
            if (Plugin.blockedLobbyNames == null || Plugin.blockedLobbyNames.Length == 0)
            {
                return;
            }

            lobbyList = lobbyList.Where(lobby =>
            {
                string lobbyName = lobby.GetData("name").ToLower();
                return !Plugin.blockedLobbyNames.Any(blockedName => lobbyName.Contains(blockedName));
            }).ToArray();
        }

        // Add block buttons to lobby list
        [HarmonyPatch(typeof(SteamLobbyManager), "loadLobbyListAndFilter")]
        [HarmonyPostfix]
        private static IEnumerator PostFix(IEnumerator result)
        {
            while (result.MoveNext())
            {
                yield return result.Current;
            }
            var textLabels = new string[] { "Block", "Success!", "Invalid" };

            LobbySlot[] lobbySlots = UnityEngine.Object.FindObjectsOfType<LobbySlot>();
            foreach (LobbySlot lobbySlot in lobbySlots)
            {
                Button? joinButton = lobbySlot.transform.Find("JoinButton")?.GetComponent<Button>();
                if (joinButton && !lobbySlot.transform.Find("BlockNameButton"))
                {
                    if (joinButton != null)
                    {
                        var BlockNameButton = Object.Instantiate(joinButton, joinButton.transform.parent);


                        BlockNameButton.name = "BlockNameButton";
                        RectTransform rectTransform = BlockNameButton.GetComponent<RectTransform>();
                        rectTransform.anchoredPosition -= new Vector2(78f, 0f);
                        var BlockNameTextMesh = BlockNameButton.GetComponentInChildren<TextMeshProUGUI>();
                        BlockNameTextMesh.text = textLabels[0];
                        BlockNameButton.onClick = new Button.ButtonClickedEvent();
                        BlockNameButton.onClick.AddListener(() => AddNameToBlockList(lobbySlot.LobbyName));
                    }
                }
            }

        }

        internal static void AddNameToBlockList(TextMeshProUGUI lobbyName)
        {
            Plugin.blockedLobbyNames = Plugin.blockedLobbyNames.AddToArray(lobbyName.text.ToLower());
            Plugin.MikseLogger?.LogInfo(lobbyName.text + " added to block list");
            // Plugin.MyConfig.Reload();

            // Update the lobby list
            SteamLobbyManager lobbyManager = UnityEngine.Object.FindObjectOfType<SteamLobbyManager>();
            lobbyManager?.RefreshServerListButton();

            Plugin.activeFilter = Plugin.blockedLobbyNames;
        }
    }
}