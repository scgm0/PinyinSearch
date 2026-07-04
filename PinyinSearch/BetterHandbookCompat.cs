using HarmonyLib;

namespace PinyinSearch;

[HarmonyPatchCategory("betterhandbook")]
public static class BetterHandbookCompat {
	private static string _searchText = string.Empty;

	[HarmonyPatch("HandbookCache.HandbookFilterCachePatch", "BuildResults")]
	[HarmonyPrefix]
	public static void BuildResultsPrefix(string searchText) {
		_searchText = searchText;
	}

	[HarmonyPatch("HandbookCache.HandbookFilterCachePatch", "CountMatches")]
	[HarmonyPostfix]
	public static void CountMatchesPostfix(ref int __result, string text) {
		__result += PinyinSearchModSystem.Matcher?.CountMatches(text, _searchText) * 100 ?? 0;
	}
}