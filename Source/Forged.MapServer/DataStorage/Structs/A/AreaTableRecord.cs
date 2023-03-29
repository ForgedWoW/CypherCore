// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed class AreaTableRecord
{
    public uint Id;
    public string ZoneName;
    public LocalizedString AreaName;
    public ushort ContinentID;
    public ushort ParentAreaID;
    public short AreaBit;
    public byte SoundProviderPref;
    public byte SoundProviderPrefUnderwater;
    public ushort AmbienceID;
    public ushort UwAmbience;
    public ushort ZoneMusic;
    public ushort UwZoneMusic;
    public ushort IntroSound;
    public uint UwIntroSound;
    public byte FactionGroupMask;
    public float AmbientMultiplier;
    public byte MountFlags;
    public short PvpCombatWorldStateID;
    public byte WildBattlePetLevelMin;
    public byte WildBattlePetLevelMax;
    public byte WindSettingsID;
    public uint ContentTuningID;
    public uint[] Flags = new uint[2];
    public ushort[] LiquidTypeID = new ushort[4];

    public bool IsSanctuary()
    {
        return HasFlag(AreaFlags.Sanctuary);
    }

    public bool IsFlyable()
    {
        if (HasFlag(AreaFlags.Outland))
            if (!HasFlag(AreaFlags.NoFlyZone))
                return true;

        return false;
    }

    public bool HasFlag(AreaFlags flag)
    {
        return Flags[0].HasAnyFlag((uint)flag);
    }

    public bool HasFlag2(AreaFlags2 flag)
    {
        return Flags[1].HasAnyFlag((uint)flag);
    }
}