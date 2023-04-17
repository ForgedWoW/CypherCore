// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
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

namespace Scripts.EasternKingdoms.Karazhan;

internal struct DataTypes
{
    public const uint ATTUMEN = 0;
    public const uint MOROES = 1;
    public const uint MAIDEN_OF_VIRTUE = 2;
    public const uint OPTIONAL_BOSS = 3;
    public const uint OPERA_PERFORMANCE = 4;
    public const uint CURATOR = 5;
    public const uint ARAN = 6;
    public const uint TERESTIAN = 7;
    public const uint NETHERSPITE = 8;
    public const uint CHESS = 9;
    public const uint MALCHEZZAR = 10;
    public const uint NIGHTBANE = 11;

    public const uint OPERA_OZ_DEATHCOUNT = 14;

    public const uint KILREK = 15;
    public const uint GO_CURTAINS = 18;
    public const uint GO_STAGEDOORLEFT = 19;
    public const uint GO_STAGEDOORRIGHT = 20;
    public const uint GO_LIBRARY_DOOR = 21;
    public const uint GO_MASSIVE_DOOR = 22;
    public const uint GO_NETHER_DOOR = 23;
    public const uint GO_GAME_DOOR = 24;
    public const uint GO_GAME_EXIT_DOOR = 25;

    public const uint IMAGE_OF_MEDIVH = 26;
    public const uint MASTERS_TERRACE_DOOR1 = 27;
    public const uint MASTERS_TERRACE_DOOR2 = 28;
    public const uint GO_SIDE_ENTRANCE_DOOR = 29;
    public const uint GO_BLACKENED_URN = 30;
}

internal struct CreatureIds
{
    public const uint HYAKISS_THE_LURKER = 16179;
    public const uint ROKAD_THE_RAVAGER = 16181;
    public const uint SHADIKITH_THE_GLIDER = 16180;
    public const uint TERESTIAN_ILLHOOF = 15688;
    public const uint MOROES = 15687;
    public const uint NIGHTBANE = 17225;
    public const uint ATTUMEN_UNMOUNTED = 15550;
    public const uint ATTUMEN_MOUNTED = 16152;
    public const uint MIDNIGHT = 16151;

    // Trash
    public const uint COLDMIST_WIDOW = 16171;
    public const uint COLDMIST_STALKER = 16170;
    public const uint SHADOWBAT = 16173;
    public const uint VAMPIRIC_SHADOWBAT = 16175;
    public const uint GREATER_SHADOWBAT = 16174;
    public const uint PHASE_HOUND = 16178;
    public const uint DREADBEAST = 16177;
    public const uint SHADOWBEAST = 16176;
    public const uint KILREK = 17229;
}

internal struct GameObjectIds
{
    public const uint STAGE_CURTAIN = 183932;
    public const uint STAGE_DOOR_LEFT = 184278;
    public const uint STAGE_DOOR_RIGHT = 184279;
    public const uint PRIVATE_LIBRARY_DOOR = 184517;
    public const uint MASSIVE_DOOR = 185521;
    public const uint GAMESMAN_HALL_DOOR = 184276;
    public const uint GAMESMAN_HALL_EXIT_DOOR = 184277;
    public const uint NETHERSPACE_DOOR = 185134;
    public const uint MASTERS_TERRACE_DOOR = 184274;
    public const uint MASTERS_TERRACE_DOOR2 = 184280;
    public const uint SIDE_ENTRANCE_DOOR = 184275;
    public const uint DUST_COVERED_CHEST = 185119;
    public const uint BLACKENED_URN = 194092;
}

internal enum KzMisc
{
    OptionalBossRequiredDeathCount = 50
}

[Script]
internal class InstanceKarazhan : InstanceMapScript, IInstanceMapGetInstanceScript
{
    public static Position[] OptionalSpawn =
    {
        new(-10960.981445f, -1940.138428f, 46.178097f, 4.12f),  // Hyakiss the Lurker
        new(-10945.769531f, -2040.153320f, 49.474438f, 0.077f), // Shadikith the Glider
        new(-10899.903320f, -2085.573730f, 49.474449f, 1.38f)   // Rokad the Ravager
    };

    private static readonly DungeonEncounterData[] Encounters =
    {
        new(DataTypes.ATTUMEN, 652), new(DataTypes.MOROES, 653), new(DataTypes.MAIDEN_OF_VIRTUE, 654), new(DataTypes.OPERA_PERFORMANCE, 655), new(DataTypes.CURATOR, 656), new(DataTypes.ARAN, 658), new(DataTypes.TERESTIAN, 657), new(DataTypes.NETHERSPITE, 659), new(DataTypes.CHESS, 660), new(DataTypes.MALCHEZZAR, 661), new(DataTypes.NIGHTBANE, 662)
    };

    public InstanceKarazhan() : base(nameof(InstanceKarazhan), 532) { }

    public InstanceScript GetInstanceScript(InstanceMap map)
    {
        return new InstanceKarazhanInstanceMapScript(map);
    }

