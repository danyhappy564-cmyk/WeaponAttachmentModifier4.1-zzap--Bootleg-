# Weapon Attachment Modifier

Retune weapon attachment stats — ergonomics, recoil and muzzle durability burn — for **SPT 4.1.5**.

Ported from [McOnie's original](https://github.com/McOnie/WeaponAttachmentModifier) (SPT 4.0.3),
with a BepInEx client plugin added so the multipliers can be adjusted **live from the F12 menu**
instead of only from a config file at server start.

## The two halves

| | What it is | Where it is configured | When it applies |
|---|---|---|---|
| **Server mod** | `user\mods\WeaponAttachmentModifier` | `config\config.jsonc` | Baked into the item database at server start |
| **Client plugin** | `BepInEx\plugins\WeaponAttachmentModifier` | **F12** (BepInEx ConfigurationManager) | Live, no restart |

Both ship completely neutral — every multiplier is `1.0` and both override switches are off — so a
fresh install changes nothing until you touch one of them.

**They stack.** The plugin captures whatever stats the client received from the server and applies
its own numbers on top. So pick one: leave `config.jsonc` alone and tune with F12 (the easy way),
or leave F12 alone and tune the file. Setting a 1.5x in both gives you 2.25x.

Neither half needs the other. The server mod works on its own, and the plugin works on its own.

## Using F12

Launch the game, press **F12**, find *Weapon Attachment Modifier*. Four sections:

- `1. Foregrip` — ergonomics, recoil
- `2. Stock` — ergonomics, recoil
- `3. Pistol Grip` — ergonomics
- `4. Muzzle Device` — ergonomics, recoil, durability burn (brakes/compensators, suppressors and
  thread adapters share this block)

Every stat has a **Multiplier** slider, plus an *Override Mode* / *Override Value* pair behind the
**Advanced** toggle for when you want to pin an exact number rather than scale one.

Changes take effect the moment you release the slider. Move it back to `1.0`, or untick
`0. General → Enabled`, and the stats snap straight back to what the server sent — the plugin
always recomputes from the original values rather than editing what it wrote last time, so nothing
gets baked in and nothing compounds.

One caveat: a weapon already spawned in an active raid keeps the numbers it was built with. Tune in
the hideout or on the main menu and the next raid picks it up.

## What the multipliers mean

### Ergonomics
Sign matters here, because a positive ergonomics value on an attachment is a bonus and a negative
one is a penalty. So the multiplier scales bonuses **up** and penalties **down**:

- `+10` ergo at `2.0` → `+20`
- `-10` ergo at `2.0` → `-5`

Above `1.0` is always "better gun", below `1.0` is always "worse gun".

### Recoil
Plain multiply. Attachment recoil values are percentages, and a stronger negative means more recoil
reduction:

- `-20%` at `1.5` → `-30%`
- `-20%` at `0.5` → `-10%`

### Durability burn
Plain multiply, on muzzle devices only. Above `1.0` wears the barrel faster:

- `+50%` at `1.5` → `+75%`

## Override modes

Both halves check the same three settings in the same order — hard set first, then additive, then
the multiplier:

| Mode | Effect |
|---|---|
| **HardSet** | Ignore the item's value entirely and use yours |
| **Additive** | Item's value **+** yours |
| **Off** / multiplier | Scale by the multiplier per the rules above |

The formula lives in one file (`Shared/AttachmentStats.cs`) that is compiled into both the server
mod and the plugin, so an F12 slider and a `config.jsonc` value set to the same number always
produce the same stat.

## Per-item overrides

`config.jsonc` has a `SpecificAttachmentOverrides` list for pinning exact values on individual
attachments by template id. An item listed there is set to precisely those numbers and skips its
category's tuning entirely.

This is server-side only — F12 has no list editor — and the shipped config carries a commented-out
example. Only the fields you include are touched, and a field is ignored if the item does not carry
that stat to begin with.

## Building

`Directory.Build.props` defaults `SptRoot` to `E:\SPT 4.1`. Override it with a `local.props`
(gitignored) or on the command line:

```
dotnet build -c Release -p:SptRoot="D:\Your SPT"
```

A successful build deploys itself:

- `$(SptRoot)\SPT_Runtime\user\mods\WeaponAttachmentModifier\` — the server mod and, on a first
  build only, `config\config.jsonc` (an existing config is never overwritten)
- `$(SptRoot)\BepInEx\plugins\WeaponAttachmentModifier\` — the client plugin

Pass `-p:SkipDeploy=true` when the server or the game is running and holding a dll open, or set
`SptRoot` to `CHANGE_ME` to turn deployment off entirely. Deployment only runs on Windows, since
`SptRoot` is a Windows path; add `-p:OS=Windows_NT` if you need it elsewhere.

## Notes on the 4.1 port

- `AbstractModMetadata` became the `IModMetadata` interface (`IsBundleMod` gone, `HasPrepatcher`
  new), and `IOnLoad.OnLoad()` became `OnLoadAsync(CancellationToken)`.
- `DatabaseServer` / `DatabaseTables` no longer exist; the mod injects `TemplateTable` directly.
- `OnLoadOrder.PostDBModLoader` is gone. The pass now runs at `PostLoad`, the last slot, so
  attachments added by other mods get retuned too.
- **Ergonomics and recoil are no longer rounded to whole numbers.** The original reached these
  properties through reflection and rounded on the way in; on SPT 4.1 they are plain `double?`, and
  real templates carry values like `-21.25` recoil and `-0.5` ergonomics. Rounding a `1.2x`
  multiplier on `-0.5` ergo turned it into `-1`, a 67% error. Nothing rounds any more.
- A missing or unreadable `config.jsonc` now logs an error and changes nothing instead of taking
  the server down — which matters, because the original repository never shipped the file it reads.

## Credits

Original mod by **McOnie**. MIT licensed.
