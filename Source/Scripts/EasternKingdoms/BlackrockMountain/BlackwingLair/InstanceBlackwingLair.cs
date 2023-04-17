// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.BaseScripts;
using Forged.MapServer.Scripting.Interfaces.IMap;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackwingLair;

internal struct DataTypes
{
    // Encounter States/Boss GUIDs
    public const uint RAZORGORE_THE_UNTAMED = 0;
    public const uint VAELASTRAZ_THE_CORRUPT = 1;
    public const uint BROODLORD_LASHLAYER = 2;
    public const uint FIREMAW = 3;
    public const uint EBONROC = 4;
    public const uint FLAMEGOR = 5;
    public const uint CHROMAGGUS = 6;
    public const uint NEFARIAN = 7;

    // Additional Data
    public const uint LORD_VICTOR_NEFARIUS = 8;

    // Doors
    public const uint GO_CHROMAGGUS_DOOR = 9;
}

internal struct BwlCreatureIds
{
    public const uint RAZORGORE = 12435;
    public const uint BLACKWING_DRAGON = 12422;
    public const uint BLACKWING_TASKMASTER = 12458;
    public const uint BLACKWING_LEGIONAIRE = 12416;
    public const uint BLACKWING_WARLOCK = 12459;
    public const uint VAELASTRAZ = 13020;
    public const uint BROODLORD = 12017;
    public const uint FIREMAW = 11983;
    public const uint EBONROC = 14601;
    public const uint FLAMEGOR = 11981;
    public const uint CHROMAGGUS = 14020;
    public const uint VICTOR_NEFARIUS = 10162;
    public const uint NEFARIAN = 11583;
}

internal struct BwlGameObjectIds
{
    public const uint BLACK_DRAGON_EGG = 177807;
    public const uint PORTCULLIS_RAZORGORE = 176965;
    public const uint PORTCULLIS_VAELASTRASZ = 179364;
    public const uint PORTCULLIS_BROODLORD = 179365;
    public const uint PORTCULLIS_THREEDRAGONS = 179115;
    public const uint PORTCULLIS_CHROMAGGUS = 179117; //Door after you kill him, not the one for his room
    public const uint CHROMAGGUS_LEVER = 179148;
    public const uint CHROMAGGUS_DOOR = 179116;
    public const uint PORTCULLIS_NEFARIAN = 176966;
    public const uint SUPPRESSION_DEVICE = 179784;
}

internal struct EventIds
{
    public const uint RAZOR_SPAWN = 1;
    public const uint RAZOR_PHASE_TWO = 2;
    public const uint RESPAWN_NEFARIUS = 3;
}

internal struct BwlMisc
{
    public const uint ENCOUNTER_COUNT = 8;

    // Razorgore Egg Event
    public const int ACTION_PHASE_TWO = 1;
    public const uint DATA_EGG_EVENT = 2;

    public static DoorData[] DoorData =
    {
        new(BwlGameObjectIds.PORTCULLIS_RAZORGORE, DataTypes.RAZORGORE_THE_UNTAMED, DoorType.Passage), new(BwlGameObjectIds.PORTCULLIS_VAELASTRASZ, DataTypes.VAELASTRAZ_THE_CORRUPT, DoorType.Passage), new(BwlGameObjectIds.PORTCULLIS_BROODLORD, DataTypes.BROODLORD_LASHLAYER, DoorType.Passage), new(BwlGameObjectIds.PORTCULLIS_THREEDRAGONS, DataTypes.FIREMAW, DoorType.Passage), new(BwlGameObjectIds.PORTCULLIS_THREEDRAGONS, DataTypes.EBONROC, DoorType.Passage), new(BwlGameObjectIds.PORTCULLIS_THREEDRAGONS, DataTypes.FLAMEGOR, DoorType.Passage), new(BwlGameObjectIds.PORTCULLIS_CHROMAGGUS, DataTypes.CHROMAGGUS, DoorType.Passage), new(BwlGameObjectIds.PORTCULLIS_NEFARIAN, DataTypes.NEFARIAN, DoorType.Room)
    };

    public static ObjectData[] CreatureData =
    {
        new(BwlCreatureIds.RAZORGORE, DataTypes.RAZORGORE_THE_UNTAMED), new(BwlCreatureIds.VAELASTRAZ, DataTypes.VAELASTRAZ_THE_CORRUPT), new(BwlCreatureIds.BROODLORD, DataTypes.BROODLORD_LASHLAYER), new(BwlCreatureIds.FIREMAW, DataTypes.FIREMAW), new(BwlCreatureIds.EBONROC, DataTypes.EBONROC), new(BwlCreatureIds.FLAMEGOR, DataTypes.FLAMEGOR), new(BwlCreatureIds.CHROMAGGUS, DataTypes.CHROMAGGUS), new(BwlCreatureIds.NEFARIAN, DataTypes.NEFARIAN), new(BwlCreatureIds.VICTOR_NEFARIUS, DataTypes.LORD_VICTOR_NEFARIUS)
    };

