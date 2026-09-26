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
///   4. 让 PPSh-41 冲锋枪的枪管槽支持安装莫辛纳甘的全部 4 种尺寸枪管；
///   5. 让 PPSh-41 冲锋枪的枪托槽支持安装 Benelli M3 可伸缩枪托、PKM / PKP 枪托、Ultima MP-155 塑料手枪式握把与 KS-23 金属枪托；
///   6. 让 HUXWRX HX-QD 消音器（含黄褐色变体）与 PPSh-41 防尘盖互不兼容；
///   7. 让 Aim Sports“三轨”莫辛步枪导轨的第一个战术配件槽（mod_tactical_000）支持安装多种前握把；
///   8. 让 UZI StormWerkz 护木底轨的 mod_tactical 槽支持安装 Zenit RK 系列前握把。
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
    private const string StockSlotName = "mod_stock";
    private const string TacticalSlotName = "mod_tactical";

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

    // PPSh-41 冲锋枪（承载枪管槽与枪托槽）。
    private const string Ppsh41Id = "5ea03f7400685063ec28bfa8";

    // 莫辛纳甘枪管（4 种尺寸）
    private const string MosinBarrel200Id = "5bfd4cc90db834001d23e846"; // 200mm 锯短
    private const string MosinBarrel220ThreadedId = "5bfd4cd60db834001c38f095"; // 220mm 锯短螺纹
    private const string MosinBarrel514Id = "5bfd4cbe0db834001b73449f"; // 514mm 卡宾
    private const string MosinBarrel730Id = "5ae09bff5acfc4001562219d"; // 730mm 标准

    // PPSh-41 枪托槽兼容的枪托 / 握把。
    private const string BenelliM3TelescopicStockId = "6259c3387d6aab70bc23a18d"; // Benelli M3 可伸缩枪托
    private const string PkmWoodenStockId = "646371a9f2404ab67905c8e6"; // PKM 木制枪托
    private const string PkZenitPt2StockId = "6492d7847363b8a52206bc52"; // Zenit PT-2 "Klassika" PK 机枪枪托
    private const string PkpPolymerStockId = "6492e3a97df7d749100e29ee"; // PKP 聚合物枪托
    private const string UltimaMp155PistolGripId = "606eef46232e5a31c233d500"; // Ultima MP-155 塑料手枪式握把
    private const string Ks23MetalStockId = "5e848dc4e4dbc5266a4ec63d"; // KS-23 金属枪托（原版仅可装于 KS-23M 聚合物手枪式握把的 mod_stock 槽）

    // PPSh-41 防尘盖（mod_reciever 槽承载物品）。
    private const string Ppsh41DustCoverId = "5ea03e5009aa976f2e7a514b";

    // HUXWRX HX-QD 7.62x51 消音器（黑 / 黄褐色变体）—— 由 WTT-ContentBackport 注入。
    private const string HuxwrxHxQdId = "6a158e4abf497aade10030e0";
    private const string HuxwrxHxQdTanId = "6a1eb32c6cd328ea90037455";

    // Aim Sports“三轨”莫辛步枪导轨；只改第一个战术配件槽（mod_tactical_000）。
    private const string AimSportsTriRailId = "5bbdb811d4351e45020113c7";
    private const string AimSportsTriRailTacticalSlotName = "mod_tactical_000";
    private const string AimSportsTriRailLabel = "Aim Sports tri-rail";

    // UZI StormWerkz 护木底轨（mod_tactical 槽）。
    private const string StormwerkzLowerHandguardRailId = "66992f7d9950f5f4cd0602a8";
    private const string StormwerkzLowerHandguardLabel = "UZI StormWerkz lower handguard rail";

    // Zenit RK 系列前握把。
    private static readonly string[] ZenitRkForegripIds =
    [
        "5c1bc4812e22164bef5cfde7", // RK-0
        "5c1bc5612e221602b5429350", // RK-1
        "5c1bc5af2e221602b412949b", // RK-2
        "5c1bc5fb2e221602b1779b32", // RK-4
        "5c1bc7432e221602b412949d", // RK-5
        "5c1bc7752e221602b1779b34", // RK-6
    ];

    // Aim Sports“三轨”第一个战术配件槽兼容的前握把（RK 系列见上）。
    private static readonly string[] AimSportsTriRailForegripIds =
    [
        .. ZenitRkForegripIds,
        // 镂空前握把
        "5f6340d3ca442212f4047eb2", // Tactical Dynamics 镂空前握把
        // 垂直前握把
        "5c7fc87d2e221644f31c0298", // BCM GUNFIGHTER MOD 3 vertical
        "5c87ca002e221600114cb150", // KAC vertical
        "5cda9bcfd7f00c0c0b53e900", // ASh-12 vertical
        "5fc0f9b5d724d907e2077d82", // A3 Tactical MVF001
        "5fc0f9cbd6fa9c00c571bb90", // SIG Sauer vertical
        "615d8fd3290d254f5e6b2edc", // Monstrum Tactical
        "651a8bf3a8520e48047bf708", // Daniel Defense Enhanced M-LOK (Black)
        "651a8e529829226ceb67c319", // Daniel Defense Enhanced M-LOK (Coyote Brown)
        // BCM GUNFIGHTER MOD 3 M-LOK
        "665d5d9e338229cfd6078da1", // (Black)
        "665edce564fb556f940ab32a", // (FDE)
        // TangoDown Stubby BGV-MK46K
        "558032614bdc2de7118b4585", // (Black)
        "58c157be86f77403c74b2bb6", // (FDE)
        "58c157c886f774032749fb06", // (Stealth Grey)
    ];

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

            // PPSh-41 枪托槽：追加 Benelli M3 可伸缩枪托、PKM / PKP 枪托、Ultima MP-155 握把与 KS-23 金属枪托。
            AddItemIdsToSlot(items, Ppsh41Id, Ppsh41Label, StockSlotName, BenelliM3TelescopicStockId, PkmWoodenStockId, PkZenitPt2StockId, PkpPolymerStockId, UltimaMp155PistolGripId, Ks23MetalStockId);

            // PPSh-41 防尘盖与 HUXWRX HX-QD 消音器互不兼容。
            AddDustCoverSuppressorConflict(items);

            // Aim Sports“三轨”的第一个战术配件槽：追加多种前握把。
            AddItemIdsToSlot(items, AimSportsTriRailId, AimSportsTriRailLabel, AimSportsTriRailTacticalSlotName, AimSportsTriRailForegripIds);

            // UZI StormWerkz 护木底轨的 mod_tactical 槽：追加 Zenit RK 系列前握把。
            AddItemIdsToSlot(items, StormwerkzLowerHandguardRailId, StormwerkzLowerHandguardLabel, TacticalSlotName, ZenitRkForegripIds);
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

    /// <summary>
    /// 让 PPSh-41 防尘盖与 HUXWRX HX-QD 消音器（含黄褐色变体）互不兼容（幂等）。
    /// 防尘盖为原版物品、必定存在；消音器由 WTT-ContentBackport 注入，若不在 Items 中则跳过
    /// （防尘盖侧足以建立冲突，且避开了第三方 mod 的加载时序依赖）。
    /// </summary>
    private void AddDustCoverSuppressorConflict(Dictionary<MongoId, TemplateItem> items)
    {
        AddConflictingItems(items, Ppsh41DustCoverId, "PPSh-41 dust cover", HuxwrxHxQdId, HuxwrxHxQdTanId);
        AddConflictingItems(items, HuxwrxHxQdId, "HUXWRX HX-QD", Ppsh41DustCoverId);
        AddConflictingItems(items, HuxwrxHxQdTanId, "HUXWRX HX-QD (Tan)", Ppsh41DustCoverId);
    }

    /// <summary>
    /// 往 <paramref name="ownerId"/> 的 ConflictingItems 追加 <paramref name="conflictIds"/>（幂等）。
    /// </summary>
    private void AddConflictingItems(
        Dictionary<MongoId, TemplateItem> items,
        string ownerId,
        string ownerLabel,
        params string[] conflictIds)
    {
        if (!items.TryGetValue(ownerId, out TemplateItem? owner))
        {
            logger.Debug($"UziStormwerkzElcanScope: {ownerLabel} id '{ownerId}' not in Items, skip conflicting items");
            return;
        }

        owner.Properties ??= new TemplateItemProperties();
        owner.Properties.ConflictingItems ??= new HashSet<MongoId>();

        foreach (string conflictId in conflictIds)
        {
            if (owner.Properties.ConflictingItems.Add(conflictId))
            {
                logger.Info($"UziStormwerkzElcanScope: added conflicting item '{conflictId}' to '{ownerLabel}' ({owner.Name})");
            }
            else
            {
                logger.Debug($"UziStormwerkzElcanScope: conflicting item '{conflictId}' already on '{ownerLabel}' ({owner.Name})");
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
