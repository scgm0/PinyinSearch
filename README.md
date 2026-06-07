# 拼音搜索

为复古物语中的搜索界面添加了拼音搜索的功能，目前支持原版的创造背包和生存手册，对模组添加的搜索界面的支持尚不明确

核心匹配逻辑使用了[ZeroPinyin](http://github.com/scgm0/ZeroPinyin)，为拼音搜索提供了高效快速的即时匹配

其内置的拼音数据来自[pinyin-data](https://github.com/mozillazg/pinyin-data)，覆盖了约4.4万字，如遇到无法匹配的情况，请在[ZeroPinyin](http://github.com/scgm0/ZeroPinyin)的仓库中创建[issue](https://github.com/scgm0/ZeroPinyin/issues)详细说明

配置文件: `ModConfig/PinyinSearch.json`

```
{
  "EnableFuzzyInitials": true, // 开启声母模糊音
  "EnableFuzzyFinals": true, // 开启韵母模糊音
}
```

兼容的模组:

1. [BetterHandbook](https://mods.vintagestory.at/show/mod/50652)
