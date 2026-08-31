using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace Xidong.UZI.ELCAN;

/// <summary>
/// 让 UZI StormWerkz 瞄具基座的 mod_scope 槽支持安装 ELCAN SpecterDR 1x/4x 及其 FDE 变体。
/// 做法：往基座 mod_scope 槽的 SlotFilter.Filter 集合追加两个瞄具 id（幂等，已存在则不重复）。
/// </summary>
[Injectable(InjectionType.Singleton, null, int.MaxValue, TypePriority = 400001)]
public class UziStormwerkzElcanScopePlugin(
    ISptLogger<UziStormwerkzElcanScopePlugin> logger,
    DatabaseService databaseService) : IOnLoad
{
    private const string ScopeSlotName = "mod_scope";

    // UZI StormWerkz 瞄具基座（顶盖导轨）
    private const string StormwerkzTopCoverRailId = "6698c90829e062525d0ad8ad";

    // ELCAN SpecterDR 1x/4x（黑）与 FDE 变体
    private const string SpecterDrId = "57ac965c24597706be5f975c";
    private const string SpecterDrFdeId = "57aca93d2459771f2c7e26db";

    public Task OnLoad()
    {
        try
        {
            AddScopesToStormwerkzMount();
            logger.Info("UziStormwerkzElcanScope: ELCAN SpecterDR scopes added to UZI StormWerkz mount");
        }
        catch (Exception ex)
        {
            logger.Error("UziStormwerkzElcanScope failed: " + ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 往基座 mod_scope 槽追加 SpecterDR 瞄具 id。
    /// 扩展了所有含有 mod_scope 槽位的 StormWerkz 瞄具基座，持续兼容未来新增基座。
    /// </summary>
    private void AddScopesToStormwerkzMount()
    {
        Dictionary<MongoId, TemplateItem> items = databaseService.GetItems();

        // 将 id 字符串映射为 item 集合（通过 id 定位基座）。
        // 若已存在则跳过，幂等。
        if (!items.TryGetValue(StormwerkzTopCoverRailId, out TemplateItem? mount))
        {
            logger.Warning($"UziStormwerkzElcanScope: mount id '{StormwerkzTopCoverRailId}' not found in Items");
            return;
        }

        IEnumerable<Slot>? slots = mount.Properties?.Slots;
        if (slots == null)
        {
            logger.Warning("UziStormwerkzElcanScope: mount has no slots");
            return;
        }

        foreach (Slot? slot in slots.Where(s => s != null && string.Equals(s.Name, ScopeSlotName, StringComparison.OrdinalIgnoreCase)))
        {
            IEnumerable<SlotFilter>? filters = slot.Properties?.Filters;
            if (filters == null)
            {
                continue;
            }

            foreach (SlotFilter? filter in filters)
            {
                AddToFilter(filter, SpecterDrId, mount, slot);
                AddToFilter(filter, SpecterDrFdeId, mount, slot);
            }
        }
    }

    private void AddToFilter(SlotFilter? filter, string itemId, TemplateItem mount, Slot? slot)
    {
        if (filter == null)
        {
            return;
        }

        if (filter.Filter == null)
        {
            filter.Filter = new HashSet<MongoId>();
        }

        if (filter.Filter.Add(itemId))
        {
            logger.Info($"UziStormwerkzElcanScope: added '{itemId}' to slot '{slot?.Name}' ({mount.Name})");
        }
        else
        {
            logger.Debug($"UziStormwerkzElcanScope: '{itemId}' already in slot '{slot?.Name}' ({mount.Name})");
        }
    }
}
