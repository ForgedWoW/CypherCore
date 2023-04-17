// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.BaseScripts;
using Forged.MapServer.Scripting.Interfaces.IMap;
using Framework.Constants;

namespace Scripts.EasternKingdoms.MagistersTerrace;

internal struct DataTypes
{
    // Encounter states
    public const uint SELIN_FIREHEART = 0;
    public const uint VEXALLUS = 1;
    public const uint PRIESTESS_DELRISSA = 2;
    public const uint KAELTHAS_SUNSTRIDER = 3;

    // Encounter related
    public const uint KAELTHAS_INTRO = 4;
    public const uint DELRISSA_DEATH_COUNT = 5;

    // Additional data
    public const uint KALECGOS = 6;
    public const uint ESCAPE_ORB = 7;
}

internal struct CreatureIds
{
    // Bosses
    public const uint KAELTHAS_SUNSTRIDER = 24664;
    public const uint SELIN_FIREHEART = 24723;
    public const uint VEXALLUS = 24744;
    public const uint PRIESTESS_DELRISSA = 24560;

    // Encounter related
    // Kael'thas Sunstrider
    public const uint ARCANE_SPHERE = 24708;
    public const uint FLAME_STRIKE = 24666;
    public const uint PHOENIX = 24674;
    public const uint PHOENIX_EGG = 24675;

    // Selin Fireheart
    public const uint FEL_CRYSTAL = 24722;

    // Event related
    public const uint KALECGOS = 24844;
    public const uint HUMAN_KALECGOS = 24848;
    public const uint COILSKAR_WITCH = 24696;
    public const uint SUNBLADE_WARLOCK = 24686;
    public const uint SUNBLADE_MAGE_GUARD = 24683;
    public const uint SISTER_OF_TORMENT = 24697;
    public const uint ETHEREUM_SMUGGLER = 24698;
    public const uint SUNBLADE_BLOOD_KNIGHT = 24684;
}

internal struct GameObjectIds
{
    public const uint ASSEMBLY_CHAMBER_DOOR = 188065;
    public const uint SUNWELL_RAID_GATE2 = 187979;
    public const uint SUNWELL_RAID_GATE4 = 187770;
    public const uint SUNWELL_RAID_GATE5 = 187896;
    public const uint ASYLUM_DOOR = 188064;
    public const uint ESCAPE_ORB = 188173;
}

internal struct MiscConst
{
    public const uint EVENT_SPAWN_KALECGOS = 16547;

    public const uint SAY_KALECGOS_SPAWN = 0;

    public const uint PATH_KALECGOS_FLIGHT = 248440;

    public static ObjectData[] CreatureData =
    {
        new(CreatureIds.SELIN_FIREHEART, DataTypes.SELIN_FIREHEART), new(CreatureIds.VEXALLUS, DataTypes.VEXALLUS), new(CreatureIds.PRIESTESS_DELRISSA, DataTypes.PRIESTESS_DELRISSA), new(CreatureIds.KAELTHAS_SUNSTRIDER, DataTypes.KAELTHAS_SUNSTRIDER), new(CreatureIds.KALECGOS, DataTypes.KALECGOS), new(CreatureIds.HUMAN_KALECGOS, DataTypes.KALECGOS)
    };

    public static ObjectData[] GameObjectData =
    {
        new(GameObjectIds.ESCAPE_ORB, DataTypes.ESCAPE_ORB)
    };

    public static DoorData[] DoorData =
    {
        new(GameObjectIds.SUNWELL_RAID_GATE2, DataTypes.SELIN_FIREHEART, DoorType.Passage), new(GameObjectIds.ASSEMBLY_CHAMBER_DOOR, DataTypes.SELIN_FIREHEART, DoorType.Room), new(GameObjectIds.SUNWELL_RAID_GATE5, DataTypes.VEXALLUS, DoorType.Passage), new(GameObjectIds.SUNWELL_RAID_GATE4, DataTypes.PRIESTESS_DELRISSA, DoorType.Passage), new(GameObjectIds.ASYLUM_DOOR, DataTypes.KAELTHAS_SUNSTRIDER, DoorType.Room)
    };

    public static Position KalecgosSpawnPos = new(164.3747f, -397.1197f, 2.151798f, 1.66219f);
    public static Position KaelthasTrashGroupDistanceComparisonPos = new(150.0f, 141.0f, -14.4f);
}

[Script]
internal class InstanceMagistersTerrace : InstanceMapScript, IInstanceMapGetInstanceScript
{
    private static readonly DungeonEncounterData[] Encounters =
    {
        new(DataTypes.SELIN_FIREHEART, 1897), new(DataTypes.VEXALLUS, 1898), new(DataTypes.PRIESTESS_DELRISSA, 1895), new(DataTypes.KAELTHAS_SUNSTRIDER, 1894)
    };

    public InstanceMagistersTerrace() : base(nameof(InstanceMagistersTerrace), 585) { }

    public InstanceScript GetInstanceScript(InstanceMap map)
    {
        return new InstanceMagistersTerraceInstanceMapScript(map);
    }

