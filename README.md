# UziStormwerkzElcanScope

SPT 4.1.x（ServerMod，C#）插件：

1. 让 UZI StormWerkz 瞄具基座（顶盖导轨）支持安装 ELCAN SpecterDR 1x/4x 瞄准镜及其 FDE 变体，以及 SIG Sauer BRAVO4 4x30 瞄准镜。
2. 把 UZI StormWerkz 顶盖导轨和 MP-18 瞄具基座的 id 加进 CR 200DS 转轮手枪的 `mod_sight_front` 前准星槽 Filter 白名单。
3. 把莫辛纳甘的 4 种尺寸枪管 id 加进 PPSh-41 冲锋枪的 `mod_barrel` 枪管槽 Filter 白名单。
4. 把 Benelli M3 可伸缩枪托、PKM / PKP 枪托、Ultima MP-155 塑料手枪式握把与 KS-23 金属枪托的 id 加进 PPSh-41 冲锋枪的 `mod_stock` 枪托槽 Filter 白名单。
5. 让 HUXWRX HX-QD 消音器（含黄褐色变体）与 PPSh-41 防尘盖互不兼容。
6. 让 Aim Sports“三轨”莫辛步枪导轨的第一个战术配件槽（`mod_tactical_000`）支持安装 20 款前握把（Zenit RK 系列、镂空型、垂直型、BCM MOD.3、TangoDown Stubby BGV-MK46K）。
7. 让 UZI StormWerkz 护木底轨的 `mod_tactical` 槽支持安装 Zenit RK 系列前握把。

## 原理

往目标槽的 `Slot.Properties.Filters[].Filter`（`HashSet<MongoId>`）追加物品 id；不兼容关系则往 `TemplateItem.Properties.ConflictingItems`（`HashSet<MongoId>`）追加物品 id。均幂等：已存在则不重复添加。

