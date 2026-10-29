namespace SiteSimulation;

public sealed record ItemDefinition(string Id, string Name, int WidthMm, int LengthMm, int ThicknessMm, int MassGrams,
    string[] Tags, string? WearSlot = null, string? Capability = null, int HandleMs = 300, int HandleAp = 1, StorageRegion[]? Regions = null, WearProfile? Wear = null,
    string? DefaultDropRegion = null, AttachmentProfile? Attachment = null);
public sealed record WearMount(string Slot, string[] Coverage, string? BodySurface = null);
public sealed record WearProfile(int Layer, WearMount[] Mounts, bool Outermost = false,
    string[]? OpenExposes = null, int FastenMs = 700, int FastenAp = 2,
    bool CoversAccess = true, bool BlocksRemoval = true, string[]? OverlapTags = null, string SecureVerb = "Fasten",
    string OpenVerb = "Open", bool SecureOnClose = false, string? FitProfile = null, string[]? RemovalRegions = null, WearFitProfile? Fit = null,
    int? OverlayLimit = null, bool BlocksFastenings = false, BodySurfaceMask[]? FasteningFootprint = null);
public sealed record StorageRegion(string Id, string Name, int Columns, int Rows, int CellMm, int HeightMm, int MaxMassGrams,
    string? MountSlot = null, string[]? AcceptTags = null, int[]? Mask = null, bool EquipmentReady = false, int AccessMs = 300, int AccessAp = 1, string[]? AccessBody = null,
    string SecureVerb = "Close", string OpenVerb = "Open", bool SecureOnClose = true, BodySurfaceMask[]? AccessFootprint = null, AttachmentSurface? Attachment = null, bool Sealable = true, bool Snug = false);
public sealed record InventoryRoot(string Id, string Name, StorageRegion[] Regions, string? ActorId = null, string? PropId = null, int MaxMassGrams = 20000, string? BodyFamily = null);
public sealed record ItemPlacement(string ParentId, string RegionId, int X = 0, int Y = 0, bool Rotated = false, BodyPose? BodyPose = null, BodyPose? AttachmentPose = null, long? WornOrder = null);
public sealed record InventoryItem(string Id, string DefinitionId, ItemPlacement Placement, bool Powered = true, int Condition = 100, bool GarmentOpen = false,
    bool? SecureOnClose = null);
public sealed record RegionState(string ItemId, string RegionId, bool Open = true, bool? SecureOnClose = null);
public sealed record InventoryContent(ItemDefinition[] Definitions, InventoryRoot[] Roots, InventoryItem[] Items, RegionState[] Regions);
public enum InventoryActionKind { Move, Open, Close, PowerOn, PowerOff, OpenGarment, CloseGarment, AutoPlace, Arrange }
public sealed record InventoryRequest(InventoryActionKind Kind, string ItemId, ItemPlacement? Destination = null, string? RegionId = null);
public sealed record InventoryWork(string ActorId, InventoryRequest Request, Point2 Position, int TotalMs, int RemainingMs,
    long Revision, int CostAp, bool Tactical, string? AbortReason = null, InventoryPackingPlan? Packing = null);
public sealed record InventoryPackingMove(string ItemId, ItemPlacement From, ItemPlacement To);
public sealed record InventoryPackingPlan(InventoryPackingMove[] Moves, bool Repacked);
public sealed record InventoryPackingQuote(bool Allowed, string Reason, int DurationMs, int Ap, InventoryPackingPlan? Plan);
public sealed record InventorySecureStep(InventoryRequest Request, ItemPlacement Placement, bool Automatic = false);
public sealed record InventorySnapshot(long Revision, InventoryItem[] Items, RegionState[] Regions, InventoryWork? Pending,
    InventorySecureStep[]? SecureQueue = null);
public sealed record InventoryAccess(bool Allowed, string Reason);

public sealed record InventoryActionQuote(bool Allowed, string Reason, int DurationMs, int Ap);
public sealed record InventoryClosureAction(string Verb, InventoryRequest Request);
