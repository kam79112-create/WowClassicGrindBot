# WoW: Forever port notes

- **Modern client.** WoW: Forever is a new official Blizzard product (Battle.net,
  worldofwarcraft.blizzard.com/forever), version 1.60.1, client build **15400**
  (`Debug: 1.60.1.15400 (core 172)` on screen; 70959 is the CDN summary build, not the
  client build), on a modern engine.
  It is **not** vanilla 1.12.
- **No legacy paths.** Nothing routes Forever into `DataToColor.IsLegacy()`. No
  Interact With Target workaround, no secure-button workaround, no `Legacy/*.lua` loads.
- **`WOW_PROJECT_ID` = 18.** The client defines no `WOW_PROJECT_*` constant for it, so
  the code uses the literal 18.
- **Interface 16001.**
- **Locale ruRU** until the client is switched to **enUS**.

Branch: `forever-port`. Each fact below is tagged *measured* (dumped in the client),
*derived* (computed from measured facts) or *user* (stated by the user, not dumped).
Anything else is under *Open* and is not assumed by the code.

## Facts

| Fact | Value | Tag |
|---|---|---|
| `GetBuildInfo()` version | `1.60.1` | measured |
| Client build | `15400` (`Debug: 1.60.1.15400 (core 172)`). 70959 = CDN summary build, **not** used | user (on-screen) |
| `GetBuildInfo()` return count | **1** (version string only, no TOC number) | user |
| `GetSpellInfo` global | **absent**, lives in `C_Spell.GetSpellInfo` | user |
| TOC suffixes auto-loaded | `_Mainline _Classic _Vanilla _TBC _Wrath _Cata _Mists` only; `_Forever` ignored | user |
| `WOW_PROJECT_ID` | `18` (no collision with 1, 2, 5, 11, 14, 19, 91–95) | measured |
| `WOW_PROJECT_FOREVER` | `nil` | measured |
| `GetLocale()` | `ruRU` | measured |
| `## Interface:` | `16001` = 1·10000 + 60·100 + 1 | **derived**. Confirm: `/dump select(4, GetBuildInfo())` |
| Interact With Target | present, default Alt-Home | user (modern-client reasoning, not checked in Key Bindings) |
| Mage @22 talents | Frost 6 (Improved Frostbolt 5/5 + 1 in tier 2), Arcane 7, 0 unspent | user |
| Talent tree | not vanilla: more tiers, different talents; Improved Blizzard ~30 | user |

## Load-critical fixes (addon died at load without these)

1. **`buildVersion` never nil.** `select(4, GetBuildInfo())` is nil on Forever and
   `buildVersion < 20000` then raised at load. Now: 4th value -> parsed from the version
   string (`1.60.1` -> 16001) -> assume Forever (16001). Exposed as `DataToColor.BuildVersion`.
2. **`GetSpellInfo` shim.** `Versions.lua` probes `GetSpellInfo(6603)` at load. When the
   global is missing and `C_Spell.GetSpellInfo` exists, it is rebuilt in the classic
   short layout (`name, rank, icon, castTime, minRange, maxRange, spellID, originalIcon`)
   and published as `DataToColor.GetSpellInfo`; `EventHandlers`, `Query` and `Trainer` bind
   that instead of the global. Feature-detected: clients with the global are untouched.
3. **API report.** On Forever the addon prints once at load which spell/aura/container
   globals are missing. Paste that line here; it is the list of the next shims needed.

Simulated with a Lua runtime (Forever mock: 1-value `GetBuildInfo`, no `GetSpellInfo`,
`C_Spell` present): `Versions.lua` loads to the end, BuildVersion 16001, ClientVersion 18,
IsLegacy false, `GetSpellInfo(6603)` -> "Attack". Legacy 4.3.4 mock: BuildVersion 40300,
ClientVersion 94, unchanged. A mock is not the client: confirm in-game.

## TOC

