// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.OutdoorPVP.Zones;

internal struct HPConst
{
    //  HP, citadel, ramparts, blood furnace, shattered halls, mag's lair
    public static uint[] BuffZones =
    {
        3483, 3563, 3562, 3713, 3714, 3836
    };

    public static uint[] CapturePointEventEnter =
    {
        11404, 11396, 11388
    };

    public static uint[] CapturePointEventLeave =
    {
        11403, 11395, 11387
    };

    public static uint[] CreditMarker =
    {
        19032, 19028, 19029
    };

    public static uint[] LangCapture_A =
    {
        DefenseMessages.BrokenHillTakenAlliance, DefenseMessages.OverlookTakenAlliance, DefenseMessages.StadiumTakenAlliance
    };

    public static uint[] LangCapture_H =
    {
        DefenseMessages.BrokenHillTakenHorde, DefenseMessages.OverlookTakenHorde, DefenseMessages.StadiumTakenHorde
    };

    public static uint[] Map_A =
    {
        2483, 2480, 2471
    };

    public static uint[] Map_H =
    {
        2484, 2481, 2470
    };

    public static uint[] Map_N =
    {
        2485, 2482, 0x9a8
    };

    public static uint[] TowerArtKit_A =
    {
        65, 62, 67
    };

    public static uint[] TowerArtKit_H =
    {
        64, 61, 68
    };

    public static uint[] TowerArtKit_N =
    {
        66, 63, 69
    };
}