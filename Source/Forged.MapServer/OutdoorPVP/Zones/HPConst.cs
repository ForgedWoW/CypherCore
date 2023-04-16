// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.OutdoorPVP.Zones;

internal struct HpConst
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

    public static uint[] LangCaptureA =
    {
        DefenseMessages.BROKEN_HILL_TAKEN_ALLIANCE, DefenseMessages.OVERLOOK_TAKEN_ALLIANCE, DefenseMessages.STADIUM_TAKEN_ALLIANCE
    };

    public static uint[] LangCaptureH =
    {
        DefenseMessages.BROKEN_HILL_TAKEN_HORDE, DefenseMessages.OVERLOOK_TAKEN_HORDE, DefenseMessages.STADIUM_TAKEN_HORDE
    };

    public static uint[] MapA =
    {
        2483, 2480, 2471
    };

    public static uint[] MapH =
    {
        2484, 2481, 2470
    };

    public static uint[] MapN =
    {
        2485, 2482, 0x9a8
    };

    public static uint[] TowerArtKitA =
    {
        65, 62, 67
    };

    public static uint[] TowerArtKitH =
    {
        64, 61, 68
    };

    public static uint[] TowerArtKitN =
    {
        66, 63, 69
    };
}