using System.Runtime.CompilerServices;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace PinyinSearch;

[HarmonyPatch(typeof(GuiDialog), nameof(GuiDialog.CountMatches))]
public static class GuiDialogPostfix {
	[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "currentSearchText")]
	public static extern string GetCurrentSearchText(GuiDialogHandbook instance);

	[HarmonyPostfix]
	public static void GuiDialogHandbookPostfix(
		GuiDialog __instance,
		ref int __result,
		string text) {
		if (__instance is not GuiDialogHandbook handbook) {
			return;
		}

		__result += PinyinSearchModSystem.Matcher?.CountMatches(text, GetCurrentSearchText(handbook)) * 100 ?? 0;
	}
}