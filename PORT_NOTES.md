# WoW: Forever port notes

Branch: `forever-port`. Rule: every client fact below was **measured in the client**.
Anything not measured is listed under *Open* and is not assumed by the code.

## Corrections (read first)

- WoW: Forever is **not** vanilla 1.12. It is a new official Blizzard product
  (Battle.net, worldofwarcraft.blizzard.com/forever) on a **modern engine**.
- Therefore: **no** legacy code paths, **no** "Interact With Target" workaround, **no**
  secure-button workaround. Nothing may route Forever into `DataToColor.IsLegacy()`.
- The talent tree is **not** the vanilla tree: more tiers, different talents.
  Improved Blizzard is unreachable before ~level 30. Never derive spells or talents
  from vanilla rank tables.

## Measured facts

| Fact | Value | Source |
|---|---|---|
| `GetBuildInfo()` version | `1.60.1` | `/dump` in client |
| Build | `70959` | user |
| `WOW_PROJECT_ID` | **18** | `/dump` in client |
| `WOW_PROJECT_FOREVER` | `nil`: the client defines no constant, so the code uses the literal 18 | `/dump` in client |
| `GetLocale()` | `ruRU` | `/dump` in client |
| Mage @22 talents | Frost 6 (Improved Frostbolt 5/5 + 1 in tier 2), Arcane 7, 0 unspent | user |

18 does not collide with any existing `ClientVersion` value (1, 2, 5, 11, 14, 19, 91–95),
so no build-range branching is needed.

## What changed

| File | Change |
|---|---|
| `SharedLib/AddonDataProviderType/ClientVersion.cs` | `Forever = 18` |
| `SharedLib/StartupConfig/StartupClientVersion.cs` | exe `FileVersion` `{1, 60}` → `(Forever, "forever")`. **Must** come before the SoM arm, otherwise `1.60` matches `{1, >=13}` and the bot thinks it is Classic Era. |
| `DataConfig/DataConfig.cs` | `ClientEra("forever") = "precata"`; new `ContentExp()` (see interim below) |
| `Frontend/wwwroot/script/leaflet-watch.js` | `clientEra('forever') = 'precata'` (JS mirror of `ClientEra`) |
| `Addons/DataToColor/Versions.lua` | `DataToColor.IsForever()` = `WOW_PROJECT_ID == 18` (project ID, not build range). `ClientVersion = WOW_PROJECT_ID` already yields 18 unpatched. |
| `Addons/DataToColor/*.toc` | patch bump 1.13.3 → 1.13.4 (Lua changed) |
| `Json/class/Mage_22_Frost_Forever.json` | single-target profile, spells gated by `Spell:<id>` IntVariables |
| `SharedLib/CLAUDE.md` | enum count 12 → 13 |

Not built here: the cloud container has no .NET SDK and cannot reach Microsoft's
download host. Run `dotnet build -c Release` on Windows.

## Interim decisions (unverified, marked in code)

1. **Data folder = `som`.** `DataConfig.ContentExp("forever")` returns `"som"` for dbc,
   area, experience, subzones, History, npcspawnlocations and mailboxlocations, because
   no `Json/*/forever` data exists yet. Spell, item and creature IDs and the XP table may
   differ in 1.60.1. Remove the arm once Forever data is extracted. `ReadDBC_CSV` has no
   Forever build entry, and wago.tools availability for build 70959 is unchecked.
2. **Era = `precata`** for navmesh, tiles, road and area_grid. The geometry has not been
   hash-compared against a 1.15 client yet. If the world differs, give Forever its own era.
3. **Wowhead:** `WApi` falls to the default (retail) branch for Forever. Left as is: which
   site (if any) covers Forever is unknown.

## Addon behaviour with ID 18 (not changed, needs verification)

ID 18 makes `IsClassic()` and `IsClassicPreCata()` false, and `IsClassicEra()` true.
Effects:

- Focus unit = `focus` (not the `party1` vanilla hack); the C# side uses `TARGETFOCUS`.
  Correct only if Forever has a focus unit and binding.
- Ammo and pet happiness are treated as "post-Cata" (always OK). Wrong for hunters if
  Forever has ammo or happiness.
- The SoM cast-event handlers and LibClassicCasterino are **off**. Correct if
  `UnitCastingInfo` works for all units (modern engine: likely, but unverified).
- `IsVanilla()` depends on the TOC number from `GetBuildInfo()` (4th return), which is
  not measured yet.

## Localization

The client is **ruRU**. Profiles and `NpcNameFinder` assume English names: spell and buff
names in profiles, and NPC name plates matched against English DBC names.

- Switch the client to **enUS** (Battle.net → game settings → Text language).
- Where a name cannot be avoided, use `Spell:<IntVariable>` with the spell ID from the
  spellbook, and `Buff_<name>: <iconId>` IntVariables for auras (icon ID, not spell ID).

## Mage_22_Frost_Forever.json

- Single target only: no Blizzard, Arcane Explosion or Cone of Cold. Frost Nova only
  when no adds are visible.
- Every spell is gated by `Spell:Spell_*`. All IDs ship as **0**, so the profile is inert
  until the real IDs are filled in. Fill them from the spellbook dump below.
- `Item_Conjure_*` = 0: conjured food and water item IDs are unknown for Forever.
- `PathFilename` is empty: pick a route for your zone.
- Default keys: 1 Frostbolt, 3 Frost Armor, 4 Arcane Intellect, 5 Fire Blast,
  6 Frost Nova, 8 Conjure Food, 9 Conjure Water, 0 Shoot, N1 food, N2 drink.

### Dump commands (run in client, enUS)

Spellbook, old API:
```
/run for i=1,200 do local n=GetSpellBookItemName(i,"spell") if not n then break end print(i,n,select(2,GetSpellBookItemInfo(i,"spell"))) end
```
Spellbook, `C_SpellBook` API (if the line above errors):
```
/run local B,P=C_SpellBook,Enum.SpellBookSpellBank.Player for i=1,200 do local n=B.GetSpellBookItemName(i,P) if not n then break end print(i,n,B.GetSpellBookItemInfo(i,P).spellID) end
```
Player buff icon IDs (cast Frost Armor and Arcane Intellect first):
```
/run for i=1,40 do local a=C_UnitAuras.GetBuffDataByIndex("player",i) if not a then break end print(a.name,a.icon,a.spellId) end
```
Bag item IDs (conjured food and water):
```
/run for b=0,4 do for s=1,C_Container.GetContainerNumSlots(b) do local id=C_Container.GetContainerItemID(b,s) if id then print(b,s,id,C_Container.GetContainerItemLink(b,s)) end end end
```

## Open (still needed, not blocking)

- [ ] `## Interface:` number → then create `Addons/DataToColor/DataToColor_Forever.toc`.
      Not created yet on purpose: no guessed Interface number.
- [ ] Client exe name → add to `Game/WoWProcess/WowProcess.cs` `defaultProcessNames`.
- [ ] Exe `FileVersion` (Explorer → Properties → Details). The mapping assumes `1.60.x`.
- [ ] "Interact With Target" present in Key Bindings?
- [ ] Improved Blizzard tier and point requirement (expected ~30).
- [ ] Focus unit and binding exist? Ammo slot and pet happiness exist?
- [ ] Spell, buff-icon and conjured-item IDs for the Mage profile (dumps above).
