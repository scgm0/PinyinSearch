using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using HarmonyLib;
using Vintagestory.API.Client;

namespace PinyinSearch;

[HarmonyPatch(typeof(GuiDialog), nameof(GuiDialog.CountMatches))]
public static class GuiDialogPostfix {
	static private readonly ConcurrentDictionary<string, string[]> WordCache = new();

	[HarmonyPostfix]
	public static void CountMatchesPostfix(string text, Regex? regex, ref int __result) {
		var matcher = PinyinSearchModSystem.Matcher;
		if (matcher == null || string.IsNullOrEmpty(text) || regex == null) {
			return;
		}

		var words = ExtractSearchWords(regex);
		if (words.Length == 0) {
			return;
		}

		var bonus = 0;
		foreach (var word in words) {
			bonus += matcher.CountMatches(text, word) * 100;
		}

		__result += bonus;
	}

	static private string[] ExtractSearchWords(Regex regex) {
		var pattern = regex.ToString();
		return WordCache.TryGetValue(pattern, out var cached) ? cached : WordCache.GetOrAdd(pattern, static p => ParseWords(p));
	}

	static private string[] ParseWords(string pattern) {
		if (pattern.Length < 2 || pattern[0] != '(' || pattern[^1] != ')') {
			return [];
		}

		var inner = pattern.AsSpan(1, pattern.Length - 2);
		var words = new List<string>();
		var start = 0;

		for (var i = 0; i <= inner.Length; i++) {
			var atEnd = i == inner.Length;
			var isSep = !atEnd && inner[i] == '|' && (i == 0 || inner[i - 1] != '\\');
			if (!atEnd && !isSep) {
				continue;
			}

			var span = inner[start..i];
			start = i + 1;

			if (span.Length >= 2 && span.StartsWith("\\b")) {
				span = span[2..];
			}

			if (span.Length >= 2 && span.EndsWith("\\b")) {
				span = span[..^2];
			}

			if (span.IsEmpty) {
				continue;
			}

			string word;
			try {
				word = Regex.Unescape(span.ToString());
			} catch (ArgumentException) {
				continue;
			}

			if (word.Length > 0) {
				words.Add(word.ToLowerInvariant());
			}
		}

		return [.. words];
	}
}
