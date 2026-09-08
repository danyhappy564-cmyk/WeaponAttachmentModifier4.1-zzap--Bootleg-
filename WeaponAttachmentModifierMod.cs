using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using Path = System.IO.Path;

namespace WeaponAttachmentModifier;

/// <summary>
/// Rewrites attachment stats in the item database at server start, from
/// <c>config/config.jsonc</c>.
///
/// SPT 4.1 notes:
/// <list type="bullet">
/// <item><c>DatabaseServer</c>/<c>DatabaseTables</c> are gone; the tables are injected
/// individually, so this takes <see cref="TemplateTable"/> and reads <c>.Items</c>.</item>
/// <item><c>IOnLoad.OnLoad()</c> became <c>OnLoadAsync(CancellationToken)</c>.</item>
/// <item><c>OnLoadOrder.PostDBModLoader</c> is gone. <c>PostLoad</c> is the last slot, which is
/// where this belongs: attachments added by other mods (ECOT, the WTT packs, ...) register well
/// before it, so running last means their muzzle devices and grips get retuned too. None of the
/// three stats feed handbook or flea pricing, so running after the ragfair pass is harmless.</item>
/// </list>
/// </summary>
[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostLoad + 100)]
public class WeaponAttachmentModifierMod(
    ISptLogger<WeaponAttachmentModifierMod> logger,
    ModHelper modHelper,
    TemplateTable templates
) : IOnLoad
{
    private const string LogPrefix = "[Weapon Attachment Modifier]";

    public Task OnLoadAsync(CancellationToken cancellationToken = default)
    {
        var config = LoadConfig();
        if (config is null)
        {
            return Task.CompletedTask;
        }

        // Index the per-item overrides once. Upstream ran a List.Find per item, which is a linear
        // scan across every one of the ~4400 attachment templates in the database.
        var overrides = new Dictionary<string, AttachmentOverride>();
        foreach (var entry in config.SpecificAttachmentOverrides)
        {
            if (string.IsNullOrWhiteSpace(entry.ItemId))
            {
                logger.Warning($"{LogPrefix} skipping an override with no ItemId.");
                continue;
            }

            overrides[entry.ItemId] = entry;
        }

        var itemsModified = 0;
        var statsChanged = 0;

        foreach (var item in templates.Items.Values)
        {
            var props = item.Properties;
            if (props is null)
            {
                continue;
            }

            if (overrides.TryGetValue(item.Id.ToString(), out var itemOverride))
            {
                var changed = ApplySpecificOverride(props, itemOverride, item.Name);
                if (changed > 0)
                {
                    itemsModified++;
                    statsChanged += changed;
                }

                continue;
            }

            var applied = ApplyCategory(props, item.Parent.ToString(), config);
            if (applied > 0)
            {
                itemsModified++;
                statsChanged += applied;
            }
        }

        logger.Success(
            $"{LogPrefix} Mod Loaded: {statsChanged} stat(s) changed across {itemsModified} attachment(s)."
        );
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reads config.jsonc. A missing or unreadable file degrades to "change nothing" instead of
    /// taking the server down with it — every knob in <see cref="ModConfig"/> already defaults to
    /// neutral, so there is nothing useful to do without the file anyway.
    /// </summary>
    private ModConfig? LoadConfig()
    {
        var configDir = Path.Combine(
            modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()),
            "config"
        );

        try
        {
            return modHelper.GetJsonDataFromFile<ModConfig>(configDir, "config.jsonc");
        }
        catch (Exception ex)
        {
            logger.Error($"{LogPrefix} could not read config.jsonc from {configDir} - no stats were changed.", ex);
            return null;
        }
    }

    /// <summary>Applies the category tuning for one item. Returns how many stats it changed.</summary>
    private static int ApplyCategory(TemplateItemProperties props, string parentId, ModConfig config)
    {
        var changed = 0;

        if (parentId == AttachmentCategories.Foregrip)
        {
            changed += Tune(props, StatKind.Ergonomics, config.ForegripErgonomics);
            changed += Tune(props, StatKind.Recoil, config.ForegripRecoil);
        }
        else if (parentId == AttachmentCategories.Stock)
        {
            changed += Tune(props, StatKind.Ergonomics, config.StockErgonomics);
            changed += Tune(props, StatKind.Recoil, config.StockRecoil);
        }
        else if (parentId == AttachmentCategories.PistolGrip)
        {
            changed += Tune(props, StatKind.Ergonomics, config.PistolGripErgonomics);
        }
        else if (AttachmentCategories.IsMuzzleDevice(parentId))
        {
            changed += Tune(props, StatKind.Ergonomics, config.MuzzleDeviceErgonomics);
            changed += Tune(props, StatKind.Recoil, config.MuzzleDeviceRecoil);
            changed += Tune(props, StatKind.DurabilityBurn, config.MuzzleDeviceDurability);
        }

        return changed;
    }

    /// <summary>Runs one stat through <see cref="StatMath"/> and writes it back. 1 if it changed.</summary>
    private static int Tune(TemplateItemProperties props, StatKind kind, StatSettings settings)
    {
        var current = Read(props, kind);
        if (current is null)
        {
            return 0;
        }

        if (!StatMath.TryApply(kind, current.Value, settings, out var tuned))
        {
            return 0;
        }

        Write(props, kind, tuned);
        return 1;
    }

    private int ApplySpecificOverride(TemplateItemProperties props, AttachmentOverride entry, string? itemName)
    {
        var label = string.IsNullOrEmpty(entry.Name) ? itemName ?? entry.ItemId : entry.Name;
        var changed = 0;

        // Each override only lands when the item actually carries that stat, so listing a scope
        // under ErgonomicsOverride does not silently invent a recoil value on it.
        if (entry.ErgonomicsOverride.HasValue && props.Ergonomics.HasValue)
        {
            props.Ergonomics = entry.ErgonomicsOverride.Value;
            logger.Info($"{LogPrefix} Overriding {label} Ergo to {props.Ergonomics}");
            changed++;
        }

        if (entry.RecoilPercentageOverride.HasValue && props.Recoil.HasValue)
        {
            props.Recoil = entry.RecoilPercentageOverride.Value;
            logger.Info($"{LogPrefix} Overriding {label} Recoil % to {props.Recoil}");
            changed++;
        }

        if (entry.DurabilityBurnOverride.HasValue && props.DurabilityBurnModificator.HasValue)
        {
            props.DurabilityBurnModificator = entry.DurabilityBurnOverride.Value;
            logger.Info($"{LogPrefix} Overriding {label} Durability Burn Rate to {props.DurabilityBurnModificator}");
            changed++;
        }

        return changed;
    }

    // Ergonomics, Recoil and DurabilityBurnModificator are all `double?` on
    // TemplateItemProperties in 4.1, so these are plain accessors. Upstream reached them through
    // reflection (FindProp/SetNumber/SetFloat) and rounded ergonomics and recoil to whole
    // numbers on the way in, which is wrong for this data: real templates carry values like
    // -21.25 recoil and -0.5 ergonomics, and rounding a 1.2x multiplier on -0.5 ergo turned it
    // into -1 (a 67% error). Nothing rounds any more.
    private static double? Read(TemplateItemProperties props, StatKind kind) => kind switch
    {
        StatKind.Ergonomics => props.Ergonomics,
        StatKind.Recoil => props.Recoil,
        StatKind.DurabilityBurn => props.DurabilityBurnModificator,
        _ => null,
    };

    private static void Write(TemplateItemProperties props, StatKind kind, double value)
    {
        switch (kind)
        {
            case StatKind.Ergonomics:
                props.Ergonomics = value;
                break;
            case StatKind.Recoil:
                props.Recoil = value;
                break;
            case StatKind.DurabilityBurn:
                props.DurabilityBurnModificator = value;
                break;
        }
    }
}
