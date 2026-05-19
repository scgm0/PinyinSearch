/*
using HarmonyLib;
using Vintagestory.API.Util;

namespace PinyinSearch;

[HarmonyPatch(typeof(StringExtensions), nameof(StringExtensions.CaseInsensitiveContains))]
public static class StringExtensionsPostfix {
	[HarmonyPostfix]
	public static void Postfix(string text, string value, ref bool __result) {
		__result = __result || PinyinSearchModSystem.Matcher?.Contains(text, value) is true;
	}
}
*/