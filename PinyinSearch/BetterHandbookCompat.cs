using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using HarmonyLib;

namespace PinyinSearch;

[HarmonyPatchCategory("betterhandbook")]
public static class BetterHandbookCompat {
	static private string _searchText = string.Empty;

	[HarmonyPatch("HandbookCache.HandbookFilterCachePatch", "BuildResults")]
	[HarmonyPrefix]
	public static void BuildResultsPrefix(string searchText) {
		_searchText = searchText;
	}

	[HarmonyPatch("HandbookCache.HandbookFilterCachePatch", "BuildResults")]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> BuildResultsTranspiler(IEnumerable<CodeInstruction> instructions) {
		var originalMethod = AccessTools.Method("HandbookCache.HandbookFilterCachePatch:CountMatches");
		var customMethod = AccessTools.Method(typeof(BetterHandbookCompat), nameof(CustomCountMatches));

		foreach (var instruction in instructions) {
			if (instruction.Calls(originalMethod)) {
				yield return new(OpCodes.Call, customMethod);
			} else {
				yield return instruction;
			}
		}
	}

	public static int CustomCountMatches(string text, Regex regex) {
		var originalCount = regex.Count(text);
		var pinyinCount = PinyinSearchModSystem.Matcher?.CountMatches(text, _searchText) * 100 ?? 0;
		return originalCount + pinyinCount;
	}
}