The plain `DataToColor.toc` in the repo is the legacy 4.3.4 / 5.4.8 TOC (Ace3 v5,
`Legacy/*.lua`, cTimerBackport). Overwriting it in the repo would break those clients.
Instead `AddonConfigurator.ApplyClientToc()` copies `DataToColor_Forever.toc` over
`DataToColor.toc` **at install time** when the running exe is Forever. The suffixed
file in the repo is the source and documentation; the client never reads it directly.

## Consequences of ID 18 / Interface 16001 in the addon

ID 18 makes `IsClassic()`, `IsClassic_*()`, `IsRetail()` and `IsClassicPreCata()` all
**false**, and `IsClassicEra()` **true**. Because 16001 < 20000, `IsVanilla()` is **true**.
Every branch keyed on those was audited:

| Behaviour | Before (ID 18 fell through) | Now | Why |
|---|---|---|---|
| Focus unit (`Constants.lua`) | `focus` | `party1` via `IsForever()` | `SetupDefaultBindings` already binds `TARGETPARTYMEMBER1` (IsVanilla), so the old state was **inconsistent**. Now addon unit + binding + C# (`ClassConfiguration`) all agree. |
| CLEU spellId=0 fallback (`SoM_OnCast*`) | off | on via `IsForever()` | Harmless if the combat log carries IDs: it only fills in when the ID is 0. |
| Ammo slot | always "has ammo" | `HasAmmoSlot()`: **feature-detected** (`pcall(GetInventorySlotInfo,"AmmoSlot")`) | Unmeasured, so detected rather than assumed |
| Pet happiness | always happy | `HasPetHappiness()`: **feature-detected** (`GetPetHappiness ~= nil`) | Same |
| LibClassicCasterino | off | off | The lib exits on anything except `WOW_PROJECT_CLASSIC`. Modern engine: native `UnitCastingInfo` assumed. Dropped from the Forever TOC's OptionalDeps. |
| `Query.lua` ammoSlot lookup | hard call | `pcall` | A client without the slot would error at load time |

If `/dump BINDING_NAME_TARGETFOCUS` is non-nil on Forever, flip the focus choice in
**three** places together: `Constants.lua`, `SetupDefaultBindings.lua` (IsVanilla
branches) and `Core/ClassConfig/ClassConfiguration.cs`.

## C# / data changes

| File | Change |
|---|---|
| `SharedLib/AddonDataProviderType/ClientVersion.cs` | `Forever = 18` |
| `SharedLib/StartupConfig/StartupClientVersion.cs` | exe `FileVersion {1, 60}` → `(Forever, "forever")`, **before** the SoM arm (otherwise 1.60 matched `{1, >=13}` = Classic Era). Unparsable/missing version (`0.0`) → assume Forever. Only picks data folders at startup; at runtime the addon's `WOW_PROJECT_ID` 18 is authoritative |
| `DataConfig/DataConfig.cs` | `ClientEra("forever") = "precata"` (navmesh, tiles, road, area_grid). DBC scope = **`Json/dbc/forever`**. `ContentExp()`: area, experience, subzones, History, npcspawnlocations and mailboxlocations read `som` (interim) |
| `Frontend/wwwroot/script/leaflet-watch.js` | `clientEra('forever') = 'precata'` (JS mirror) |
| `Core/ClassConfig/ClassConfiguration.cs` | `SoM or Forever` → `TARGETPARTYMEMBER1` |
| `Core/WowheadAPI/WApi.cs` | explicit `Forever` arm → retail Wowhead (no known Forever site) |
| `Utilities/ReadDBC_CSV/Program.cs` | `Forever → "1.60.1.15400"`; outputs to `Json/dbc/forever` |
| `Addons/DataToColor/DataToColor_Forever.toc` | Interface 16001, Classic load order, no Legacy loads, Notes "(Forever)" |
| `Addons/DataToColor/*.toc` | 1.13.3 → 1.13.4 |
| `Core/Configurator/AddonConfigurator.cs` | installs to `<process dir>\Interface\AddOns`; on Forever swaps `DataToColor_Forever.toc` in as `DataToColor.toc` (see TOC) |
| `Game/WoWProcess/WowProcess.cs` | **unchanged**: `Wow` is already listed, but the exe name is not confirmed from the install dir |