| 物品 | id | 角色 |
| --- | --- | --- |
| UZI StormWerkz 顶盖导轨（基座） | `6698c90829e062525d0ad8ad` | `mod_scope` 槽接收方 / CR 200DS `mod_sight_front` 白名单新增项 |
| MP-18 瞄具基座 | `61f804acfcba9556ea304cb8` | CR 200DS `mod_sight_front` 白名单新增项；自身提供 `mod_scope` 槽 |
| ELCAN SpecterDR 1x/4x | `57ac965c24597706be5f975c` | mod_scope 槽允许的瞄具 |
| ELCAN SpecterDR 1x/4x FDE | `57aca93d2459771f2c7e26db` | mod_scope 槽允许的瞄具 |
| SIG Sauer BRAVO4 4x30 瞄准镜 | `57adff4f24597737f373b6e6` | mod_scope 槽允许的瞄具 |
| CR 200DS（Chiappa Rhino 200DS 9x19 revolver） | `624c2e8614da335f1e034d8c` | 目标武器（仅作为槽位承载者） |
| PPSh-41 冲锋枪 | `5ea03f7400685063ec28bfa8` | `mod_barrel` / `mod_stock` 槽接收方（仅作为槽位承载者） |
| 莫辛纳甘 200mm 锯短枪管 | `5bfd4cc90db834001d23e846` | PPSh-41 `mod_barrel` 白名单新增项 |
| 莫辛纳甘 220mm 锯短螺纹枪管 | `5bfd4cd60db834001c38f095` | PPSh-41 `mod_barrel` 白名单新增项 |
| 莫辛纳甘 514mm 卡宾枪管 | `5bfd4cbe0db834001b73449f` | PPSh-41 `mod_barrel` 白名单新增项 |
| 莫辛纳甘 730mm 标准枪管 | `5ae09bff5acfc4001562219d` | PPSh-41 `mod_barrel` 白名单新增项 |
| Benelli M3 可伸缩枪托 | `6259c3387d6aab70bc23a18d` | PPSh-41 `mod_stock` 白名单新增项 |
| PKM 木制枪托 | `646371a9f2404ab67905c8e6` | PPSh-41 `mod_stock` 白名单新增项 |
| Zenit PT-2 "Klassika" PK 机枪枪托 | `6492d7847363b8a52206bc52` | PPSh-41 `mod_stock` 白名单新增项 |
| PKP 聚合物枪托 | `6492e3a97df7d749100e29ee` | PPSh-41 `mod_stock` 白名单新增项 |
| Ultima MP-155 塑料手枪式握把 | `606eef46232e5a31c233d500` | PPSh-41 `mod_stock` 白名单新增项；自带 `mod_stock` 槽 |
| KS-23 金属枪托 | `5e848dc4e4dbc5266a4ec63d` | PPSh-41 `mod_stock` 白名单新增项；原版仅可装于 KS-23M 聚合物手枪式握把的 `mod_stock` 槽 |
| PPSh-41 防尘盖 | `5ea03e5009aa976f2e7a514b` | `mod_reciever` 槽承载物品；与 HUXWRX HX-QD 消音器互斥 |
| HUXWRX HX-QD 7.62x51 消音器 | `6a158e4abf497aade10030e0` | 由 WTT-ContentBackport 注入；与 PPSh-41 防尘盖互斥 |
| HUXWRX HX-QD 7.62x51 消音器（黄褐色） | `6a1eb32c6cd328ea90037455` | 由 WTT-ContentBackport 注入；与 PPSh-41 防尘盖互斥 |
| Aim Sports“三轨”莫辛步枪导轨 | `5bbdb811d4351e45020113c7` | `mod_tactical_000` 槽接收方 |
| Zenit RK-0 前握把 | `5c1bc4812e22164bef5cfde7` | Aim Sports 三轨 `mod_tactical_000` 白名单新增项 |
| Zenit RK-1 前握把 | `5c1bc5612e221602b5429350` | 同上 |
| Zenit RK-2 前握把 | `5c1bc5af2e221602b412949b` | 同上 |
| Zenit RK-4 前握把 | `5c1bc5fb2e221602b1779b32` | 同上 |
| Zenit RK-5 前握把 | `5c1bc7432e221602b412949d` | 同上 |
| Zenit RK-6 前握把 | `5c1bc7752e221602b1779b34` | 同上 |
| Tactical Dynamics 镂空前握把 | `5f6340d3ca442212f4047eb2` | 同上 |
| BCM GUNFIGHTER MOD 3 vertical 前握把 | `5c7fc87d2e221644f31c0298` | 同上 |
| KAC 垂直前握把 | `5c87ca002e221600114cb150` | 同上 |
| ASh-12 垂直前握把 | `5cda9bcfd7f00c0c0b53e900` | 同上 |
| A3 Tactical MVF001 KeyMod 垂直前握把（黑色） | `5fc0f9b5d724d907e2077d82` | 同上 |
| SIG Sauer KeyMod 垂直握把（黑色） | `5fc0f9cbd6fa9c00c571bb90` | 同上 |
| Monstrum Tactical KeyMod 垂直前握把 | `615d8fd3290d254f5e6b2edc` | 同上 |
| Daniel Defense Enhanced M-LOK 垂直前握把（黑色） | `651a8bf3a8520e48047bf708` | 同上 |
| Daniel Defense Enhanced M-LOK 垂直前握把（狼棕色） | `651a8e529829226ceb67c319` | 同上 |
| BCM GUNFIGHTER MOD 3 M-LOK 前握把（黑色） | `665d5d9e338229cfd6078da1` | 同上 |
| BCM GUNFIGHTER MOD 3 M-LOK 前握把（FDE） | `665edce564fb556f940ab32a` | 同上 |
| TangoDown Stubby BGV-MK46K 前握把（黑色） | `558032614bdc2de7118b4585` | 同上 |
| TangoDown Stubby BGV-MK46K 前握把（FDE） | `58c157be86f77403c74b2bb6` | 同上 |
| TangoDown Stubby BGV-MK46K 前握把（哑光灰） | `58c157c886f774032749fb06` | 同上 |
| UZI StormWerkz 护木底轨 | `66992f7d9950f5f4cd0602a8` | `mod_tactical` 槽接收方 |
| Zenit RK-0 / RK-1 / RK-2 / RK-4 / RK-5 / RK-6 前握把 | `5c1bc481…` / `5c1bc561…` / `5c1bc5af…` / `5c1bc5fb…` / `5c1bc743…` / `5c1bc775…` | UZI 护木底轨 `mod_tactical` 白名单新增项（6 款） |

## 构建

需要本机 `dotnet`（net10.0 目标）。依赖通过 NuGet 拉取（`SPTarkov.Server.Core` / `SPTarkov.DI` / `SPTarkov.Common`，版本对齐服务端 4.1.2），**无需本地 SPT dll**：

```bash
dotnet restore
dotnet build -c Release
```

产物：`bin/Release/UziStormwerkzElcanScope.dll`。

## 测试

单元测试位于 `tests/UziStormwerkzElcanScope.Tests/`，用最小物品/槽位夹具直接驱动插件的槽位注入，覆盖莫辛枪管注入、幂等，以及 ELCAN / CR 200DS 既有行为回归：

```bash
dotnet test tests/UziStormwerkzElcanScope.Tests/UziStormwerkzElcanScope.Tests.csproj
```

