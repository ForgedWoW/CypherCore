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
        return InstanceType == MapTypes.Instance || InstanceType == MapTypes.Raid || InstanceType == MapTypes.Battleground || InstanceType == MapTypes.Arena || InstanceType == MapTypes.Scenario;
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
        return InstanceType == MapTypes.Battleground || InstanceType == MapTypes.Arena;
    }

    public bool IsContinent()
    {
        switch (Id)
        {
            case 0:
            case 1:
            case 530:
            case 571:
            case 870:
            case 1116:
            case 1220:
            case 1642:
            case 1643:
            case 2222:
            case 2444:
                return true;
            default:
                return false;
        }
    }

    public bool IsDungeon()
    {
        return (InstanceType == MapTypes.Instance || InstanceType == MapTypes.Raid || InstanceType == MapTypes.Scenario) && !IsGarrison();
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
        return Id == 609 || Id == 2175 || Id == 2570;
    }

    public bool IsWorldMap()
    {
        return InstanceType == MapTypes.Common;
    }
}