    private class InstanceMagistersTerraceInstanceMapScript : InstanceScript
    {
        private readonly List<ObjectGuid> _kaelthasPreTrashGuiDs = new();
        private byte _delrissaDeathCount;

        public InstanceMagistersTerraceInstanceMapScript(InstanceMap map) : base(map)
        {
            SetHeaders("MT");
            SetBossNumber(4);
            LoadObjectData(MiscConst.CreatureData, MiscConst.GameObjectData);
            LoadDoorData(MiscConst.DoorData);
            LoadDungeonEncounterData(Encounters);
        }

        public override uint GetData(uint type)
        {
            switch (type)
            {
                case DataTypes.DELRISSA_DEATH_COUNT:
                    return _delrissaDeathCount;
            }

            return 0;
        }

        public override void SetData(uint type, uint data)
        {
            switch (type)
            {
                case DataTypes.DELRISSA_DEATH_COUNT:
                    if (data == (uint)EncounterState.Special)
                        _delrissaDeathCount++;
                    else
                        _delrissaDeathCount = 0;

                    break;
            }
        }

        public override void OnCreatureCreate(Creature creature)
        {
            base.OnCreatureCreate(creature);

            switch (creature.Entry)
            {
                case CreatureIds.COILSKAR_WITCH:
                case CreatureIds.SUNBLADE_WARLOCK:
                case CreatureIds.SUNBLADE_MAGE_GUARD:
                case CreatureIds.SISTER_OF_TORMENT:
                case CreatureIds.ETHEREUM_SMUGGLER:
                case CreatureIds.SUNBLADE_BLOOD_KNIGHT:
                    if (creature.GetDistance(MiscConst.KaelthasTrashGroupDistanceComparisonPos) < 10.0f)
                        _kaelthasPreTrashGuiDs.Add(creature.GUID);

                    break;
            }
        }

        public override void OnUnitDeath(Unit unit)
        {
            if (!unit.IsCreature)
                return;

            switch (unit.Entry)
            {
                case CreatureIds.COILSKAR_WITCH:
                case CreatureIds.SUNBLADE_WARLOCK:
                case CreatureIds.SUNBLADE_MAGE_GUARD:
                case CreatureIds.SISTER_OF_TORMENT:
                case CreatureIds.ETHEREUM_SMUGGLER:
                case CreatureIds.SUNBLADE_BLOOD_KNIGHT:
                    if (_kaelthasPreTrashGuiDs.Contains(unit.GUID))
                    {
                        _kaelthasPreTrashGuiDs.Remove(unit.GUID);

                        if (_kaelthasPreTrashGuiDs.Count == 0)
                        {
                            var kaelthas = GetCreature(DataTypes.KAELTHAS_SUNSTRIDER);

                            if (kaelthas)
                                kaelthas.AI.SetData(DataTypes.KAELTHAS_INTRO, (uint)EncounterState.InProgress);
                        }
                    }

                    break;
            }
        }

        public override void OnGameObjectCreate(GameObject go)
        {
            base.OnGameObjectCreate(go);

            switch (go.Entry)
            {
                case GameObjectIds.ESCAPE_ORB:
                    if (GetBossState(DataTypes.KAELTHAS_SUNSTRIDER) == EncounterState.Done)
                        go.RemoveFlag(GameObjectFlags.NotSelectable);

                    break;
            }
        }

        public override void ProcessEvent(WorldObject obj, uint eventId, WorldObject invoker)
        {
            if (eventId == MiscConst.EVENT_SPAWN_KALECGOS)
                if (!GetCreature(DataTypes.KALECGOS) &&
                    _events.Empty())
                    _events.ScheduleEvent(MiscConst.EVENT_SPAWN_KALECGOS, TimeSpan.FromMinutes(1));
        }

        public override void Update(uint diff)
        {
            _events.Update(diff);

            if (_events.ExecuteEvent() == MiscConst.EVENT_SPAWN_KALECGOS)
            {
                Creature kalecgos = Instance.SummonCreature(CreatureIds.KALECGOS, MiscConst.KalecgosSpawnPos);

                if (kalecgos)
                {
                    kalecgos.MotionMaster.MovePath(MiscConst.PATH_KALECGOS_FLIGHT, false);
                    kalecgos.AI.Talk(MiscConst.SAY_KALECGOS_SPAWN);
                }
            }
        }

        public override bool SetBossState(uint type, EncounterState state)
        {
            if (!base.SetBossState(type, state))
                return false;

            switch (type)
            {
                case DataTypes.PRIESTESS_DELRISSA:
                    if (state == EncounterState.InProgress)
                        _delrissaDeathCount = 0;

                    break;
                case DataTypes.KAELTHAS_SUNSTRIDER:
                    if (state == EncounterState.Done)
                    {
                        var orb = GetGameObject(DataTypes.ESCAPE_ORB);

                        orb?.RemoveFlag(GameObjectFlags.NotSelectable);
                    }

                    break;
            }

            return true;
        }
    }
}