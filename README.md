# UziStormwerkzElcanScope

SPT 4.1.x（ServerMod，C#）插件：

1. 让 UZI StormWerkz 瞄具基座（顶盖导轨）支持安装 ELCAN SpecterDR 1x/4x 瞄准镜及其 FDE 变体。
2. 把 UZI StormWerkz 顶盖导轨的 id 加进 CR 200DS 转轮手枪的 `mod_sight_front` 前准星槽 Filter 白名单。

## 原理

往目标槽的 `Slot.Properties.Filters[].Filter`（`HashSet<MongoId>`）追加物品 id。幂等：已存在则不重复添加。

| 物品 | id | 角色 |
| --- | --- | --- |
| UZI StormWerkz 顶盖导轨（基座） | `6698c90829e062525d0ad8ad` | mod_scope 槽接收方 / CR 200DS mod_sight_front 白名单新增项 |
| ELCAN SpecterDR 1x/4x | `57ac965c24597706be5f975c` | mod_scope 槽允许的瞄具 |
| ELCAN SpecterDR 1x/4x FDE | `57aca93d2459771f2c7e26db` | mod_scope 槽允许的瞄具 |
| CR 200DS（Chiappa Rhino 200DS 9x19 revolver） | `624c2e8614da335f1e034d8c` | 目标武器（仅作为槽位承载者） |

## 构建

需要本机 `dotnet`（net10.0 目标）。依赖通过 NuGet 拉取（`SPTarkov.Server.Core` / `SPTarkov.DI` / `SPTarkov.Common`，版本对齐服务端 4.1.2），**无需本地 SPT dll**：

```bash
dotnet restore
dotnet build -c Release
```

产物：`bin/Release/UziStormwerkzElcanScope.dll`。

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
[Info][Xidong.UZI.ELCAN.UziStormwerkzElcanScopePlugin] UziStormwerkzElcanScope: added '6698c90829e062525d0ad8ad' to slot 'mod_sight_front' (Chiappa Rhino 200DS 9x19 revolver)
```
