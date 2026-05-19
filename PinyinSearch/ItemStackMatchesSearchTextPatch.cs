using HarmonyLib;
using Vintagestory.API.Common;

namespace PinyinSearch;

[HarmonyPatch(typeof(ItemStack), nameof(ItemStack.MatchesSearchText))]
public static class ItemStackMatchesSearchTextPostfix {
	public static void Postfix(IWorldAccessor world, string searchText, ItemStack __instance, ref bool __result) {
		var matcher = PinyinSearchModSystem.Matcher;
		__result = __result || matcher?.Contains(__instance.GetName(), searchText) is true || matcher?.Contains(__instance.GetDescription(world, new DummySlot(__instance)), searchText) is true;
	}
}
