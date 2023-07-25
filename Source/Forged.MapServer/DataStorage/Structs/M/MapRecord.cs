using System.Numerics;
using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.M;

public sealed class MapRecord
{
    public uint Id;
    public string Directory;
    public LocalizedString MapName;
    public string MapDescription0; // Horde
    public string MapDescription1; // Alliance
    public string PvpShortDescription;
    public string PvpLongDescription;
    public Vector2 Corpse; // entrance coordinates in ghost mode  (in most cases = normal entrance)
    public byte MapType;
    public MapTypes InstanceType;
    public byte ExpansionID;
    public ushort AreaTableID;
    public short LoadingScreenID;
    public short TimeOfDayOverride;
    public short ParentMapID;
    public short CosmeticParentMapID;
    public byte TimeOffset;
    public float MinimapIconScale;
    public short CorpseMapID; // map_id of entrance map in ghost mode (continent always and in most cases = normal entrance)
    public byte MaxPlayers;
    public short WindSettingsID;
    public int ZmpFileDataID;
    public int WdtFileDataID;
    public int NavigationMaxDistance;
    public uint[] Flags = new uint[3];

    // Helpers
    public Expansion Expansion() { return (Expansion)ExpansionID; }

    public bool IsDungeon()
    {
        return (InstanceType == MapTypes.Instance || InstanceType == MapTypes.Raid || InstanceType == MapTypes.Scenario) && !IsGarrison();
    }
    public bool IsNonRaidDungeon() { return InstanceType == MapTypes.Instance; }
    public bool Instanceable() { return InstanceType == MapTypes.Instance || InstanceType == MapTypes.Raid || InstanceType == MapTypes.Battleground || InstanceType == MapTypes.Arena || InstanceType == MapTypes.Scenario; }
    public bool IsRaid() { return InstanceType == MapTypes.Raid; }
    public bool IsBattleground() { return InstanceType == MapTypes.Battleground; }
    public bool IsBattleArena() { return InstanceType == MapTypes.Arena; }
    public bool IsBattlegroundOrArena() { return InstanceType == MapTypes.Battleground || InstanceType == MapTypes.Arena; }
    public bool IsScenario() { return InstanceType == MapTypes.Scenario; }
    public bool IsWorldMap() { return InstanceType == MapTypes.Common; }

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

    public bool IsDynamicDifficultyMap() { return GetFlags().HasFlag(MapFlags.DynamicDifficulty); }
    public bool IsFlexLocking() { return GetFlags().HasFlag(MapFlags.FlexibleRaidLocking); }
    public bool IsGarrison() { return GetFlags().HasFlag(MapFlags.Garrison); }
    public bool IsSplitByFaction() { return Id == 609 || Id == 2175 || Id == 2570; }

    public MapFlags GetFlags() { return (MapFlags)Flags[0]; }
    public MapFlags2 GetFlags2() { return (MapFlags2)Flags[1]; }
}