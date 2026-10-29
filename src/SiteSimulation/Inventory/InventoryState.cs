using System.Text.Json;

namespace SiteSimulation;

/// <summary>One physical item graph shared by personal, nested and nearby fixture views.</summary>
public sealed class InventoryState
{
    public const string FieldProfile = "field-kit-v1";
    public bool Enabled => Content is not null;
    public InventoryContent? Content { get; }
    public BodyFitCatalog? BodyFits { get; }
    internal string ContentJson { get; } = "";
    internal Dictionary<string, InventoryItem> Items { get; } = new(StringComparer.Ordinal);
    internal List<RegionState> Regions { get; } = [];
    public long Revision { get; internal set; }
    public InventoryWork? Pending { get; internal set; }
    internal List<InventorySecureStep> SecureQueue { get; } = [];
    public int SecuringRemaining => SecureQueue.Count;
    public IReadOnlyList<InventoryItem> DiagnosticItems => Items.Values.OrderBy(i => i.Id, StringComparer.Ordinal).ToArray();
    internal InventoryState(string? profile)
    {
        if (profile is null) return;
        if (profile != FieldProfile) throw new ArgumentException("Unknown native inventory profile.");
        using var stream = typeof(InventoryState).Assembly.GetManifestResourceStream("SiteSimulation.Inventory.Content.field-kit.json")
            ?? throw new InvalidOperationException("Native inventory content is missing.");
        ContentJson = new StreamReader(stream).ReadToEnd();
        Content = JsonSerializer.Deserialize<InventoryContent>(ContentJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new ArgumentException("Empty inventory content.");
        BodyFits = new BodyFitCatalog();
        ContentJson += "\nBODY-FIT\n" + BodyFits.Source;
        foreach (var item in Content.Items) Items.Add(item.Id, item);
        Regions.AddRange(Content.Regions);
    }
    internal InventoryState(InventoryState other)
    {
        Content = other.Content; ContentJson = other.ContentJson; BodyFits = other.BodyFits;
        foreach (var item in other.Items.Values) Items.Add(item.Id, item);
        Regions.AddRange(other.Regions); Revision = other.Revision;
    }
    public ItemDefinition Definition(string id) => Content!.Definitions.Single(d => d.Id == Items[id].DefinitionId);
    public string Name(string id) => Items.ContainsKey(id) ? Definition(id).Name : Content!.Roots.Single(r => r.Id == id).Name;
    public string EquipmentLabel(string id)
    {
        // Authored numeric fixture suffixes distinguish stand-ins without counting unknown copies elsewhere.
        var prefix = Items[id].DefinitionId + "-";
        return Definition(id).Name + (id.StartsWith(prefix, StringComparison.Ordinal) && int.TryParse(id[prefix.Length..], out var number) ? " #" + number : "");
    }
    public StorageRegion Region(string parentId, string regionId) => (Items.ContainsKey(parentId) ? Definition(parentId).Regions! : Content!.Roots.Single(r => r.Id == parentId).Regions)
        .Single(r => r.Id == regionId);
    public bool IsOpen(string parentId, string regionId) => !Items.ContainsKey(parentId) || (Region(parentId, regionId).Attachment is not null || !Region(parentId, regionId).Sealable) || Regions.Single(r => r.ItemId == parentId && r.RegionId == regionId).Open;
    public IReadOnlyList<InventoryItem> Children(string parentId, string regionId) => Items.Values
        .Where(i => i.Placement.ParentId == parentId && i.Placement.RegionId == regionId).OrderBy(i => i.Id, StringComparer.Ordinal).ToArray();
    internal InventorySnapshot Capture() => new(Revision, Items.Values.OrderBy(i => i.Id, StringComparer.Ordinal).ToArray(), Regions.ToArray(), Pending, SecureQueue.ToArray());
}
