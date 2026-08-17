using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace PinyinSearch;

[HarmonyPatch(typeof(GuiElementItemSlotGridBase), nameof(GuiElementItemSlotGridBase.FilterItemsBySearchText))]
public static class GuiElementItemSlotGridBaseTranspilerPatch {
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
		var codes = new List<CodeInstruction>(instructions);

		var indexOfIndex = -1;
		var caseInsensitiveContainsIndex = -1;
		var newobjIndex = -1;
		var brfalseIndex = -1;
		Label endOfIfLabel = default;

		for (var i = 0; i < codes.Count; i++) {
			var op = codes[i].opcode;
			var method = codes[i].operand as MethodInfo;

			if (op == OpCodes.Callvirt && method?.Name == "IndexOf" && indexOfIndex == -1) {
				indexOfIndex = i;
			} else if (op == OpCodes.Call && method?.Name == "CaseInsensitiveContains" && caseInsensitiveContainsIndex == -1) {
				caseInsensitiveContainsIndex = i;
				if (i + 1 < codes.Count &&
					(codes[i + 1].opcode == OpCodes.Brfalse || codes[i + 1].opcode == OpCodes.Brfalse_S)) {
					brfalseIndex = i + 1;
					if (codes[i + 1].operand is Label label) {
						endOfIfLabel = label;
					} else {
						var found = false;
						for (var j = brfalseIndex + 1; j < codes.Count; j++) {
							if (codes[j].opcode == OpCodes.Callvirt &&
								(codes[j].operand as MethodInfo)?.Name == "MoveNext") {
								endOfIfLabel = il.DefineLabel();
								codes[j - 1].labels.Add(endOfIfLabel);
								found = true;
								break;
							}
						}

						if (!found) {
							brfalseIndex = -1;
						}
					}
				}
			} else if (op == OpCodes.Newobj && (codes[i].operand as ConstructorInfo)?.DeclaringType?.Name == "WeightedSlot" &&
				newobjIndex == -1) {
				newobjIndex = i;
			}
		}

		if (indexOfIndex == -1 || caseInsensitiveContainsIndex == -1 || newobjIndex == -1 || brfalseIndex == -1) {
			return codes;
		}

		var injectBeforeIndex = -1;
		CodeInstruction? searchCacheNameLoadInst = null;
		for (var i = indexOfIndex - 1; i >= 0; i--) {
			if (codes[i].IsLdloc()) {
				searchCacheNameLoadInst = new(codes[i].opcode, codes[i].operand);
				injectBeforeIndex = i;
				break;
			}
		}

		CodeInstruction? text1LoadInst = null;
		for (var i = caseInsensitiveContainsIndex - 1; i >= 0; i--) {
			if (codes[i].IsLdloc()) {
				text1LoadInst = new(codes[i].opcode, codes[i].operand);
				break;
			}
		}

		CodeInstruction? getKeyInst = null;
		CodeInstruction? availableSlotLoadInst = null;
		CodeInstruction? sourceLoadInst = null;
		for (var i = newobjIndex - 1; i >= 0; i--) {
			if (codes[i].opcode == OpCodes.Call && (codes[i].operand as MethodInfo)?.Name == "get_Key") {
				getKeyInst = new(codes[i].opcode, codes[i].operand);
				availableSlotLoadInst = new(codes[i - 1].opcode, codes[i - 1].operand);
				sourceLoadInst = new(codes[i - 2].opcode, codes[i - 2].operand);
				break;
			}
		}

		CodeInstruction? itemSlotLoadInst = null;
		for (var i = newobjIndex + 1; i < codes.Count; i++) {
			if (codes[i].IsLdloc()) {
				itemSlotLoadInst = new(codes[i].opcode, codes[i].operand);
				break;
			}
		}

		if (injectBeforeIndex == -1 || sourceLoadInst == null || searchCacheNameLoadInst == null || text1LoadInst == null ||
			getKeyInst == null || availableSlotLoadInst == null || itemSlotLoadInst == null) {
			return codes;
		}

		var injection = new List<CodeInstruction>();

