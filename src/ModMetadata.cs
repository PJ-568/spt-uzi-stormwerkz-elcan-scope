using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Spt.Mod;
using Ver = SemanticVersioning.Version;
using Rng = SemanticVersioning.Range;

namespace Xidong.UZI.ELCAN;

/// <summary>
/// 元数据：SPT 4.1.x 借此识别本 mod（版本取自程序集版本号）。
/// 4.1.x 把旧的 AbstractModMetadata 抽象类替换为 IModMetadata 接口，
/// 并删除了 IsBundleMod（bundles 通过 bundles.json 自动加载），
/// 新增 HasPrepatcher 字段；License 收紧为非空。
/// </summary>
public class UziStormwerkzElcanScopeMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.xidong.uzi.elcan.scope";

    public string Name { get; init; } = "UziStormwerkzElcanScope";

    public string Author { get; init; } = "xidong";

    public List<string>? Contributors { get; init; }

    public Ver Version { get; init; } =
        new(typeof(UziStormwerkzElcanScopeMetadata).Assembly.GetName().Version?.ToString(3) ?? "2.0.0");

    public Rng SptVersion { get; init; } = new("~4.1.0");

    public bool HasPrepatcher { get; init; } = false;

    public List<string>? Incompatibilities { get; init; }

    public Dictionary<string, Rng>? ModDependencies { get; init; }

    public string? Url { get; init; }

    public string License { get; init; } = "MIT";
}
