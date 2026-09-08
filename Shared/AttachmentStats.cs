using System;

namespace WeaponAttachmentModifier;

/// <summary>
/// The three attachment stats this mod can retune. Named rather than string-keyed so the
/// server pass and the client plugin cannot drift apart on a typo.
/// </summary>
public enum StatKind
{
    Ergonomics,
    Recoil,
    DurabilityBurn,
}

/// <summary>
/// Item-category ids (a template's <c>_parent</c>) for the attachment groups the mod knows
/// how to retune. Identical on both sides: the server reads <c>TemplateItem.Parent</c>, the
/// client reads <c>ItemTemplate.ParentId</c>, and both hold the same 24-hex category id.
/// </summary>
public static class AttachmentCategories
{
    public const string Foregrip = "55818af64bdc2d5b648b4570";
    public const string Stock = "55818a594bdc2db9688b456a";
    public const string PistolGrip = "55818a684bdc2ddd698b456d";
    public const string MuzzleBrake = "5448fe394bdc2d0d028b456c";
    public const string Suppressor = "550aa4cd4bdc2dd8348b456c";
    public const string MuzzleAdapter = "550aa4dd4bdc2dc9348b4569";

    public static bool IsMuzzleDevice(string parentId) =>
        parentId == MuzzleBrake || parentId == Suppressor || parentId == MuzzleAdapter;
}

/// <summary>
/// One stat's tuning knobs. The property names are the config.jsonc field names, so this
/// type is what <c>System.Text.Json</c> binds the server config onto directly.
/// </summary>
public class StatSettings
{
    public bool EnableHardSetOverride { get; set; }
    public double HardSetOverrideValue { get; set; }
    public bool EnableAdditiveOverride { get; set; }
    public double AdditiveOverrideValue { get; set; }
    public double Multiplier { get; set; } = 1.0;
}

/// <summary>
/// The tuning formula, kept in one place and compiled into both the server mod (net10.0) and
/// the BepInEx plugin (netstandard2.1) so an F12 slider and a config.jsonc value with the same
/// number always produce the same stat.
/// </summary>
public static class StatMath
{
    /// <summary>A multiplier this close to zero would blow up the negative-ergonomics divide.</summary>
    private const double MinDivisorMagnitude = 0.01;

    /// <summary>
    /// Applies <paramref name="settings"/> to <paramref name="baseValue"/>.
    /// Returns false when the settings are neutral, in which case the caller must leave the
    /// stat alone rather than writing <paramref name="baseValue"/> back — that keeps repeated
    /// applies idempotent and keeps "changed" counts honest.
    /// </summary>
    public static bool TryApply(StatKind kind, double baseValue, StatSettings settings, out double result)
    {
        // Hard set wins outright, then additive, then the multiplier. Matches the precedence
        // the upstream mod documented and the order the config.jsonc comments describe.
        if (settings.EnableHardSetOverride)
        {
            result = settings.HardSetOverrideValue;
            return true;
        }

        if (settings.EnableAdditiveOverride)
        {
            result = baseValue + settings.AdditiveOverrideValue;
            return true;
        }

        if (settings.Multiplier != 1.0)
        {
            // Ergonomics is the one stat where the sign flips the meaning: a positive value is
            // a buff and a negative value is a penalty, so "multiplier > 1 = better gun" means
            // scaling buffs up but scaling penalties *down*, i.e. dividing.
            if (kind == StatKind.Ergonomics && baseValue < 0)
            {
                if (Math.Abs(settings.Multiplier) <= MinDivisorMagnitude)
                {
                    result = baseValue;
                    return false;
                }

                result = baseValue / settings.Multiplier;
                return true;
            }

            result = baseValue * settings.Multiplier;
            return true;
        }

        result = baseValue;
        return false;
    }
}
