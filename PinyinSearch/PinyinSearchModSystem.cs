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

	public override async void StartClientSide(ICoreClientAPI api) {
		Api = api;
		try {
			Config = api.LoadModConfig<Config?>("PinyinSearch.json") ?? new();
		} catch (Exception) {
			Config = new();
		}
		api.StoreModConfig(Config, "PinyinSearch.json");

		await Task.Run(() => {
			try {
				var t = Stopwatch.StartNew();
				Matcher = new (HanziPinyinMap.Default, new() {
					EnableFuzzyInitials = Config.EnableFuzzyInitials,
					EnableFuzzyFinals = Config.EnableFuzzyFinals
				});
				t.Stop();
				api.Logger.Debug($"[PinyinSearch] 加载拼音数据完成，耗时{t.ElapsedMilliseconds}ms");
			} catch (Exception e) {
				api.Logger.Error($"[PinyinSearch] 加载拼音数据失败: {e.Message}");
			}
		});

		if (Matcher == null) {
			api.Logger.Error("[PinyinSearch] 拼音匹配器初始化失败");
			return;
		}

		_harmony.PatchAllUncategorized();
	}

	public override void Dispose() { _harmony.UnpatchAll(); }
}