using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Spt.Mod;
using Ver = SemanticVersioning.Version;
using Rng = SemanticVersioning.Range;

namespace Xidong.UZI.ELCAN;

/// <summary>
/// 元数据：SPT 4.x 借此识别本 mod（版本取自程序集版本号，SptVersion 与目标服务端匹配）。
/// </summary>
public record UziStormwerkzElcanScopeMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; }

    public override string Name { get; init; }

    public override string Author { get; init; }

    public override List<string>? Contributors { get; init; }

    public override Ver Version { get; init; }

    public override Rng SptVersion { get; init; }

    public override List<string>? Incompatibilities { get; init; }

    public override Dictionary<string, Rng>? ModDependencies { get; init; }

    public override string? Url { get; init; }

    public override bool? IsBundleMod { get; init; }

    public override string License { get; init; }

    public UziStormwerkzElcanScopeMetadata()
    {
        ModGuid = "com.xidong.uzi.elcan.scope";
        Name = "UziStormwerkzElcanScope";
        Author = "xidong";
        Version = new Ver(typeof(UziStormwerkzElcanScopeMetadata).Assembly.GetName().Version?.ToString(3), false);
        SptVersion = new Rng("~4.0.13", false);
        License = "MIT";
    }
}
