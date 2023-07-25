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
    public uint MountFlags;
    public short PvpCombatWorldStateID;
    public byte WildBattlePetLevelMin;
    public byte WildBattlePetLevelMax;
    public byte WindSettingsID;
    public uint ContentTuningID;
    public uint[] Flags = new uint[2];
    public ushort[] LiquidTypeID = new ushort[4];

    public AreaFlags GetFlags() { return (AreaFlags)Flags[0]; }
    public AreaFlags2 GetFlags2() { return (AreaFlags2)Flags[1]; }
    public AreaMountFlags GetMountFlags() { return (AreaMountFlags)MountFlags; }

    public bool HasFlag(AreaFlags flag)
    {
        return Flags[0].HasAnyFlag((uint)flag);
    }

    public bool HasFlag2(AreaFlags2 flag)
    {
        return Flags[1].HasAnyFlag((uint)flag);
    }

    public bool IsSanctuary()
    {
        return GetFlags().HasFlag(AreaFlags.Sanctuary);
    }
}