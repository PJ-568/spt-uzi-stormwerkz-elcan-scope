using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace Xidong.UZI.ELCAN.Tests;

/// <summary>
/// 针对 <see cref="UziStormwerkzElcanScopePlugin"/> 的单元测试：
/// 用最小的物品/槽位夹具直接驱动 <c>OnLoadAsync</c>，断言目标槽 Filter 的注入与幂等。
/// 物品 id 与 src/Mod.cs 中的常量保持一致；若上游数据 id 变更，这里会先失败。
/// </summary>
public class UziStormwerkzElcanScopePluginTests
{
    private const string Ppsh41Id = "5ea03f7400685063ec28bfa8";
    private const string Ppsh41BarrelId = "5ea02bb600685063ec28bfa1";
    private const string StormwerkzRailId = "6698c90829e062525d0ad8ad";
    private const string SpecterDrId = "57ac965c24597706be5f975c";
    private const string SpecterDrFdeId = "57aca93d2459771f2c7e26db";
    private const string Bravo4Id = "57adff4f24597737f373b6e6";
    private const string Cr200DsId = "624c2e8614da335f1e034d8c";
    private const string Mp18Id = "61f804acfcba9556ea304cb8";

    private static readonly string[] MosinBarrelIds =
    [
        "5bfd4cc90db834001d23e846",
        "5bfd4cd60db834001c38f095",
        "5bfd4cbe0db834001b73449f",
        "5ae09bff5acfc4001562219d",
    ];

    private const string Ppsh41StockId = "5ea03e9400685063ec28bfa4";

    private static readonly string[] Ppsh41StockCompatIds =
    [
        "6259c3387d6aab70bc23a18d",
        "646371a9f2404ab67905c8e6",
        "6492d7847363b8a52206bc52",
        "6492e3a97df7d749100e29ee",
        "606eef46232e5a31c233d500",
        "5e848dc4e4dbc5266a4ec63d",
    ];

    private const string Ppsh41DustCoverId = "5ea03e5009aa976f2e7a514b";
    private const string HuxwrxHxQdId = "6a158e4abf497aade10030e0";
    private const string HuxwrxHxQdTanId = "6a1eb32c6cd328ea90037455";

    private const string AimSportsTriRailId = "5bbdb811d4351e45020113c7";
    private const string StormwerkzLowerHandguardRailId = "66992f7d9950f5f4cd0602a8";

    // 与 src/Mod.cs 的 ZenitRkForegripIds 对应。
    private static readonly string[] ZenitRkForegripIds =
    [
        "5c1bc4812e22164bef5cfde7",
        "5c1bc5612e221602b5429350",
        "5c1bc5af2e221602b412949b",
        "5c1bc5fb2e221602b1779b32",
        "5c1bc7432e221602b412949d",
        "5c1bc7752e221602b1779b34",
    ];

    // 与 src/Mod.cs 的 AimSportsTriRailForegripIds 对应（不含 MP9 与 Steyr AUG）。
    private static readonly string[] AimSportsTriRailForegripIds =
    [
        "5c1bc4812e22164bef5cfde7",
        "5c1bc5612e221602b5429350",
        "5c1bc5af2e221602b412949b",
        "5c1bc5fb2e221602b1779b32",
        "5c1bc7432e221602b412949d",
        "5c1bc7752e221602b1779b34",
        "5f6340d3ca442212f4047eb2",
        "5c7fc87d2e221644f31c0298",
        "5c87ca002e221600114cb150",
        "5cda9bcfd7f00c0c0b53e900",
        "5fc0f9b5d724d907e2077d82",
        "5fc0f9cbd6fa9c00c571bb90",
        "615d8fd3290d254f5e6b2edc",
        "651a8bf3a8520e48047bf708",
        "651a8e529829226ceb67c319",
        "665d5d9e338229cfd6078da1",
        "665edce564fb556f940ab32a",
        "558032614bdc2de7118b4585",
        "58c157be86f77403c74b2bb6",
        "58c157c886f774032749fb06",
    ];

