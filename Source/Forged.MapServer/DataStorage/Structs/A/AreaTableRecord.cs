// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed record AreaTableRecord
{
    public ushort AmbienceID;
    public float AmbientMultiplier;
    public short AreaBit;
    public LocalizedString AreaName;
    public uint ContentTuningID;
    public ushort ContinentID;
    public byte FactionGroupMask;
    public uint[] Flags = new uint[2];
    public uint Id;
    public ushort IntroSound;
    public ushort[] LiquidTypeID = new ushort[4];
    public uint MountFlags;
    public ushort ParentAreaID;
    public short PvpCombatWorldStateID;
    public byte SoundProviderPref;
    public byte SoundProviderPrefUnderwater;
    public ushort UwAmbience;
    public uint UwIntroSound;
    public ushort UwZoneMusic;
    public byte WildBattlePetLevelMax;
    public byte WildBattlePetLevelMin;
    public byte WindSettingsID;
    public ushort ZoneMusic;
    public string ZoneName;

    public bool HasFlag(AreaFlags flag)
    {
        return Flags[0].HasAnyFlag((uint)flag);
    }

    public bool HasFlag2(AreaFlags2 flag)
    {
        return Flags[1].HasAnyFlag((uint)flag);
    }

    public bool IsFlyable()
    {
        if (HasFlag(AreaFlags.Outland))
            if (!HasFlag(AreaFlags.NoFlyZone))
                return true;

        return false;
    }

    public bool IsSanctuary()
    {
        return HasFlag(AreaFlags.Sanctuary);
    }
}