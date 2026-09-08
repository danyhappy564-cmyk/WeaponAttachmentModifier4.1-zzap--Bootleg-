using BepInEx;

namespace WeaponAttachmentModifier.Client;

/// <summary>
/// F12 front end for Weapon Attachment Modifier.
///
/// The server mod bakes <c>config/config.jsonc</c> into the item database at boot; this plugin
/// layers live adjustments on top of whatever the client received, so the two stack. Both ship
/// neutral, so out of the box only the one you actually edit does anything.
///
/// No Harmony patches: the plugin only writes public fields on templates the game already has
/// loaded, which is why it has nothing to bind against and nothing to break on a game update.
/// </summary>
[BepInPlugin(PluginGuid, "Weapon Attachment Modifier", "3.0.0")]
public class WeaponAttachmentModifierPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.mconie.weaponattachmentmodifier.client";

    private TemplateTuner _tuner = null!;

    private void Awake()
    {
        var settings = new TunerSettings(Config);
        _tuner = new TemplateTuner(Logger, settings);

        // One handler covers every entry, so each F12 edit schedules a re-apply on the next
        // frame. It also fires for a Config.Reload(), which is what picks up hand edits to the
        // .cfg.
        Config.SettingChanged += (_, _) => _tuner.MarkDirty();

        Logger.LogInfo("Weapon Attachment Modifier client loaded. Adjust it with F12.");
    }

    private void Update()
    {
        _tuner.Tick();
    }
}