`PathingAPI` default exp (`som`), `Stance.cs` (Wrath-only branch) and
`FrameConfigurator` (`!= None`) need no Forever case.

### Not runnable yet

- **`Json/dbc/forever` does not exist.** The bot will not start on Forever until you run
  `ReadDBC_CSV -v forever` on the PC. From the cloud container wago.tools was
  unreachable, so it is unknown whether wago carries build 15400. If it does not, the
  DBC data has to come from the client's own CASC archives.
- **Not compiled.** The container has no .NET SDK and cannot reach Microsoft's download
  host. Run `dotnet build -c Release` on Windows. Lua was syntax-checked (parser only).

## Verify the TOC is the one that loads

After install, `Interface\AddOns\<Title>\<Title>.toc` must start with `## Interface: 16001`.
In game:
```
/dump C_AddOns.GetAddOnMetadata("DataToColor", "Notes")
```
Expected: `Displays data as colors (Forever)`. (Use your addon Title if it was renamed.)

## Localization

ruRU client. Profiles and `NpcNameFinder` match English names (spell and buff names,
nameplates against English DBC). Switch the client to **enUS**. Where a name is
unavoidable, use `Spell:<IntVariable>` (spellbook ID) and `Buff_<name>: <iconId>` (icon
ID, not spell ID).

## Mage_22_Frost_Forever.json

Spell IDs from the player's own action bar dump (user): Frostbolt 7322, Frost Nova 122,
Fire Blast 2138, Ice Armor 7301, Conjure Water 5506, Conjure Food 990, Shoot (wand) 5019.
Each action is gated by `Spell:SPELL_*`. `Name` is only a label (the key is pressed);
buff checks such as `!Ice Armor` resolve by **icon ID** in `Storage.lua`, not by text, so
they work on ruRU too, provided Forever uses the same icon IDs (unverified).

Single target only. No Blizzard, Arcane Explosion or Cone of Cold. Frost Nova only when
no adds are visible. Path: `23_Duskwood.json`.
Keys: 1 Frostbolt, 2 Frost Nova, 3 Fire Blast, 4 Drink, 5 Food, 6 Shoot, N2 Ice Armor,
N0 Conjure Water, N9 Conjure Food.

Spellbook (try the first line; if it errors, use the second):
```
/run for i=1,200 do local n=GetSpellBookItemName(i,"spell") if not n then break end print(i,n,select(2,GetSpellBookItemInfo(i,"spell"))) end
/run local B,P=C_SpellBook,Enum.SpellBookSpellBank.Player for i=1,200 do local n=B.GetSpellBookItemName(i,P) if not n then break end print(i,n,B.GetSpellBookItemInfo(i,P).spellID) end
```
Buff icons:
```
/run for i=1,40 do local a=C_UnitAuras.GetBuffDataByIndex("player",i) if not a then break end print(a.name,a.icon,a.spellId) end
```
Bag item IDs:
```
/run for b=0,4 do for s=1,C_Container.GetContainerNumSlots(b) do local id=C_Container.GetContainerItemID(b,s) if id then print(b,s,id,C_Container.GetContainerItemLink(b,s)) end end end
```

## Open

- [ ] Exe name: `Get-ChildItem "<client dir>" -Filter *.exe`, and its FileVersion (expected 1.60.x)
- [ ] Paste the `DataToColor: Forever build ... missing globals: ...` line from chat
- [ ] Which TOC loads (Notes check above)
- [ ] `/dump BINDING_NAME_TARGETFOCUS, GetPetHappiness, GetInventorySlotInfo("AmmoSlot")`
- [ ] Generate `Json/dbc/forever` (ReadDBC_CSV or CASC)
- [ ] Geometry: hash-compare Forever ADTs vs 1.15 before trusting `precata`
- [ ] Confirm armor buff icon IDs match `Storage.lua` (`!Ice Armor` check)
