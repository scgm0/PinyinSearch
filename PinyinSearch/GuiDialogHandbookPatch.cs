using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace PinyinSearch;

[HarmonyPatch(typeof(GuiDialogHandbook), "CountMatches")]
public static class GuiDialogHandbookPostfix {
	[HarmonyPostfix]
	public static void Postfix(ICoreClientAPI ___capi, ref int __result, string text, ref string ___currentSearchText) {
		__result += PinyinSearchModSystem.Matcher?.CountMatches(text, ___currentSearchText) * 100 ?? 0;
	}
}