    private class InstanceKarazhanInstanceMapScript : InstanceScript
    {
        private readonly ObjectGuid[] _mastersTerraceDoor = new ObjectGuid[2];
        private readonly uint _operaEvent;
        private ObjectGuid _blackenedUrnGUID;
        private ObjectGuid _curtainGUID;
        private ObjectGuid _dustCoveredChest;
        private ObjectGuid _gamesmansDoor;     // Door before Chess
        private ObjectGuid _gamesmansExitDoor; // Door after Chess
        private ObjectGuid _imageGUID;
        private ObjectGuid _kilrekGUID;
        private ObjectGuid _libraryDoor; // Door at Shade of Aran
        private ObjectGuid _massiveDoor; // Door at Netherspite
        private ObjectGuid _moroesGUID;
        private ObjectGuid _netherspaceDoor; // Door at Malchezaar
        private ObjectGuid _nightbaneGUID;
        private uint _optionalBossCount;
        private uint _ozDeathCount;
        private ObjectGuid _sideEntranceDoor; // Side Entrance
        private ObjectGuid _stageDoorLeftGUID;
        private ObjectGuid _stageDoorRightGUID;
        private ObjectGuid _terestianGUID;

        public InstanceKarazhanInstanceMapScript(InstanceMap map) : base(map)
        {
            SetHeaders("KZ");
            SetBossNumber(12);
            LoadDungeonEncounterData(Encounters);

            // 1 - Oz, 2 - Hood, 3 - Raj, this never gets altered.
            _operaEvent = RandomHelper.URand(1, 3);
            _ozDeathCount = 0;
            _optionalBossCount = 0;
        }

        public override void OnCreatureCreate(Creature creature)
        {
            switch (creature.Entry)
            {
                case CreatureIds.KILREK:
                    _kilrekGUID = creature.GUID;

                    break;
                case CreatureIds.TERESTIAN_ILLHOOF:
                    _terestianGUID = creature.GUID;

                    break;
                case CreatureIds.MOROES:
                    _moroesGUID = creature.GUID;

                    break;
                case CreatureIds.NIGHTBANE:
                    _nightbaneGUID = creature.GUID;

                    break;
            }
        }

        public override void OnUnitDeath(Unit unit)
        {
            var creature = unit.AsCreature;

            if (!creature)
                return;

            switch (creature.Entry)
            {
                case CreatureIds.COLDMIST_WIDOW:
                case CreatureIds.COLDMIST_STALKER:
                case CreatureIds.SHADOWBAT:
                case CreatureIds.VAMPIRIC_SHADOWBAT:
                case CreatureIds.GREATER_SHADOWBAT:
                case CreatureIds.PHASE_HOUND:
                case CreatureIds.DREADBEAST:
                case CreatureIds.SHADOWBEAST:
                    if (GetBossState(DataTypes.OPTIONAL_BOSS) == EncounterState.ToBeDecided)
                    {
                        ++_optionalBossCount;

                        if (_optionalBossCount == (uint)KzMisc.OptionalBossRequiredDeathCount)
                            switch (RandomHelper.URand(CreatureIds.HYAKISS_THE_LURKER, CreatureIds.ROKAD_THE_RAVAGER))
                            {
                                case CreatureIds.HYAKISS_THE_LURKER:
                                    Instance.SummonCreature(CreatureIds.HYAKISS_THE_LURKER, OptionalSpawn[0]);

                                    break;
                                case CreatureIds.SHADIKITH_THE_GLIDER:
                                    Instance.SummonCreature(CreatureIds.SHADIKITH_THE_GLIDER, OptionalSpawn[1]);

                                    break;
                                case CreatureIds.ROKAD_THE_RAVAGER:
                                    Instance.SummonCreature(CreatureIds.ROKAD_THE_RAVAGER, OptionalSpawn[2]);

                                    break;
                            }
                    }

                    break;
            }
        }

        public override void SetData(uint type, uint data)
        {
            switch (type)
            {
                case DataTypes.OPERA_OZ_DEATHCOUNT:
                    if (data == (uint)EncounterState.Special)
                        ++_ozDeathCount;
                    else if (data == (uint)EncounterState.InProgress)
                        _ozDeathCount = 0;

                    break;
            }
        }

        public override bool SetBossState(uint type, EncounterState state)
        {
            if (!base.SetBossState(type, state))
                return false;

            switch (type)
            {
                case DataTypes.OPERA_PERFORMANCE:
                    if (state == EncounterState.Done)
                    {
                        HandleGameObject(_stageDoorLeftGUID, true);
                        HandleGameObject(_stageDoorRightGUID, true);
                        var sideEntrance = Instance.GetGameObject(_sideEntranceDoor);

                        if (sideEntrance)
                            sideEntrance.RemoveFlag(GameObjectFlags.Locked);

                        UpdateEncounterStateForKilledCreature(16812, null);
                    }

                    break;
                case DataTypes.CHESS:
                    if (state == EncounterState.Done)
                        DoRespawnGameObject(_dustCoveredChest, TimeSpan.FromHours(24));

                    break;
            }

            return true;
        }

