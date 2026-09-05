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
/// SPT 4.1.x 服务端 mod：
///   1. 让 UZI StormWerkz 瞄具基座的 mod_scope 槽支持安装 ELCAN SpecterDR 1x/4x 及其 FDE 变体；
///   2. 让 StormWerkz 瞄具基座也能安装到 CR 200DS 转轮手枪的准星槽（mod_sight_*）。
///
/// 做法：往目标槽的 SlotFilter.Filter（HashSet&lt;MongoId&gt;）追加物品 id（幂等）。
/// SPT 4.1.x 的 IOnLoad 改为 OnLoadAsync(CancellationToken)；
/// DatabaseService.GetItems() 已删除，改为直接注入 TemplateTable Singleton
/// （SPT 启动器在 host build 前 AddSingleton(databaseTables.Templates)）。
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
    // 警告：mod_sight_front 是「前准星」槽（默认装 Chiappa Rhino 专用前准星），
    // 把 UZI 顶盖导轨（接收器挂件）挂到这里只是把 id 加进 Filter 白名单，
    // 客户端能让玩家在 UI 里装上，但游戏里没有适配的 3D 装配模型/动画。
    // 详见 README「已知问题」。
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
    /// 把 StormWerkz 顶盖导轨（接收器挂件）的 id 加进 CR 200DS 前准星槽的 Filter 白名单。
    /// 警告：mod_sight_front 是前准星槽，EFT 数据上与 UZI 顶盖不兼容，详见 README。
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
