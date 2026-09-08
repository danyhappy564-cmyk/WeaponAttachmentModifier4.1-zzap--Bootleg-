using BepInEx.Configuration;

namespace WeaponAttachmentModifier.Client;

/// <summary>How an F12 stat block overrides the item's own value.</summary>
public enum OverrideMode
{
    /// <summary>Use the multiplier (the default).</summary>
    Off,

    /// <summary>Ignore the item's value and use Override Value verbatim.</summary>
    HardSet,

    /// <summary>Add Override Value to the item's value.</summary>
    Additive,
}

/// <summary>
/// One stat's three F12 entries, bundled so <see cref="ToSettings"/> can hand the shared
/// <see cref="StatMath"/> exactly the same shape the server config binds to.
/// </summary>
internal sealed class StatBinding
{
    private readonly ConfigEntry<float> _multiplier;
    private readonly ConfigEntry<OverrideMode> _mode;
    private readonly ConfigEntry<float> _overrideValue;

    internal StatBinding(
        ConfigFile config,
        string section,
        string stat,
        string multiplierHelp,
        int order)
    {
        _multiplier = config.Bind(
            section,
            $"{stat} Multiplier",
            1f,
            new ConfigDescription(
                multiplierHelp,
                new AcceptableValueRange<float>(0.1f, 5f),
                new ConfigurationManagerAttributes { Order = order }));

        _mode = config.Bind(
            section,
            $"{stat} Override Mode",
            OverrideMode.Off,
            new ConfigDescription(
                $"Off uses the {stat} Multiplier. HardSet replaces the value outright, Additive adds to it. "
                + "Either override wins over the multiplier.",
                null,
                new ConfigurationManagerAttributes { Order = order - 1, IsAdvanced = true }));

        _overrideValue = config.Bind(
            section,
            $"{stat} Override Value",
            0f,
            new ConfigDescription(
                $"The number used by {stat} Override Mode. Ignored while the mode is Off.",
                null,
                new ConfigurationManagerAttributes { Order = order - 2, IsAdvanced = true }));
    }

    /// <summary>Snapshot of the current F12 values in the form the shared formula takes.</summary>
    internal StatSettings ToSettings() => new()
    {
        EnableHardSetOverride = _mode.Value == OverrideMode.HardSet,
        HardSetOverrideValue = _overrideValue.Value,
        EnableAdditiveOverride = _mode.Value == OverrideMode.Additive,
        AdditiveOverrideValue = _overrideValue.Value,
        Multiplier = _multiplier.Value,
    };
}

/// <summary>Every knob the F12 window exposes.</summary>
internal sealed class TunerSettings
{
    private const string ErgoHelp =
        "Above 1 makes ergonomics better: buffs are multiplied and penalties are divided. Below 1 does the reverse.";

    private const string RecoilHelp =
        "Above 1 strengthens the attachment's recoil effect (-20% at 1.5 becomes -30%). Below 1 weakens it.";

    private const string BurnHelp =
        "Above 1 burns barrel durability faster (+50% at 1.5 becomes +75%). Below 1 is gentler.";

    internal ConfigEntry<bool> Enabled { get; }
    internal ConfigEntry<bool> VerboseLogging { get; }

    internal StatBinding ForegripErgonomics { get; }
    internal StatBinding ForegripRecoil { get; }
    internal StatBinding StockErgonomics { get; }
    internal StatBinding StockRecoil { get; }
    internal StatBinding PistolGripErgonomics { get; }
    internal StatBinding MuzzleErgonomics { get; }
    internal StatBinding MuzzleRecoil { get; }
    internal StatBinding MuzzleDurability { get; }

    internal TunerSettings(ConfigFile config)
    {
        const string general = "0. General";
        Enabled = config.Bind(
            general,
            "Enabled",
            true,
            new ConfigDescription(
                "Turn the whole plugin off. Attachment stats snap back to whatever the server sent, live.",
                null,
                new ConfigurationManagerAttributes { Order = 100 }));

        VerboseLogging = config.Bind(
            general,
            "Verbose Logging",
            false,
            new ConfigDescription(
                "Write a line to the BepInEx log every time the stats are re-applied.",
                null,
                new ConfigurationManagerAttributes { Order = 90, IsAdvanced = true }));

        const string foregrip = "1. Foregrip";
        ForegripErgonomics = new StatBinding(config, foregrip, "Ergonomics", ErgoHelp, 100);
        ForegripRecoil = new StatBinding(config, foregrip, "Recoil", RecoilHelp, 90);

        const string stock = "2. Stock";
        StockErgonomics = new StatBinding(config, stock, "Ergonomics", ErgoHelp, 100);
        StockRecoil = new StatBinding(config, stock, "Recoil", RecoilHelp, 90);

        const string pistolGrip = "3. Pistol Grip";
        PistolGripErgonomics = new StatBinding(config, pistolGrip, "Ergonomics", ErgoHelp, 100);

        // Brakes/compensators, suppressors and thread adapters all share this block, matching
        // the server config's single MuzzleDevice* group.
        const string muzzle = "4. Muzzle Device";
        MuzzleErgonomics = new StatBinding(config, muzzle, "Ergonomics", ErgoHelp, 100);
        MuzzleRecoil = new StatBinding(config, muzzle, "Recoil", RecoilHelp, 90);
        MuzzleDurability = new StatBinding(config, muzzle, "Durability Burn", BurnHelp, 80);
    }
}
