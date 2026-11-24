using System;

namespace SharedLib;

public sealed class StartupClientVersion
{
    public ClientVersion Version { get; }

    public string Path { get; }

    public StartupClientVersion(Version v)
    {
        (Version, Path) = v switch
        {
            // --- WoW: Forever (1.60.x, e.g. 1.60.1.15400) ---
            // Must precede the SoM arm: Minor 60 would otherwise match { Major: 1, Minor: >= 13 }.
            { Major: 1, Minor: 60 } => (ClientVersion.Forever, "forever"),

            // Exe file version missing or unparsable (WowProcess falls back to
            // new Version() = 0.0). Assume Forever, the only client this port
            // targets. At runtime the addon's WOW_PROJECT_ID (18) is authoritative;
            // this only picks the data folders before the addon is read.
            { Major: 0 } => (ClientVersion.Forever, "forever"),

            // --- Classic branch ---
            { Major: 1, Minor: >= 13 } => (ClientVersion.SoM, "som"), // Vanilla / SoM
            { Major: 2, Minor: >= 5 } => (ClientVersion.TBC, "tbc"),  // TBC Classic
            { Major: 3, Minor: >= 4 } => (ClientVersion.Wrath, "wrath"),
            { Major: 4, Minor: >= 4 } => (ClientVersion.Cata, "cata"),

            // --- Legacy branch ---
            { Major: 1, Minor: <= 12 } => (ClientVersion.Legacy_Vanilla, "legacy_vanilla"),
            { Major: 2, Minor: <= 4 } => (ClientVersion.Legacy_TBC, "legacy_tbc"),
            { Major: 3, Minor: <= 3 } => (ClientVersion.Legacy_Wrath, "legacy_wrath"),
            { Major: 4, Minor: <= 3 } => (ClientVersion.Legacy_Cata, "legacy_cata"),
            { Major: 5, Minor: <= 4 } => (ClientVersion.Legacy_Mop, "legacy_mop"),

            // --- Retail fallback ---
            { Major: >= 9 } => (ClientVersion.Retail, "retail"),

            _ => (ClientVersion.None, "unknown")
        };
    }

}
