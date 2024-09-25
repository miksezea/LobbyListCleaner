using HarmonyLib;
using Steamworks.Data;
using System.Collections.Generic;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LobbyListCleaner.src.Patches
{
    internal class TestPatches
    {
        // Taken from https://github.com/VisualError/Better-Lobbies/blob/204d4c028b2a1864c84294941ca51e7993e5f4dd/Patches/LobbyPatches.cs
        [HarmonyPatch(typeof(SteamLobbyManager), nameof(SteamLobbyManager.loadLobbyListAndFilter), MethodType.Enumerator)]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> loadLobbyListAndFilter_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var currentLobbyListField =
            AccessTools.Field(typeof(SteamLobbyManager), nameof(SteamLobbyManager.currentLobbyList));
            var thisLobbyField =
                AccessTools.Field(typeof(LobbySlot), nameof(LobbySlot.thisLobby));

            var initializeLobbySlotMethod =
                AccessTools.Method(typeof(TestPatches), nameof(InitializeLobbySlot));

            // Does the following:
            // - Adds dup before last componentInChildren line to keep componentInChildren value on the stack
            // - Calls InitializeLobbySlot(lobbySlot)

            return new CodeMatcher(instructions)

                .MatchForward(false, [
                new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(OpCodes.Ldfld, currentLobbyListField),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(inst => inst.opcode == OpCodes.Ldfld), // Compiler-generated field
                new CodeMatch(OpCodes.Ldelem, typeof(Lobby)),
                new CodeMatch(OpCodes.Stfld, thisLobbyField) ])
                .ThrowIfNotMatch("Unable to find LobbySlot.thisLobby line.")
                .InsertAndAdvance([
                new CodeInstruction(OpCodes.Dup) ])
                .Advance(6)
                .InsertAndAdvance([
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Call, initializeLobbySlotMethod) ])
                .InstructionEnumeration();
        }

        private void InitializeLobbySlot(LobbySlot lobbySlot)
        {
            lobbySlot.playerCount.text = string.Format("{0} / {1}", lobbySlot.thisLobby.MemberCount, lobbySlot.thisLobby.MaxMembers);
            var JoinButton = lobbySlot.GetComponentInChildren<Button>();
            if (JoinButton != null)
            {
                var HideLobbyButton = Object.Instantiate(JoinButton, JoinButton.transform.parent);
                HideLobbyButton.name = "HideLobbyButton";
                RectTransform rectTransform = HideLobbyButton.GetComponent<RectTransform>();
                rectTransform!.anchoredPosition -= new Vector2(78f, 0f);
                var TextMesh = HideLobbyButton.GetComponentInChildren<TextMeshProUGUI>();
                TextMesh.text = "Code";
                HideLobbyButton!.onClick.m_PersistentCalls.Clear();
                //HideLobbyButton!.onClick.AddListener(() => LobbySlotListeners.CopyLobbyCodeToClipboard(lobbySlot, TextMesh));
            }
        }
    }
}