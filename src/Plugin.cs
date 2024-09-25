using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LobbyListCleaner.src.Patches;

namespace LobbyListCleaner.src
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; } = null!;
        internal static Harmony? Harmony { get; set; }
        internal new static ManualLogSource Logger { get; private set; } = null!;
        internal new static ConfigFile Config { get; private set; } = null!;

        // TODO: Add your configuration options here

        private void Awake()
        {
            Instance = this;
            Logger = base.Logger;

            Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            Harmony.PatchAll(typeof(TestPatches));

            Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
        }
    }
}