## 提交钩子

仓库自带 `.githooks/`（`commit-msg` 提交信息语法 + 排版检查，`pre-commit` 暂存文件排版检查），源自模板 `~/模板/project-repo/`。克隆后启用：

```bash
git config core.hooksPath .githooks
```

- `scripts/check-grammar.mjs`：校验提交信息语法（`【类型，范围】摘要`）。
- `autocorrect --lint`：校验汉字与英文、数字、符号间空格排版。

绕过：`git commit --no-verify`。

## 自动发布（GitHub Actions）

`.github/workflows/build.yml` 在推送 `v*` 标签或手动触发时自动构建，并把 dll 按游戏根目录结构打成 zip：

```
UziStormwerkzElcanScope-V{版本}.zip
└── SPT_Runtime
    └── user
        └── mods
            └── UziStormwerkzElcanScope
                └── UziStormwerkzElcanScope.dll
```

- 上传该 zip 为 workflow artifact
- 创建一个 GitHub Release 并把 zip 作为附件（仅 tag 触发）

`Version`（csproj）与标签保持一致（如 `v2.0.0` 对应 `<Version>2.0.0</Version>`）。解压 zip 到游戏根目录（让 `SPT_Runtime/user/mods/UziStormwerkzElcanScope/` 与服务端 mod 根目录平级）即可。

## 部署

将 dll 放入服务端 `user/mods/UziStormwerkzElcanScope/UziStormwerkzElcanScope.dll`，重启 `pj568-spt-server`：

```bash
systemctl --user restart pj568-spt-server
```

## 验证

启动日志（`user/logs/spt/sptYYYYMMDD.log`）应出现：

```
[Info][ModValidator] 模组：UziStormwerkzElcanScope ... 已加载
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added '57ac965c24597706be5f975c' to slot 'mod_scope' ...
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added '57aca93d2459771f2c7e26db' to slot 'mod_scope' ...
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added '57adff4f24597737f373b6e6' to slot 'mod_scope' ...
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added '6698c90829e062525d0ad8ad' to slot 'mod_sight_front' (weapon_chiappa_rhino_200ds_9x19)
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added '61f804acfcba9556ea304cb8' to slot 'mod_sight_front' (weapon_chiappa_rhino_200ds_9x19)
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added '5bfd4cc90db834001d23e846' to slot 'mod_barrel' (weapon_zis_ppsh41_762x25)
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added '5bfd4cd60db834001c38f095' to slot 'mod_barrel' (weapon_zis_ppsh41_762x25)
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added '5bfd4cbe0db834001b73449f' to slot 'mod_barrel' (weapon_zis_ppsh41_762x25)
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added '5ae09bff5acfc4001562219d' to slot 'mod_barrel' (weapon_zis_ppsh41_762x25)
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added '6259c3387d6aab70bc23a18d' to slot 'mod_stock' (weapon_zis_ppsh41_762x25)
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added '646371a9f2404ab67905c8e6' to slot 'mod_stock' (weapon_zis_ppsh41_762x25)
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added '6492d7847363b8a52206bc52' to slot 'mod_stock' (weapon_zis_ppsh41_762x25)
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added '6492e3a97df7d749100e29ee' to slot 'mod_stock' (weapon_zis_ppsh41_762x25)
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added '606eef46232e5a31c233d500' to slot 'mod_stock' (weapon_zis_ppsh41_762x25)
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added '5e848dc4e4dbc5266a4ec63d' to slot 'mod_stock' (weapon_zis_ppsh41_762x25)
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added conflicting item '6a158e4abf497aade10030e0' to 'PPSh-41 dust cover' (reciever_ppsh_zis_ppsh41_std)
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added conflicting item '6a1eb32c6cd328ea90037455' to 'PPSh-41 dust cover' (reciever_ppsh_zis_ppsh41_std)
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added conflicting item '5ea03e5009aa976f2e7a514b' to 'HUXWRX HX-QD' ...
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added conflicting item '5ea03e5009aa976f2e7a514b' to 'HUXWRX HX-QD (Tan)' ...
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added '5c1bc4812e22164bef5cfde7' to slot 'mod_tactical_000' (mount_mosin_aim_sports_tri_rail)
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added '5c1bc5612e221602b5429350' to slot 'mod_tactical_000' (mount_mosin_aim_sports_tri_rail)
...（mod_tactical_000 共 20 条）...
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added '5c1bc4812e22164bef5cfde7' to slot 'mod_tactical' (mount_uzi_stormwerkz_lower_handguard_rail)
...（UZI 护木底轨 mod_tactical 共 6 条）...
```
