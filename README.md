# UziStormwerkzElcanScope

SPT 4.x（ServerMod，C#）插件：让 UZI StormWerkz 瞄具基座（顶盖导轨）支持安装 ELCAN SpecterDR 1x/4x 瞄准镜及其 FDE 变体。

## 原理

往基座 item 的 `mod_scope` 槽 `SlotFilter.Filter`（`HashSet<MongoId>`）追加两个瞄具 id。幂等：已存在则不重复添加。

目标物品 id：

| 物品 | id |
| --- | --- |
| UZI StormWerkz 顶盖导轨（基座） | `6698c90829e062525d0ad8ad` |
| ELCAN SpecterDR 1x/4x | `57ac965c24597706be5f975c` |
| ELCAN SpecterDR 1x/4x FDE | `57aca93d2459771f2c7e26db` |

## 构建

需要本机 `dotnet`（net9.0 目标）。引用集 `ref/` 里的 SPT 程序集需从服务端拷贝（已被 `.gitignore` 排除，需自行准备）：

```bash
# 从服务端拷贝引用集到 ref/（服务端通过 k9-eth.sh 访问）
mkdir -p ref && k9-eth.sh 'cd ~/.local/bin/aki/SPT && tar cf - SPTarkov.Server.Core.dll SPTarkov.DI.dll SPTarkov.Common.dll SemanticVersioning.dll SPT.Server.dll' | tar xf - -C ref/

dotnet build -c Release
```

产物：`bin/Release/UziStormwerkzElcanScope.dll`。

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
```