    public static ObjectData[] GameObjectData =
    {
        new(BwlGameObjectIds.CHROMAGGUS_DOOR, DataTypes.GO_CHROMAGGUS_DOOR)
    };

    public static Position[] SummonPosition =
    {
        new(-7661.207520f, -1043.268188f, 407.199554f, 6.280452f), new(-7644.145020f, -1065.628052f, 407.204956f, 0.501492f), new(-7624.260742f, -1095.196899f, 407.205017f, 0.544694f), new(-7608.501953f, -1116.077271f, 407.199921f, 0.816443f), new(-7531.841797f, -1063.765381f, 407.199615f, 2.874187f), new(-7547.319336f, -1040.971924f, 407.205078f, 3.789175f), new(-7568.547852f, -1013.112488f, 407.204926f, 3.773467f), new(-7584.175781f, -989.6691289f, 407.199585f, 4.527447f)
    };

    public static uint[] Entry =
    {
        12422, 12458, 12416, 12420, 12459
    };
}

[Script]
internal class InstanceBlackwingLair : InstanceMapScript, IInstanceMapGetInstanceScript
{
    private static readonly DungeonEncounterData[] Encounters =
    {
        new(DataTypes.RAZORGORE_THE_UNTAMED, 610), new(DataTypes.VAELASTRAZ_THE_CORRUPT, 611), new(DataTypes.BROODLORD_LASHLAYER, 612), new(DataTypes.FIREMAW, 613), new(DataTypes.EBONROC, 614), new(DataTypes.FLAMEGOR, 615), new(DataTypes.CHROMAGGUS, 616), new(DataTypes.NEFARIAN, 617)
    };

    public InstanceBlackwingLair() : base(nameof(InstanceBlackwingLair), 469) { }

    public InstanceScript GetInstanceScript(InstanceMap map)
    {
        return new InstanceBlackwingLairInstanceMapScript(map);
    }

    private class InstanceBlackwingLairInstanceMapScript : InstanceScript
    {
        private readonly List<ObjectGuid> _eggList = new();

        // Razorgore
        private byte _eggCount;
        private uint _eggEvent;

        public InstanceBlackwingLairInstanceMapScript(InstanceMap map) : base(map)
        {
            SetHeaders("BWL");
            SetBossNumber(BwlMisc.ENCOUNTER_COUNT);
            LoadDoorData(BwlMisc.DoorData);
            LoadObjectData(BwlMisc.CreatureData, BwlMisc.GameObjectData);
            LoadDungeonEncounterData(Encounters);

            // Razorgore
            _eggCount = 0;
            _eggEvent = 0;
        }

        public override void OnCreatureCreate(Creature creature)
        {
            base.OnCreatureCreate(creature);

            switch (creature.Entry)
            {
                case BwlCreatureIds.BLACKWING_DRAGON:
                case BwlCreatureIds.BLACKWING_TASKMASTER:
                case BwlCreatureIds.BLACKWING_LEGIONAIRE:
                case BwlCreatureIds.BLACKWING_WARLOCK:
                    var razor = GetCreature(DataTypes.RAZORGORE_THE_UNTAMED);

                    if (razor != null)
                    {
                        var razorAI = razor.AI;

                        razorAI?.JustSummoned(creature);
                    }

                    break;
            }
        }

        public override uint GetGameObjectEntry(ulong spawnId, uint entry)
        {
            if (entry == BwlGameObjectIds.BLACK_DRAGON_EGG &&
                GetBossState(DataTypes.FIREMAW) == EncounterState.Done)
                return 0;

            return entry;
        }

        public override void OnGameObjectCreate(GameObject go)
        {
            base.OnGameObjectCreate(go);

            switch (go.Entry)
            {
                case BwlGameObjectIds.BLACK_DRAGON_EGG:
                    _eggList.Add(go.GUID);

                    break;
            }
        }

        public override void OnGameObjectRemove(GameObject go)
        {
            base.OnGameObjectRemove(go);

            if (go.Entry == BwlGameObjectIds.BLACK_DRAGON_EGG)
                _eggList.Remove(go.GUID);
        }

        public override bool CheckRequiredBosses(uint bossId, Player player = null)
        {
            if (_SkipCheckRequiredBosses(player))
                return true;

            switch (bossId)
            {
                case DataTypes.BROODLORD_LASHLAYER:
                    if (GetBossState(DataTypes.VAELASTRAZ_THE_CORRUPT) != EncounterState.Done)
                        return false;

                    break;
                case DataTypes.FIREMAW:
                case DataTypes.EBONROC:
                case DataTypes.FLAMEGOR:
                    if (GetBossState(DataTypes.BROODLORD_LASHLAYER) != EncounterState.Done)
                        return false;

                    break;
                case DataTypes.CHROMAGGUS:
                    if (GetBossState(DataTypes.FIREMAW) != EncounterState.Done ||
                        GetBossState(DataTypes.EBONROC) != EncounterState.Done ||
                        GetBossState(DataTypes.FLAMEGOR) != EncounterState.Done)
                        return false;

                    break;
            }

            return true;
        }

