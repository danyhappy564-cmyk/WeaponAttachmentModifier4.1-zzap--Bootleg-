using SPTarkov.Server.Core.Models.Spt.Mod;

namespace WeaponAttachmentModifier;

/// <summary>
/// SPT 4.1 replaced <c>AbstractModMetadata</c> (an abstract record with overridable members)
/// with the <c>IModMetadata</c> interface: <c>IsBundleMod</c> is gone and <c>HasPrepatcher</c>
/// is new.
/// </summary>
public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.mconie.weaponattachmentmodifier";
    public string Name { get; init; } = "WeaponAttachmentModifier";
    public string Author { get; init; } = "McOnie";
    public SemanticVersioning.Version Version { get; init; } = new("3.0.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.5");
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; }

    public List<string>? Contributors { get; init; }
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/danyhappy564-cmyk/WeaponAttachmentModifier4.1";
}
