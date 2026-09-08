using System.Collections.Generic;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using JsonType;

namespace WeaponAttachmentModifier.Client;

/// <summary>
/// Applies the F12 settings straight onto the client's loaded item templates.
///
/// This works because nothing downstream caches the numbers: <c>Mod.Ergonomics</c> is
/// <c>GetTemplate&lt;ModTemplate&gt;().Ergonomics</c>, and <c>Weapon.ErgonomicsDelta</c> /
/// <c>Weapon.RecoilDelta</c> re-sum <c>mod.Template.*</c> on every read. Writing a template field
/// therefore moves the inspect window, the inventory stat bars and the weapon's real handling at
/// the same time, with no restart.
/// </summary>
internal sealed class TemplateTuner
{
    /// <summary>Which config block a template belongs to, decided once at capture time.</summary>
    private enum Category
    {
        Foregrip,
        Stock,
        PistolGrip,
        MuzzleDevice,
    }

    /// <summary>A template plus the values it had before this plugin ever touched it.</summary>
    private sealed class Entry
    {
        internal ModTemplate Template = null!;
        internal MuzzleModTemplate? Muzzle;
        internal Category Category;
        internal float BaseErgonomics;
        internal float BaseRecoil;
        internal float BaseDurabilityBurn;
    }

    private readonly ManualLogSource _log;
    private readonly TunerSettings _settings;

    private readonly List<Entry> _entries = new();

    // Reference identity, so a template we already captured is never re-captured from values we
    // ourselves wrote.
    private readonly HashSet<ItemTemplate> _captured = new();

    private ItemTemplates? _bound;
    private int _boundCount = -1;
    private bool _dirty;

    internal TemplateTuner(ManualLogSource log, TunerSettings settings)
    {
        _log = log;
        _settings = settings;
    }

    /// <summary>Ask for a re-apply on the next tick. Cheap and safe to call repeatedly.</summary>
    internal void MarkDirty() => _dirty = true;

    internal void Tick()
    {
        if (!Singleton<ItemFactory>.Instantiated)
        {
            // Between profiles / back at the launcher: forget the binding so the next factory
            // gets a fresh baseline instead of one taken from a dictionary that no longer exists.
            _bound = null;
            _boundCount = -1;
            return;
        }

        var templates = Singleton<ItemFactory>.Instance?.ItemTemplates;
        if (templates == null || templates.Count == 0)
        {
            return;
        }

        if (!ReferenceEquals(templates, _bound))
        {
            // A different dictionary means templates were just (re)loaded from the server, so
            // everything in it is pristine and the whole baseline is rebuilt.
            Capture(templates, full: true);
            _bound = templates;
            _boundCount = templates.Count;
            _dirty = true;
        }
        else if (templates.Count != _boundCount)
        {
            // Same dictionary, more entries: capture only the ones we have not seen, because the
            // rest already carry our edits.
            Capture(templates, full: false);
            _boundCount = templates.Count;
            _dirty = true;
        }

        if (!_dirty)
        {
            return;
        }

        Apply();
        _dirty = false;
    }

