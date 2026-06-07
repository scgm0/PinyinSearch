using System;
using System.Diagnostics;
using System.Threading.Tasks;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using ZeroPinyin;

namespace PinyinSearch;

public class PinyinSearchModSystem : ModSystem {
	private readonly Harmony _harmony = new("pinyinsearch");
	public static PinyinMatcher? Matcher { get; private set; }
	public static ICoreClientAPI? Api { get; private set; }
	public static Config? Config { get; private set; }

	public override void StartPre(ICoreAPI api) {
		Api = api as ICoreClientAPI;
		try {
			Config = api.LoadModConfig<Config?>("PinyinSearch.json") ?? new();
		} catch (Exception) {
			Config = new();
		}

		api.StoreModConfig(Config, "PinyinSearch.json");

		try {
			api.Logger.Debug("[PinyinSearch] 加载拼音数据中...");
			var t = Stopwatch.StartNew();
			Matcher = new(HanziPinyinMap.Default,
				new() {
					EnableFuzzyInitials = Config.EnableFuzzyInitials,
					EnableFuzzyFinals = Config.EnableFuzzyFinals,
					ExactMatchForHanzi = Config.ExactMatchForHanzi,
				});
			t.Stop();
			api.Logger.Debug($"[PinyinSearch] 加载拼音数据完成，耗时{t.ElapsedMilliseconds}ms");
		} catch (Exception e) {
			api.Logger.Error($"[PinyinSearch] 加载拼音数据失败: {e.Message}");
		}

		if (Matcher == null) {
			api.Logger.Error("[PinyinSearch] 拼音匹配器初始化失败");
			return;
		}

		_harmony.PatchAllUncategorized();
		if (api.ModLoader.IsModEnabled("betterhandbook")) {
			_harmony.PatchCategory("betterhandbook");
			api.Logger.Notification("[PinyinSearch] 已启用betterhandbook兼容");
		}

		api.Logger.Notification($"[PinyinSearch] 声母模糊音: {Config.EnableFuzzyInitials} 韵母模糊音: {Config.EnableFuzzyFinals} 汉字精确匹配: {Config.ExactMatchForHanzi}");
		api.Logger.Notification("[PinyinSearch] 拼音搜索初始化完成");
	}

	public override void Dispose() { _harmony.UnpatchAll(); }
}