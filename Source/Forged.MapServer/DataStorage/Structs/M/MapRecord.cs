// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.M;

public sealed class MapRecord
{
    public ushort AreaTableID;
    public Vector2 Corpse;
    public short CorpseMapID;
    public short CosmeticParentMapID;
    public string Directory;
    public byte ExpansionID;
    public uint[] Flags = new uint[3];
    public uint Id;
    public MapTypes InstanceType;
    public short LoadingScreenID;

    public string MapDescription0;

    // Horde
    public string MapDescription1;

    public LocalizedString MapName;

    // entrance coordinates in ghost mode  (in most cases = normal entrance)
    public byte MapType;

    // map_id of entrance map in ghost mode (continent always and in most cases = normal entrance)
    public byte MaxPlayers;

    public float MinimapIconScale;

    public int NavigationMaxDistance;

    public short ParentMapID;

    public string PvpLongDescription;

    // Alliance
    public string PvpShortDescription;
    public short TimeOfDayOverride;
    public byte TimeOffset;
    public int WdtFileDataID;
    public short WindSettingsID;

    public int ZmpFileDataID;

    // Helpers
    public Expansion Expansion()
    {
        return (Expansion)ExpansionID;
    }

    public bool GetEntrancePos(out uint mapid, out float x, out float y)
    {
        mapid = 0;
        x = 0;
        y = 0;

        if (CorpseMapID < 0)
            return false;

        mapid = (uint)CorpseMapID;
        x = Corpse.X;
        y = Corpse.Y;

        return true;
    }

    public MapFlags GetFlags()
    {
        return (MapFlags)Flags[0];
    }

    public MapFlags2 GetFlags2()
    {
        return (MapFlags2)Flags[1];
    }

    public bool Instanceable()
    {
        return InstanceType is MapTypes.Instance or MapTypes.Raid or MapTypes.Battleground or MapTypes.Arena or MapTypes.Scenario;
    }

    public bool IsBattleArena()
    {
        return InstanceType == MapTypes.Arena;
    }

    public bool IsBattleground()
    {
        return InstanceType == MapTypes.Battleground;
    }

    public bool IsBattlegroundOrArena()
    {
        return InstanceType is MapTypes.Battleground or MapTypes.Arena;
    }

    public bool IsContinent()
    {
        return Id switch
        {
            0    => true,
            1    => true,
            530  => true,
            571  => true,
            870  => true,
            1116 => true,
            1220 => true,
            1642 => true,
            1643 => true,
            2222 => true,
            2444 => true,
            _    => false
        };
    }

    public bool IsDungeon()
    {
        return InstanceType is MapTypes.Instance or MapTypes.Raid or MapTypes.Scenario && !IsGarrison();
    }

    public bool IsDynamicDifficultyMap()
    {
        return GetFlags().HasFlag(MapFlags.DynamicDifficulty);
    }

    public bool IsFlexLocking()
    {
        return GetFlags().HasFlag(MapFlags.FlexibleRaidLocking);
    }

    public bool IsGarrison()
    {
        return GetFlags().HasFlag(MapFlags.Garrison);
    }

    public bool IsNonRaidDungeon()
    {
        return InstanceType == MapTypes.Instance;
    }

    public bool IsRaid()
    {
        return InstanceType == MapTypes.Raid;
    }

    public bool IsScenario()
    {
        return InstanceType == MapTypes.Scenario;
    }

    public bool IsSplitByFaction()
    {
        return Id is 609 or 2175 or 2570;
    }

    public bool IsWorldMap()
    {
        return InstanceType == MapTypes.Common;
    }
}