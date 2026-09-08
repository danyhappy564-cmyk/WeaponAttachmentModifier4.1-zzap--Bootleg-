// ReSharper disable UnusedMember.Global
#pragma warning disable CS0169 // field never used - ConfigurationManager reads these by reflection

namespace WeaponAttachmentModifier.Client;

/// <summary>
/// The standard drop-in class BepInEx's ConfigurationManager (the F12 window) looks for by name.
/// It is never referenced by ConfigurationManager as a type - the fields are read reflectively -
/// so this deliberately carries no reference to it and is safe when the plugin is absent.
/// Only the members this mod actually sets are declared.
/// </summary>
internal sealed class ConfigurationManagerAttributes
{
    /// <summary>Higher numbers sort earlier inside a section.</summary>
    public int? Order;

    /// <summary>Hide behind the "Advanced" toggle.</summary>
    public bool? IsAdvanced;
}
