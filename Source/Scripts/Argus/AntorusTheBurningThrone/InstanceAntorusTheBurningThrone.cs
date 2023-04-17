// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.BaseScripts;
using Forged.MapServer.Scripting.Interfaces.IMap;
using Framework.Constants;

namespace Scripts.Argus.AntorusTheBurningThrone;

internal struct DataTypes
{
    // Encounters
    public const uint GAROTHI_WORLDBREAKER = 0;
    public const uint FELHOUNDS_OF_SAGERAS = 1;
    public const uint ANTORAN_HIGH_COMMAND = 2;
    public const uint PORTAL_KEEPER_HASABEL = 3;
    public const uint EONAR_THE_LIFE_BINDER = 4;
    public const uint IMONAR_THE_SOULHUNTER = 5;
    public const uint KINGAROTH = 6;
    public const uint VARIMATHRAS = 7;
    public const uint THE_COVEN_OF_SHIVARRA = 8;
    public const uint AGGRAMAR = 9;
    public const uint ARGUS_THE_UNMAKER = 10;

    // Encounter related data
    public const uint DECIMATOR = 11;
    public const uint ANNIHILATOR = 12;
}

internal struct BossIds
{
    // Bosses
    public const uint GAROTHI_WORLDBREAKER = 122450;
    public const uint ENCOUNTER_COUNT = 10;
}

internal struct CreatureIds
{
    // Garothi Worldbreaker
    public const uint DECIMATOR = 122773;
    public const uint ANNIHILATOR = 122778;
    public const uint ANNIHILATION = 122818;
    public const uint GAROTHI_WORLDBREAKER = 124167;
}

internal struct GameObjectIds
{
    public const uint COLLISION = 277365;
    public const uint ROCK = 278488;
}

[Script]
internal class InstanceAntorusTheBurningThrone : InstanceMapScript, IInstanceMapGetInstanceScript
{
    private static readonly ObjectData[] CreatureData =
    {
        new(BossIds.GAROTHI_WORLDBREAKER, DataTypes.GAROTHI_WORLDBREAKER), new(CreatureIds.DECIMATOR, DataTypes.DECIMATOR), new(CreatureIds.ANNIHILATOR, DataTypes.ANNIHILATOR)
    };

    private static readonly DoorData[] DoorData =
    {
        new(GameObjectIds.COLLISION, DataTypes.GAROTHI_WORLDBREAKER, DoorType.Passage), new(GameObjectIds.ROCK, DataTypes.GAROTHI_WORLDBREAKER, DoorType.Passage)
    };

    private static readonly DungeonEncounterData[] Encounters =
    {
        new(DataTypes.GAROTHI_WORLDBREAKER, 2076), new(DataTypes.FELHOUNDS_OF_SAGERAS, 2074), new(DataTypes.ANTORAN_HIGH_COMMAND, 2070), new(DataTypes.PORTAL_KEEPER_HASABEL, 2064), new(DataTypes.EONAR_THE_LIFE_BINDER, 2075), new(DataTypes.IMONAR_THE_SOULHUNTER, 2082), new(DataTypes.KINGAROTH, 2088), new(DataTypes.VARIMATHRAS, 2069), new(DataTypes.THE_COVEN_OF_SHIVARRA, 2073), new(DataTypes.AGGRAMAR, 2063), new(DataTypes.ARGUS_THE_UNMAKER, 2092)
    };

    public InstanceAntorusTheBurningThrone() : base("instance_antorus_the_burning_throne", 1712) { }

    public InstanceScript GetInstanceScript(InstanceMap map)
    {
        return new InstanceAntorusTheBurningThroneInstanceMapScript(map);
    }

    private class InstanceAntorusTheBurningThroneInstanceMapScript : InstanceScript
    {
        public InstanceAntorusTheBurningThroneInstanceMapScript(InstanceMap map) : base(map)
        {
            SetHeaders("ABT");
            SetBossNumber(BossIds.ENCOUNTER_COUNT);
            LoadObjectData(CreatureData, null);
            LoadDoorData(DoorData);
            LoadDungeonEncounterData(Encounters);
        }

        public override void OnCreatureCreate(Creature creature)
        {
            base.OnCreatureCreate(creature);

            switch (creature.Entry)
            {
                case CreatureIds.ANNIHILATION:
                    var garothi = GetCreature(DataTypes.GAROTHI_WORLDBREAKER);

                    if (garothi)
                        garothi.AI.JustSummoned(creature);

                    break;
            }
        }
    }
}