    private void Capture(ItemTemplates templates, bool full)
    {
        if (full)
        {
            _entries.Clear();
            _captured.Clear();
        }

        var added = 0;
        foreach (var pair in templates)
        {
            var template = pair.Value;
            if (template == null || _captured.Contains(template))
            {
                continue;
            }

            // ParentId is the template's _parent, the same 24-hex category id the server config
            // groups by.
            var parentId = template.ParentId?.ToString();
            if (string.IsNullOrEmpty(parentId))
            {
                continue;
            }

            Category category;
            if (parentId == AttachmentCategories.Foregrip)
            {
                category = Category.Foregrip;
            }
            else if (parentId == AttachmentCategories.Stock)
            {
                category = Category.Stock;
            }
            else if (parentId == AttachmentCategories.PistolGrip)
            {
                category = Category.PistolGrip;
            }
            else if (AttachmentCategories.IsMuzzleDevice(parentId))
            {
                category = Category.MuzzleDevice;
            }
            else
            {
                continue;
            }

            // Every one of the four categories is a ModTemplate subclass (ForegripTemplate,
            // StockTemplate, PistolGripTemplate, and MuzzleModTemplate with SilencerTemplate and
            // MuzzleComboTemplate under it), so Ergonomics and Recoil are always reachable.
            // Anything else in the category is left alone rather than guessed at.
            if (!(template is ModTemplate modTemplate))
            {
                continue;
            }

            var muzzle = modTemplate as MuzzleModTemplate;
            _entries.Add(new Entry
            {
                Template = modTemplate,
                Muzzle = muzzle,
                Category = category,
                BaseErgonomics = modTemplate.Ergonomics,
                BaseRecoil = modTemplate.Recoil,
                BaseDurabilityBurn = muzzle?.DurabilityBurnModificator ?? 0f,
            });
            _captured.Add(template);
            added++;
        }

        if (added > 0)
        {
            _log.LogInfo($"Captured baseline stats for {added} attachment template(s) ({_entries.Count} total).");
        }
    }

    private void Apply()
    {
        var on = _settings.Enabled.Value;

        // Snapshot the settings once per pass rather than per template.
        var foregripErgo = _settings.ForegripErgonomics.ToSettings();
        var foregripRecoil = _settings.ForegripRecoil.ToSettings();
        var stockErgo = _settings.StockErgonomics.ToSettings();
        var stockRecoil = _settings.StockRecoil.ToSettings();
        var gripErgo = _settings.PistolGripErgonomics.ToSettings();
        var muzzleErgo = _settings.MuzzleErgonomics.ToSettings();
        var muzzleRecoil = _settings.MuzzleRecoil.ToSettings();
        var muzzleBurn = _settings.MuzzleDurability.ToSettings();

        foreach (var entry in _entries)
        {
            switch (entry.Category)
            {
                case Category.Foregrip:
                    entry.Template.Ergonomics = Resolve(on, StatKind.Ergonomics, entry.BaseErgonomics, foregripErgo);
                    entry.Template.Recoil = Resolve(on, StatKind.Recoil, entry.BaseRecoil, foregripRecoil);
                    break;

                case Category.Stock:
                    entry.Template.Ergonomics = Resolve(on, StatKind.Ergonomics, entry.BaseErgonomics, stockErgo);
                    entry.Template.Recoil = Resolve(on, StatKind.Recoil, entry.BaseRecoil, stockRecoil);
                    break;

                case Category.PistolGrip:
                    entry.Template.Ergonomics = Resolve(on, StatKind.Ergonomics, entry.BaseErgonomics, gripErgo);
                    break;

                case Category.MuzzleDevice:
                    entry.Template.Ergonomics = Resolve(on, StatKind.Ergonomics, entry.BaseErgonomics, muzzleErgo);
                    entry.Template.Recoil = Resolve(on, StatKind.Recoil, entry.BaseRecoil, muzzleRecoil);
                    if (entry.Muzzle != null)
                    {
                        entry.Muzzle.DurabilityBurnModificator =
                            Resolve(on, StatKind.DurabilityBurn, entry.BaseDurabilityBurn, muzzleBurn);
                    }

                    break;
            }
        }

        if (_settings.VerboseLogging.Value)
        {
            _log.LogInfo(on
                ? $"Applied settings to {_entries.Count} attachment template(s)."
                : $"Disabled - restored {_entries.Count} attachment template(s) to their server values.");
        }
    }

    /// <summary>
    /// Always resolves to an absolute value rather than editing in place: every write starts from
    /// the captured baseline, so dragging a slider back to 1.0 (or unticking Enabled) restores the
    /// original number instead of leaving the last multiplier baked in, and re-applying any number
    /// of times is a no-op.
    /// </summary>
    private static float Resolve(bool enabled, StatKind kind, float baseValue, StatSettings settings)
    {
        if (!enabled)
        {
            return baseValue;
        }

        return StatMath.TryApply(kind, baseValue, settings, out var tuned) ? (float)tuned : baseValue;
    }
}
