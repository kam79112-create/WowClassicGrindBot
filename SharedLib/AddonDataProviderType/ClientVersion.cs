namespace SharedLib;

public enum ClientVersion
{
    None = 0,
    Retail = 1,
    SoM = 2,
    TBC = 5,
    Wrath = 11,
    Cata = 14,
    Forever = 18, // WoW: Forever 1.60.1 — WOW_PROJECT_ID measured in-client, no WOW_PROJECT_* constant exists
    Mop = 19,
    //
    Legacy_Vanilla = 91,
    Legacy_TBC = 92,
    Legacy_Wrath = 93,
    Legacy_Cata = 94,
    Legacy_Mop = 95,
}
