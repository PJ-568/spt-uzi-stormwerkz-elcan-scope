using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Common.Models.Logging;

namespace Xidong.UZI.ELCAN;

/// <summary>
/// 服务端 mod：
///   1. 让 UZI StormWerkz 瞄具基座的 mod_scope 槽支持安装 ELCAN SpecterDR 1x/4x 及其 FDE 变体；
///   2. 让 StormWerkz 顶盖导轨可安装到 CR 200DS 转轮手枪的前准星槽。
///
/// 做法：往目标槽的 SlotFilter.Filter（HashSet&lt;MongoId&gt;）追加物品 id（幂等）。
/// </summary>
[Injectable(InjectionType.Singleton, OnLoadOrder.Preload + 4)]
public class UziStormwerkzElcanScopePlugin(
    ISptLogger<UziStormwerkzElcanScopePlugin> logger,
    TemplateTable templateTable) : IOnLoad
{
    private const string ScopeSlotName = "mod_scope";

    // UZI StormWerkz 瞄具基座（顶盖导轨）
    private const string StormwerkzTopCoverRailId = "6698c90829e062525d0ad8ad";

    // ELCAN SpecterDR 1x/4x（黑）与 FDE 变体
    private const string SpecterDrId = "57ac965c24597706be5f975c";
    private const string SpecterDrFdeId = "57aca93d2459771f2c7e26db";

    // CR 200DS 转轮手枪（Chiappa Rhino 200DS 9x19 revolver）—— 前准星槽。
    private const string Cr200DsId = "624c2e8614da335f1e034d8c";
    private const string Cr200DsSightSlotName = "mod_sight_front";

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            Dictionary<MongoId, TemplateItem> items = templateTable.Items;

            AddScopesToStormwerkzMount(items);
            AddStormwerkzMountToCr200DsSight(items);
        }
        catch (Exception ex)
        {
            logger.Error("UziStormwerkzElcanScope failed: " + ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 往 UZI StormWerkz 顶盖导轨的 mod_scope 槽追加 SpecterDR 瞄具 id。
    /// </summary>
    private void AddScopesToStormwerkzMount(Dictionary<MongoId, TemplateItem> items)
    {
        if (!items.TryGetValue(StormwerkzTopCoverRailId, out TemplateItem? mount))
        {
            logger.Warning($"UziStormwerkzElcanScope: mount id '{StormwerkzTopCoverRailId}' not found in Items");
            return;
        }

        IEnumerable<Slot>? slots = mount.Properties?.Slots;
        if (slots is null)
        {
            logger.Warning("UziStormwerkzElcanScope: mount has no slots");
            return;
        }

        foreach (Slot slot in slots.Where(s => s is not null && string.Equals(s.Name, ScopeSlotName, StringComparison.OrdinalIgnoreCase)))
        {
            IEnumerable<SlotFilter>? filters = slot.Properties?.Filters;
            if (filters is null)
            {
                continue;
            }

            foreach (SlotFilter filter in filters)
            {
                AddToFilter(filter, SpecterDrId, mount, slot);
                AddToFilter(filter, SpecterDrFdeId, mount, slot);
            }
        }
    }

    /// <summary>
    /// 把 StormWerkz 顶盖导轨的 id 加进 CR 200DS 前准星槽的 Filter 白名单。
    /// </summary>
    private void AddStormwerkzMountToCr200DsSight(Dictionary<MongoId, TemplateItem> items)
    {
        if (!items.TryGetValue(Cr200DsId, out TemplateItem? revolver))
        {
            logger.Warning($"UziStormwerkzElcanScope: CR 200DS id '{Cr200DsId}' not found in Items");
            return;
        }

        IEnumerable<Slot>? slots = revolver.Properties?.Slots;
        if (slots is null)
        {
            logger.Warning("UziStormwerkzElcanScope: CR 200DS has no slots");
            return;
        }

        foreach (Slot slot in slots.Where(s => s is not null && string.Equals(s.Name, Cr200DsSightSlotName, StringComparison.OrdinalIgnoreCase)))
        {
            IEnumerable<SlotFilter>? filters = slot.Properties?.Filters;
            if (filters is null)
            {
                continue;
            }

            foreach (SlotFilter filter in filters)
            {
                AddToFilter(filter, StormwerkzTopCoverRailId, revolver, slot);
            }
        }
    }

    private void AddToFilter(SlotFilter? filter, string itemId, TemplateItem parent, Slot slot)
    {
        if (filter is null)
        {
            return;
        }

        filter.Filter ??= new HashSet<MongoId>();

        if (filter.Filter.Add(itemId))
        {
            logger.Info($"UziStormwerkzElcanScope: added '{itemId}' to slot '{slot.Name}' ({parent.Name})");
        }
        else
        {
            logger.Debug($"UziStormwerkzElcanScope: '{itemId}' already in slot '{slot.Name}' ({parent.Name})");
        }
    }
}