    private static UziStormwerkzElcanScopePlugin BuildPlugin(Dictionary<MongoId, TemplateItem> items)
    {
        var logger = new Mock<ISptLogger<UziStormwerkzElcanScopePlugin>>();
        return new UziStormwerkzElcanScopePlugin(logger.Object, CreateTemplateTable(items));
    }

    // TemplateTable（NuGet 4.1.2）带多个 required 成员，测试只关心 Items；
    // 用反射实例化以绕过编译期 required 校验。
    private static TemplateTable CreateTemplateTable(Dictionary<MongoId, TemplateItem> items)
    {
        var table = (TemplateTable)Activator.CreateInstance(typeof(TemplateTable))!;
        typeof(TemplateTable).GetProperty(nameof(TemplateTable.Items))!.SetValue(table, items);
        return table;
    }

    private static TemplateItem ItemWithSlot(string name, string slotName, params string[] initialFilter)
    {
        var filter = new SlotFilter
        {
            Filter = new HashSet<MongoId>(initialFilter.Select(id => new MongoId(id))),
        };
        var slot = new Slot
        {
            Name = slotName,
            Properties = new SlotProperties { Filters = [filter] },
        };
        return new TemplateItem
        {
            Name = name,
            Properties = new TemplateItemProperties { Slots = [slot] },
        };
    }

    private static TemplateItem ItemWithConflicts(string name) => new()
    {
        Name = name,
        Properties = new TemplateItemProperties { ConflictingItems = [] },
    };

    private static HashSet<MongoId> FilterOf(TemplateItem item, string slotName) =>
        item.Properties!.Slots!.Single(s => s.Name == slotName).Properties!.Filters!.Single().Filter!;

    [Fact]
    public async Task AddsAllMosinBarrelsToPpsh41BarrelSlot()
    {
        var ppsh = ItemWithSlot("weapon_zis_ppsh41_762x25", "mod_barrel", Ppsh41BarrelId);
        var items = new Dictionary<MongoId, TemplateItem> { [new MongoId(Ppsh41Id)] = ppsh };

        await BuildPlugin(items).OnLoadAsync(CancellationToken.None);

        var filter = FilterOf(ppsh, "mod_barrel");
        Assert.Contains(new MongoId(Ppsh41BarrelId), filter);
        foreach (var id in MosinBarrelIds)
        {
            Assert.Contains(new MongoId(id), filter);
        }

        Assert.Equal(1 + MosinBarrelIds.Length, filter.Count);
    }

    [Fact]
    public async Task IsIdempotentWhenLoadedTwice()
    {
        var ppsh = ItemWithSlot("weapon_zis_ppsh41_762x25", "mod_barrel", Ppsh41BarrelId);
        var items = new Dictionary<MongoId, TemplateItem> { [new MongoId(Ppsh41Id)] = ppsh };
        var plugin = BuildPlugin(items);

        await plugin.OnLoadAsync(CancellationToken.None);
        await plugin.OnLoadAsync(CancellationToken.None);

        Assert.Equal(1 + MosinBarrelIds.Length, FilterOf(ppsh, "mod_barrel").Count);
    }

    [Fact]
    public async Task AddsScopesToStormwerkzMountScopeSlot()
    {
        var mount = ItemWithSlot("mount_uzi_stormwerkz_top_cover_rail", "mod_scope");
        var items = new Dictionary<MongoId, TemplateItem> { [new MongoId(StormwerkzRailId)] = mount };

        await BuildPlugin(items).OnLoadAsync(CancellationToken.None);

        var filter = FilterOf(mount, "mod_scope");
        Assert.Contains(new MongoId(SpecterDrId), filter);
        Assert.Contains(new MongoId(SpecterDrFdeId), filter);
        Assert.Contains(new MongoId(Bravo4Id), filter);
        Assert.Equal(3, filter.Count);
    }