		var firstInst = new CodeInstruction(sourceLoadInst.opcode, sourceLoadInst.operand);
		firstInst.labels.AddRange(codes[injectBeforeIndex].labels);
		codes[injectBeforeIndex].labels.Clear();
		injection.Add(firstInst);

		injection.Add(new(availableSlotLoadInst.opcode, availableSlotLoadInst.operand));
		injection.Add(new(getKeyInst.opcode, getKeyInst.operand));
		injection.Add(new(itemSlotLoadInst.opcode, itemSlotLoadInst.operand));
		injection.Add(new(searchCacheNameLoadInst.opcode, searchCacheNameLoadInst.operand));
		injection.Add(new(OpCodes.Ldarg_0));
		injection.Add(new(OpCodes.Ldfld,
			AccessTools.Field(typeof(GuiElementItemSlotGridBase), "searchText")));
		injection.Add(new(text1LoadInst.opcode, text1LoadInst.operand));

		injection.Add(CodeInstruction.Call(typeof(GuiElementItemSlotGridBaseTranspilerPatch), nameof(ProcessItemMatch)));

		injection.Add(new(OpCodes.Br, endOfIfLabel));

		codes.InsertRange(injectBeforeIndex, injection);

		return codes;
	}

	public static void ProcessItemMatch(
		Vintagestory.API.Datastructures.OrderedDictionary<int, WeightedSlot> source,
		int key,
		ItemSlot itemSlot,
		string searchCacheName,
		string searchText,
		string text1) {
		var vanillaWeight = GetVanillaWeight(searchCacheName, searchText, text1);

		if (vanillaWeight == 0.0f) {
			source.Add(key, new() { slot = itemSlot, weight = 0.0f });
			return;
		}

		var pinyinWeight = GetPinyinWeight(searchCacheName, searchText, text1);

		var finalWeight = -1f;
		switch (vanillaWeight) {
			case >= 0 when pinyinWeight >= 0: finalWeight = Math.Min(vanillaWeight, pinyinWeight); break;
			case >= 0: finalWeight = vanillaWeight; break;
			default: {
				if (pinyinWeight >= 0) {
					finalWeight = pinyinWeight;
				}

				break;
			}
		}

		if (finalWeight >= 0) {
			source.Add(key, new() { slot = itemSlot, weight = finalWeight });
		}
	}

	static private float GetVanillaWeight(string searchCacheName, string searchText, string text1) {
		if (string.IsNullOrEmpty(searchCacheName) || string.IsNullOrEmpty(searchText)) {
			return -1f;
		}

		var num = searchCacheName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);

		if (num >= 0) {
			var nameLen = searchCacheName.Length;
			var textLen = searchText.Length;

			if (num == 0) {
				if (nameLen == textLen) {
					return 0.0f;
				}

				if (nameLen > textLen && searchCacheName[textLen] == ' ') {
					return 0.125f;
				}

				return 0.75f;
			}

			if (searchCacheName[num - 1] == ' ') {
				return num + textLen == nameLen ? 0.25f : 0.5f;
			}

			return 1.0f;
		}

		if (!string.IsNullOrEmpty(text1)) {
			if (text1.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)) {
				return 2f;
			}

			if (text1.Contains(searchText, StringComparison.OrdinalIgnoreCase)) {
				return 3f;
			}
		}

		return -1f;
	}

	static private float GetPinyinWeight(string searchCacheName, string searchText, string text1) {
		var matcher = PinyinSearchModSystem.Matcher;
		if (matcher == null) {
			return -1f;
		}

		if (!string.IsNullOrEmpty(searchCacheName)) {
			if (matcher.StartsWith(searchCacheName, searchText)) {
				return matcher.IsMatch(searchCacheName, searchText) ? 0f : 0.1f;
			}

			if (matcher.Contains(searchCacheName, searchText)) {
				return 0.2f;
			}
		}

		if (!string.IsNullOrEmpty(text1)) {
			if (matcher.StartsWith(text1, searchText)) {
				return 1f;
			}

			if (matcher.Contains(text1, searchText)) {
				return 2f;
			}
		}

		return -1f;
	}
}