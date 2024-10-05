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

// Inspired by https://github.com/1A3Dev/LC-LobbyImprovements
namespace LobbyListCleaner
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        private readonly Harmony harmony = new(MyPluginInfo.PLUGIN_GUID);
        internal static ManualLogSource MyLogger { get; private set; }
        internal static ConfigFile MyConfig { get; private set; }
        private static bool initialized;
        // public static ConfigEntry<string> filteredLobbyNames;
        public static string[]? filteredLobbyNamesParsed;
        public static string[]? activeFilter;

        private void Awake()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            Instance = this;
            MyLogger = Logger;
            MyConfig = Config;

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
        // Filter out lobbies with names in the block list
        [HarmonyPatch(typeof(SteamLobbyManager), "loadLobbyListAndFilter")]
        [HarmonyPrefix]
        private static void Prefix(ref Lobby[] lobbyList)
        {
            if (Plugin.filteredLobbyNamesParsed == null || Plugin.filteredLobbyNamesParsed.Length == 0)
            {
                return;
            }

            lobbyList = lobbyList.Where(lobby =>
            {
                string lobbyName = lobby.GetData("name");
                return !Plugin.filteredLobbyNamesParsed.Any(blockedName => lobbyName.Contains(blockedName));
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
            var textLabels = new string[] { "Block", "Blocked!", "Invalid" };

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
            Plugin.filteredLobbyNamesParsed = Plugin.filteredLobbyNamesParsed.AddToArray(lobbyName.text);
            Plugin.MyLogger.LogInfo(lobbyName.text + " added to block list");
            // Plugin.MyConfig.Reload();

            // Update the lobby list
            SteamLobbyManager lobbyManager = UnityEngine.Object.FindObjectOfType<SteamLobbyManager>();
            lobbyManager?.RefreshServerListButton();

            Plugin.activeFilter = Plugin.filteredLobbyNamesParsed;
        }
    }
}