    [Fact]
    public async Task AddsMountsToCr200DsSightSlot()
    {
        var revolver = ItemWithSlot("weapon_chiappa_rhino_200ds_9x19", "mod_sight_front");
        var items = new Dictionary<MongoId, TemplateItem> { [new MongoId(Cr200DsId)] = revolver };

        await BuildPlugin(items).OnLoadAsync(CancellationToken.None);

        var filter = FilterOf(revolver, "mod_sight_front");
        Assert.Contains(new MongoId(StormwerkzRailId), filter);
        Assert.Contains(new MongoId(Mp18Id), filter);
        Assert.Equal(2, filter.Count);
    }

    [Fact]
    public async Task AddsStocksToPpsh41StockSlot()
    {
        var ppsh = ItemWithSlot("weapon_zis_ppsh41_762x25", "mod_stock", Ppsh41StockId);
        var items = new Dictionary<MongoId, TemplateItem> { [new MongoId(Ppsh41Id)] = ppsh };

        await BuildPlugin(items).OnLoadAsync(CancellationToken.None);

        var filter = FilterOf(ppsh, "mod_stock");
        Assert.Contains(new MongoId(Ppsh41StockId), filter);
        foreach (var id in Ppsh41StockCompatIds)
        {
            Assert.Contains(new MongoId(id), filter);
        }

        Assert.Equal(1 + Ppsh41StockCompatIds.Length, filter.Count);
    }

    [Fact]
    public async Task MakesPpsh41DustCoverConflictWithHuxwrxSuppressors()
    {
        var dustCover = ItemWithConflicts("PPSH-41 dust cover");
        var black = ItemWithConflicts("HUXWRX HX-QD 7.62x51 sound suppressor");
        var tan = ItemWithConflicts("HUXWRX HX-QD 7.62x51 sound suppressor (Tan)");
        var items = new Dictionary<MongoId, TemplateItem>
        {
            [new MongoId(Ppsh41DustCoverId)] = dustCover,
            [new MongoId(HuxwrxHxQdId)] = black,
            [new MongoId(HuxwrxHxQdTanId)] = tan,
        };

        await BuildPlugin(items).OnLoadAsync(CancellationToken.None);

        Assert.Contains(new MongoId(HuxwrxHxQdId), dustCover.Properties!.ConflictingItems!);
        Assert.Contains(new MongoId(HuxwrxHxQdTanId), dustCover.Properties!.ConflictingItems!);
        Assert.Contains(new MongoId(Ppsh41DustCoverId), black.Properties!.ConflictingItems!);
        Assert.Contains(new MongoId(Ppsh41DustCoverId), tan.Properties!.ConflictingItems!);
    }

    [Fact]
    public async Task AddsForegripsToAimSportsTriRailFirstTacticalSlot()
    {
        var rail = ItemWithSlot("mount_mosin_aim_sports_tri_rail", "mod_tactical_000");
        var items = new Dictionary<MongoId, TemplateItem> { [new MongoId(AimSportsTriRailId)] = rail };

        await BuildPlugin(items).OnLoadAsync(CancellationToken.None);

        var filter = FilterOf(rail, "mod_tactical_000");
        Assert.Equal(AimSportsTriRailForegripIds.Length, filter.Count);
        foreach (var id in AimSportsTriRailForegripIds)
        {
            Assert.Contains(new MongoId(id), filter);
        }
    }

    [Fact]
    public async Task AddsZenitRkForegripsToStormwerkzLowerHandguardRail()
    {
        var rail = ItemWithSlot("handguard_uzi_stormwerkz_lower_rail", "mod_tactical");
        var items = new Dictionary<MongoId, TemplateItem> { [new MongoId(StormwerkzLowerHandguardRailId)] = rail };

        await BuildPlugin(items).OnLoadAsync(CancellationToken.None);

        var filter = FilterOf(rail, "mod_tactical");
        Assert.Equal(ZenitRkForegripIds.Length, filter.Count);
        foreach (var id in ZenitRkForegripIds)
        {
            Assert.Contains(new MongoId(id), filter);
        }
    }

    [Fact]
    public async Task MissingItemsDoNotThrow()
    {
        await BuildPlugin(new Dictionary<MongoId, TemplateItem>()).OnLoadAsync(CancellationToken.None);
    }
}
