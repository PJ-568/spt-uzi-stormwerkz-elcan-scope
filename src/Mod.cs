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
///   1. 让 UZI StormWerkz 瞄具基座的 mod_scope 槽支持安装 ELCAN SpecterDR 1x/4x 及其 FDE 变体，
///      以及 SIG Sauer BRAVO4 4x30 瞄准镜；
///   2. 让 StormWerkz 顶盖导轨可安装到 CR 200DS 转轮手枪的前准星槽；
///   3. 让 CR 200DS 的前准星槽支持安装 MP-18 瞄具基座；
///   4. 让 PPSh-41 冲锋枪的枪管槽支持安装莫辛纳甘的全部 4 种尺寸枪管。
///
/// 做法：往目标槽的 SlotFilter.Filter（HashSet&lt;MongoId&gt;）追加物品 id（幂等）。
/// </summary>
[Injectable(InjectionType.Singleton, OnLoadOrder.Preload + 4)]
public class UziStormwerkzElcanScopePlugin(
    ISptLogger<UziStormwerkzElcanScopePlugin> logger,
    TemplateTable templateTable) : IOnLoad
{
    private const string ScopeSlotName = "mod_scope";
    private const string SightSlotName = "mod_sight_front";
    private const string BarrelSlotName = "mod_barrel";

    // UZI StormWerkz 瞄具基座（顶盖导轨）
    private const string StormwerkzTopCoverRailId = "6698c90829e062525d0ad8ad";

    // ELCAN SpecterDR 1x/4x（黑）与 FDE 变体
    private const string SpecterDrId = "57ac965c24597706be5f975c";
    private const string SpecterDrFdeId = "57aca93d2459771f2c7e26db";

    // SIG Sauer BRAVO4 4x30 瞄准镜
    private const string Bravo4Id = "57adff4f24597737f373b6e6";

    // CR 200DS 转轮手枪（Chiappa Rhino 200DS 9x19 revolver）—— 前准星槽。
    private const string Cr200DsId = "624c2e8614da335f1e034d8c";

    // MP-18 瞄具基座
    private const string Mp18ScopeBaseId = "61f804acfcba9556ea304cb8";

    // PPSh-41 冲锋枪 —— 枪管槽。
    private const string Ppsh41Id = "5ea03f7400685063ec28bfa8";

    // 莫辛纳甘枪管（4 种尺寸）
    private const string MosinBarrel200Id = "5bfd4cc90db834001d23e846"; // 200mm 锯短
    private const string MosinBarrel220ThreadedId = "5bfd4cd60db834001c38f095"; // 220mm 锯短螺纹
    private const string MosinBarrel514Id = "5bfd4cbe0db834001b73449f"; // 514mm 卡宾
    private const string MosinBarrel730Id = "5ae09bff5acfc4001562219d"; // 730mm 标准

    // 日志中用于标识槽位承载者的短名。
    private const string MountLabel = "mount";
    private const string Cr200DsLabel = "CR 200DS";
    private const string Ppsh41Label = "PPSh-41";

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            Dictionary<MongoId, TemplateItem> items = templateTable.Items;

            // UZI StormWerkz 顶盖导轨的 mod_scope 槽：追加 SpecterDR 与 BRAVO4 瞄具。
            AddItemIdsToSlot(items, StormwerkzTopCoverRailId, MountLabel, ScopeSlotName, SpecterDrId, SpecterDrFdeId, Bravo4Id);

            // CR 200DS 前准星槽：追加 StormWerkz 顶盖导轨与 MP-18 瞄具基座。
            AddItemIdsToSlot(items, Cr200DsId, Cr200DsLabel, SightSlotName, StormwerkzTopCoverRailId, Mp18ScopeBaseId);

            // PPSh-41 枪管槽：追加莫辛纳甘的全部尺寸枪管。
            AddItemIdsToSlot(items, Ppsh41Id, Ppsh41Label, BarrelSlotName, MosinBarrel200Id, MosinBarrel220ThreadedId, MosinBarrel514Id, MosinBarrel730Id);
        }
        catch (Exception ex)
        {
            logger.Error("UziStormwerkzElcanScope failed: " + ex.Message);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 往 <paramref name="ownerId"/> 名为 <paramref name="slotName"/> 的槽的各 Filter 追加 <paramref name="itemIds"/>（幂等）。
    /// </summary>
    private void AddItemIdsToSlot(
        Dictionary<MongoId, TemplateItem> items,
        string ownerId,
        string ownerLabel,
        string slotName,
        params string[] itemIds)
    {
        if (!items.TryGetValue(ownerId, out TemplateItem? owner))
        {
            logger.Warning($"UziStormwerkzElcanScope: {ownerLabel} id '{ownerId}' not found in Items");
            return;
        }

        IEnumerable<Slot>? slots = owner.Properties?.Slots;
        if (slots is null)
        {
            logger.Warning($"UziStormwerkzElcanScope: {ownerLabel} has no slots");
            return;
        }

        foreach (Slot slot in slots.Where(s => s is not null && string.Equals(s.Name, slotName, StringComparison.OrdinalIgnoreCase)))
        {
            IEnumerable<SlotFilter>? filters = slot.Properties?.Filters;
            if (filters is null)
            {
                continue;
            }

            foreach (SlotFilter filter in filters)
            {
                foreach (string itemId in itemIds)
                {
                    AddToFilter(filter, itemId, owner, slot);
                }
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
