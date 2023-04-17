// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.BaseScripts;
using Forged.MapServer.Scripting.Interfaces.IMap;
using Framework.Constants;

namespace Scripts.DragonIsles.RubyLifePools;

internal struct DataTypes
{
    // Encounters
    public const uint MELIDRUSSA_CHILLWORN = 0;
    public const uint KOKIA_BLAZEHOOF = 1;
    public const uint KYRAKKA_AND_ERKHART_STORMVEIN = 2;
}

internal struct CreatureIds
{
    // Bosses
    public const uint MELIDRUSSA_CHILLWORN = 188252;
    public const uint KOKIA_BLAZEHOOF = 189232;
    public const uint KYRAKKA = 190484;
}

internal struct GameObjectIds
{
    public const uint FIRE_WALL = 377194;
}

[Script]
internal class InstanceRubyLifePools : InstanceMapScript, IInstanceMapGetInstanceScript
{
    public static ObjectData[] CreatureData =
    {
        new(CreatureIds.MELIDRUSSA_CHILLWORN, DataTypes.MELIDRUSSA_CHILLWORN), new(CreatureIds.KOKIA_BLAZEHOOF, DataTypes.KOKIA_BLAZEHOOF), new(CreatureIds.KYRAKKA, DataTypes.KYRAKKA_AND_ERKHART_STORMVEIN)
    };

    public static DoorData[] DoorData =
    {
        new(GameObjectIds.FIRE_WALL, DataTypes.KOKIA_BLAZEHOOF, DoorType.Passage)
    };

    public static DungeonEncounterData[] Encounters =
    {
        new(DataTypes.MELIDRUSSA_CHILLWORN, 2609), new(DataTypes.KOKIA_BLAZEHOOF, 2606), new(DataTypes.KYRAKKA_AND_ERKHART_STORMVEIN, 2623)
    };

    public InstanceRubyLifePools() : base(nameof(InstanceRubyLifePools), 2521) { }

    public InstanceScript GetInstanceScript(InstanceMap map)
    {
        return new InstanceRubyLifePoolsInstanceMapScript(map);
    }

    private class InstanceRubyLifePoolsInstanceMapScript : InstanceScript
    {
        public InstanceRubyLifePoolsInstanceMapScript(InstanceMap map) : base(map)
        {
            SetHeaders("RLP");
            SetBossNumber(3);
            LoadObjectData(CreatureData, null);
            LoadDoorData(DoorData);
            LoadDungeonEncounterData(Encounters);
        }
    }
}