        public override bool SetBossState(uint type, EncounterState state)
        {
            if (!base.SetBossState(type, state))
                return false;

            switch (type)
            {
                case DataTypes.RAZORGORE_THE_UNTAMED:
                    if (state == EncounterState.Done)
                        foreach (var guid in _eggList)
                        {
                            var egg = Instance.GetGameObject(guid);

                            if (egg)
                                egg.SetLootState(LootState.JustDeactivated);
                        }

                    SetData(BwlMisc.DATA_EGG_EVENT, (uint)EncounterState.NotStarted);

                    break;
                case DataTypes.NEFARIAN:
                    switch (state)
                    {
                        case EncounterState.NotStarted:
                            var nefarian = GetCreature(DataTypes.NEFARIAN);

                            if (nefarian)
                                nefarian.DespawnOrUnsummon();

                            break;
                        case EncounterState.Fail:
                            _events.ScheduleEvent(EventIds.RESPAWN_NEFARIUS, TimeSpan.FromMinutes(15));
                            SetBossState(DataTypes.NEFARIAN, EncounterState.NotStarted);

                            break;
                    }

                    break;
            }

            return true;
        }

        public override void SetData(uint type, uint data)
        {
            if (type == BwlMisc.DATA_EGG_EVENT)
                switch ((EncounterState)data)
                {
                    case EncounterState.InProgress:
                        _events.ScheduleEvent(EventIds.RAZOR_SPAWN, TimeSpan.FromSeconds(45));
                        _eggEvent = data;
                        _eggCount = 0;

                        break;
                    case EncounterState.NotStarted:
                        _events.CancelEvent(EventIds.RAZOR_SPAWN);
                        _eggEvent = data;
                        _eggCount = 0;

                        break;
                    case EncounterState.Special:
                        if (++_eggCount == 15)
                        {
                            var razor = GetCreature(DataTypes.RAZORGORE_THE_UNTAMED);

                            if (razor)
                            {
                                SetData(BwlMisc.DATA_EGG_EVENT, (uint)EncounterState.Done);
                                razor.RemoveAura(42013); // MindControl
                                DoRemoveAurasDueToSpellOnPlayers(42013, true, true);
                            }

                            _events.ScheduleEvent(EventIds.RAZOR_PHASE_TWO, TimeSpan.FromSeconds(1));
                            _events.CancelEvent(EventIds.RAZOR_SPAWN);
                        }

                        if (_eggEvent == (uint)EncounterState.NotStarted)
                            SetData(BwlMisc.DATA_EGG_EVENT, (uint)EncounterState.InProgress);

                        break;
                }
        }

        public override void OnUnitDeath(Unit unit)
        {
            //! Hack, needed because of buggy CreatureAI after charm
            if (unit.Entry == BwlCreatureIds.RAZORGORE &&
                GetBossState(DataTypes.RAZORGORE_THE_UNTAMED) != EncounterState.Done)
                SetBossState(DataTypes.RAZORGORE_THE_UNTAMED, EncounterState.Done);
        }

        public override void Update(uint diff)
        {
            if (_events.Empty())
                return;

            _events.Update(diff);

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case EventIds.RAZOR_SPAWN:
                        for (var i = RandomHelper.URand(2, 5); i > 0; --i)
                        {
                            Creature summon = Instance.SummonCreature(BwlMisc.Entry[RandomHelper.URand(0, 4)], BwlMisc.SummonPosition[RandomHelper.URand(0, 7)]);

                            if (summon)
                                summon.AI.DoZoneInCombat();
                        }

                        _events.ScheduleEvent(EventIds.RAZOR_SPAWN, TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(17));

                        break;
                    case EventIds.RAZOR_PHASE_TWO:
                        _events.CancelEvent(EventIds.RAZOR_SPAWN);
                        var razor = GetCreature(DataTypes.RAZORGORE_THE_UNTAMED);

                        if (razor)
                            razor.AI.DoAction(BwlMisc.ACTION_PHASE_TWO);

                        break;
                    case EventIds.RESPAWN_NEFARIUS:
                        var nefarius = GetCreature(DataTypes.LORD_VICTOR_NEFARIUS);

                        if (nefarius)
                        {
                            nefarius.SetActive(true);
                            nefarius.SetFarVisible(true);
                            nefarius.Respawn();
                            nefarius.MotionMaster.MoveTargetedHome();
                        }

                        break;
                }
            });
        }
    }
}