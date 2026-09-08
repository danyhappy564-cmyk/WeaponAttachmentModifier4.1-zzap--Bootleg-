namespace WeaponAttachmentModifier;

/// <summary>
/// A hard-set stat override for one specific attachment, matched by template id. Anything left
/// null is not touched, and an item listed here skips its category's tuning entirely.
/// </summary>
public class AttachmentOverride
{
    /// <summary>Template id (<c>_id</c>) of the attachment to override.</summary>
    public string ItemId { get; set; } = string.Empty;

    /// <summary>Free-text label used in log lines. Falls back to the item's own name.</summary>
    public string? Name { get; set; }

    public double? ErgonomicsOverride { get; set; }
    public double? RecoilPercentageOverride { get; set; }
    public double? DurabilityBurnOverride { get; set; }
}

/// <summary>
/// Shape of <c>config/config.jsonc</c>. Field names are matched case-sensitively by SPT's JSON
/// reader, so they must stay spelled exactly as the shipped config writes them.
/// </summary>
public class ModConfig
{
    public StatSettings ForegripErgonomics { get; set; } = new();
    public StatSettings ForegripRecoil { get; set; } = new();
    public StatSettings StockErgonomics { get; set; } = new();
    public StatSettings StockRecoil { get; set; } = new();
    public StatSettings PistolGripErgonomics { get; set; } = new();
    public StatSettings MuzzleDeviceErgonomics { get; set; } = new();
    public StatSettings MuzzleDeviceRecoil { get; set; } = new();
    public StatSettings MuzzleDeviceDurability { get; set; } = new();

    public List<AttachmentOverride> SpecificAttachmentOverrides { get; set; } = [];
}