        public override void SetGuidData(uint type, ObjectGuid data)
        {
            if (type == DataTypes.IMAGE_OF_MEDIVH)
                _imageGUID = data;
        }

        public override void OnGameObjectCreate(GameObject go)
        {
            switch (go.Entry)
            {
                case GameObjectIds.STAGE_CURTAIN:
                    _curtainGUID = go.GUID;

                    break;
                case GameObjectIds.STAGE_DOOR_LEFT:
                    _stageDoorLeftGUID = go.GUID;

                    if (GetBossState(DataTypes.OPERA_PERFORMANCE) == EncounterState.Done)
                        go.SetGoState(GameObjectState.Active);

                    break;
                case GameObjectIds.STAGE_DOOR_RIGHT:
                    _stageDoorRightGUID = go.GUID;

                    if (GetBossState(DataTypes.OPERA_PERFORMANCE) == EncounterState.Done)
                        go.SetGoState(GameObjectState.Active);

                    break;
                case GameObjectIds.PRIVATE_LIBRARY_DOOR:
                    _libraryDoor = go.GUID;

                    break;
                case GameObjectIds.MASSIVE_DOOR:
                    _massiveDoor = go.GUID;

                    break;
                case GameObjectIds.GAMESMAN_HALL_DOOR:
                    _gamesmansDoor = go.GUID;

                    break;
                case GameObjectIds.GAMESMAN_HALL_EXIT_DOOR:
                    _gamesmansExitDoor = go.GUID;

                    break;
                case GameObjectIds.NETHERSPACE_DOOR:
                    _netherspaceDoor = go.GUID;

                    break;
                case GameObjectIds.MASTERS_TERRACE_DOOR:
                    _mastersTerraceDoor[0] = go.GUID;

                    break;
                case GameObjectIds.MASTERS_TERRACE_DOOR2:
                    _mastersTerraceDoor[1] = go.GUID;

                    break;
                case GameObjectIds.SIDE_ENTRANCE_DOOR:
                    _sideEntranceDoor = go.GUID;

                    if (GetBossState(DataTypes.OPERA_PERFORMANCE) == EncounterState.Done)
                        go.SetFlag(GameObjectFlags.Locked);
                    else
                        go.RemoveFlag(GameObjectFlags.Locked);

                    break;
                case GameObjectIds.DUST_COVERED_CHEST:
                    _dustCoveredChest = go.GUID;

                    break;
                case GameObjectIds.BLACKENED_URN:
                    _blackenedUrnGUID = go.GUID;

                    break;
            }

            switch (_operaEvent)
            {
                /// @todo Set Object visibilities for Opera based on performance
                case 1:
                    break;

                case 2:
                    break;

                case 3:
                    break;
            }
        }

        public override uint GetData(uint type)
        {
            switch (type)
            {
                case DataTypes.OPERA_PERFORMANCE:
                    return _operaEvent;
                case DataTypes.OPERA_OZ_DEATHCOUNT:
                    return _ozDeathCount;
            }

            return 0;
        }

        public override ObjectGuid GetGuidData(uint type)
        {
            switch (type)
            {
                case DataTypes.KILREK:
                    return _kilrekGUID;
                case DataTypes.TERESTIAN:
                    return _terestianGUID;
                case DataTypes.MOROES:
                    return _moroesGUID;
                case DataTypes.NIGHTBANE:
                    return _nightbaneGUID;
                case DataTypes.GO_STAGEDOORLEFT:
                    return _stageDoorLeftGUID;
                case DataTypes.GO_STAGEDOORRIGHT:
                    return _stageDoorRightGUID;
                case DataTypes.GO_CURTAINS:
                    return _curtainGUID;
                case DataTypes.GO_LIBRARY_DOOR:
                    return _libraryDoor;
                case DataTypes.GO_MASSIVE_DOOR:
                    return _massiveDoor;
                case DataTypes.GO_SIDE_ENTRANCE_DOOR:
                    return _sideEntranceDoor;
                case DataTypes.GO_GAME_DOOR:
                    return _gamesmansDoor;
                case DataTypes.GO_GAME_EXIT_DOOR:
                    return _gamesmansExitDoor;
                case DataTypes.GO_NETHER_DOOR:
                    return _netherspaceDoor;
                case DataTypes.MASTERS_TERRACE_DOOR1:
                    return _mastersTerraceDoor[0];
                case DataTypes.MASTERS_TERRACE_DOOR2:
                    return _mastersTerraceDoor[1];
                case DataTypes.IMAGE_OF_MEDIVH:
                    return _imageGUID;
                case DataTypes.GO_BLACKENED_URN:
                    return _blackenedUrnGUID;
            }

            return ObjectGuid.Empty;
        }
    }
}