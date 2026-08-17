using System.Runtime.CompilerServices;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace PinyinSearch;

[HarmonyPatch(typeof(GuiDialog), nameof(GuiDialog.CountMatches))]
public static class GuiDialogPostfix {
	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "currentSearchText")]
	public static extern string GetCurrentSearchText(GuiDialogHandbook instance);

	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "currentSearchText")]
	public static extern string GetCurrentSearchText(GuiDialogTrader instance);

	[HarmonyPostfix]
	public static void GuiDialogHandbookPostfix(
		GuiDialog __instance,
		ref int __result,
		string text) {
		switch (__instance) {
			case GuiDialogHandbook handbook:
				__result += PinyinSearchModSystem.Matcher?.CountMatches(text, GetCurrentSearchText(handbook)) * 100 ?? 0; break;
			case GuiDialogTrader trader:
				__result += PinyinSearchModSystem.Matcher?.CountMatches(text, GetCurrentSearchText(trader)) * 100 ?? 0; break;
		}
	}
}