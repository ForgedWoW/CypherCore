// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting.BaseScripts;
using Forged.MapServer.Scripting.Interfaces.IMap;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths;

internal struct CreatureIds
{
    public const uint EMPEROR = 9019;
    public const uint PHALANX = 9502;
    public const uint ANGERREL = 9035;
    public const uint DOPEREL = 9040;
    public const uint HATEREL = 9034;
    public const uint VILEREL = 9036;
    public const uint SEETHREL = 9038;
    public const uint GLOOMREL = 9037;
    public const uint DOOMREL = 9039;
    public const uint MAGMUS = 9938;
    public const uint MOIRA = 8929;
    public const uint COREN = 23872;
}

internal struct GameObjectIds
{
    public const uint ARENA1 = 161525;
    public const uint ARENA2 = 161522;
    public const uint ARENA3 = 161524;
    public const uint ARENA4 = 161523;
    public const uint SHADOW_LOCK = 161460;
    public const uint SHADOW_MECHANISM = 161461;
    public const uint SHADOW_GIANT_DOOR = 157923;
    public const uint SHADOW_DUMMY = 161516;
    public const uint BAR_KEG_SHOT = 170607;
    public const uint BAR_KEG_TRAP = 171941;
    public const uint BAR_DOOR = 170571;
    public const uint TOMB_ENTER = 170576;
    public const uint TOMB_EXIT = 170577;
    public const uint LYCEUM = 170558;
    public const uint SF_N = 174745;         // Shadowforge Brazier North
    public const uint SF_S = 174744;         // Shadowforge Brazier South
    public const uint GOLEM_ROOM_N = 170573; // Magmus door North
    public const uint GOLEM_ROOM_S = 170574; // Magmus door Soutsh
    public const uint THRONE_ROOM = 170575;  // Throne door
    public const uint SPECTRAL_CHALICE = 164869;
    public const uint CHEST_SEVEN = 169243;
}

internal struct DataTypes
{
    public const uint TYPE_RING_OF_LAW = 1;
    public const uint TYPE_VAULT = 2;
    public const uint TYPE_BAR = 3;
    public const uint TYPE_TOMB_OF_SEVEN = 4;
    public const uint TYPE_LYCEUM = 5;
    public const uint TYPE_IRON_HALL = 6;

    public const uint DATA_EMPEROR = 10;
    public const uint DATA_PHALANX = 11;

    public const uint DATA_ARENA1 = 12;
    public const uint DATA_ARENA2 = 13;
    public const uint DATA_ARENA3 = 14;
    public const uint DATA_ARENA4 = 15;

    public const uint DATA_GO_BAR_KEG = 16;
    public const uint DATA_GO_BAR_KEG_TRAP = 17;
    public const uint DATA_GO_BAR_DOOR = 18;
    public const uint DATA_GO_CHALICE = 19;

    public const uint DATA_GHOSTKILL = 20;
    public const uint DATA_EVENSTARTER = 21;

    public const uint DATA_GOLEM_DOOR_N = 22;
    public const uint DATA_GOLEM_DOOR_S = 23;

    public const uint DATA_THRONE_DOOR = 24;

    public const uint DATA_SF_BRAZIER_N = 25;
    public const uint DATA_SF_BRAZIER_S = 26;
    public const uint DATA_MOIRA = 27;
    public const uint DATA_COREN = 28;
}

internal struct MiscConst
{
    public const uint TIMER_TOMB_OF_THE_SEVEN = 15000;
    public const uint MAX_ENCOUNTER = 6;
    public const uint TOMB_OF_SEVEN_BOSS_NUM = 7;
}

internal class InstanceBlackrockDepths : InstanceMapScript, IInstanceMapGetInstanceScript
{
    public InstanceBlackrockDepths() : base(nameof(InstanceBlackrockDepths), 230) { }

    public InstanceScript GetInstanceScript(InstanceMap map)
    {
        return new InstanceBlackrockDepthsInstanceMapScript(map);
    }

    private class InstanceBlackrockDepthsInstanceMapScript : InstanceScript
    {
        private readonly ObjectGuid[] _tombBossGuiDs = new ObjectGuid[MiscConst.TOMB_OF_SEVEN_BOSS_NUM];
        private uint _barAleCount;
        private ObjectGuid _corenGUID;
        private ObjectGuid _emperorGUID;
        private uint _ghostKillCount;

        private ObjectGuid _goArena1GUID;
        private ObjectGuid _goArena2GUID;
        private ObjectGuid _goArena3GUID;
        private ObjectGuid _goArena4GUID;
        private ObjectGuid _goBarDoorGUID;
        private ObjectGuid _goBarKegGUID;
        private ObjectGuid _goBarKegTrapGUID;
        private ObjectGuid _goChestGUID;
        private ObjectGuid _goGolemNguid;
        private ObjectGuid _goGolemSguid;
        private ObjectGuid _goLyceumGUID;
        private ObjectGuid _goSfnguid;
        private ObjectGuid _goSfsguid;
        private ObjectGuid _goShadowDummyGUID;
        private ObjectGuid _goShadowGiantGUID;
        private ObjectGuid _goShadowLockGUID;
        private ObjectGuid _goShadowMechGUID;
        private ObjectGuid _goSpectralChaliceGUID;
        private ObjectGuid _goThroneGUID;
        private ObjectGuid _goTombEnterGUID;
        private ObjectGuid _goTombExitGUID;
        private ObjectGuid _magmusGUID;
        private ObjectGuid _moiraGUID;
        private ObjectGuid _phalanxGUID;
        private uint _tombEventCounter;
        private ObjectGuid _tombEventStarterGUID;
        private uint _tombTimer;

        public InstanceBlackrockDepthsInstanceMapScript(InstanceMap map) : base(map)
        {
            SetHeaders("BRD");
            SetBossNumber(MiscConst.MAX_ENCOUNTER);

            _barAleCount = 0;
            _ghostKillCount = 0;
            _tombTimer = MiscConst.TIMER_TOMB_OF_THE_SEVEN;
            _tombEventCounter = 0;
        }

        public override void OnCreatureCreate(Creature creature)
        {
            switch (creature.Entry)
            {
                case CreatureIds.EMPEROR:
                    _emperorGUID = creature.GUID;

                    break;
                case CreatureIds.PHALANX:
                    _phalanxGUID = creature.GUID;

                    break;
                case CreatureIds.MOIRA:
                    _moiraGUID = creature.GUID;

                    break;
                case CreatureIds.COREN:
                    _corenGUID = creature.GUID;

                    break;
                case CreatureIds.DOOMREL:
                    _tombBossGuiDs[0] = creature.GUID;

                    break;
                case CreatureIds.DOPEREL:
                    _tombBossGuiDs[1] = creature.GUID;

                    break;
                case CreatureIds.HATEREL:
                    _tombBossGuiDs[2] = creature.GUID;

                    break;
                case CreatureIds.VILEREL:
                    _tombBossGuiDs[3] = creature.GUID;

                    break;
                case CreatureIds.SEETHREL:
                    _tombBossGuiDs[4] = creature.GUID;

                    break;
                case CreatureIds.GLOOMREL:
                    _tombBossGuiDs[5] = creature.GUID;

                    break;
                case CreatureIds.ANGERREL:
                    _tombBossGuiDs[6] = creature.GUID;

                    break;
                case CreatureIds.MAGMUS:
                    _magmusGUID = creature.GUID;

                    if (!creature.IsAlive)
                        HandleGameObject(GetGuidData(DataTypes.DATA_THRONE_DOOR), true); // if Magmus is dead open door to last boss

                    break;
            }
        }

        public override void OnGameObjectCreate(GameObject go)
        {
            switch (go.Entry)
            {
                case GameObjectIds.ARENA1:
                    _goArena1GUID = go.GUID;

                    break;
                case GameObjectIds.ARENA2:
                    _goArena2GUID = go.GUID;

                    break;
                case GameObjectIds.ARENA3:
                    _goArena3GUID = go.GUID;

                    break;
                case GameObjectIds.ARENA4:
                    _goArena4GUID = go.GUID;

                    break;
                case GameObjectIds.SHADOW_LOCK:
                    _goShadowLockGUID = go.GUID;

                    break;
                case GameObjectIds.SHADOW_MECHANISM:
                    _goShadowMechGUID = go.GUID;

                    break;
                case GameObjectIds.SHADOW_GIANT_DOOR:
                    _goShadowGiantGUID = go.GUID;

                    break;
                case GameObjectIds.SHADOW_DUMMY:
                    _goShadowDummyGUID = go.GUID;

                    break;
                case GameObjectIds.BAR_KEG_SHOT:
                    _goBarKegGUID = go.GUID;

                    break;
                case GameObjectIds.BAR_KEG_TRAP:
                    _goBarKegTrapGUID = go.GUID;

                    break;
                case GameObjectIds.BAR_DOOR:
                    _goBarDoorGUID = go.GUID;

                    break;
                case GameObjectIds.TOMB_ENTER:
                    _goTombEnterGUID = go.GUID;

                    break;
                case GameObjectIds.TOMB_EXIT:
                    _goTombExitGUID = go.GUID;

                    if (_ghostKillCount >= MiscConst.TOMB_OF_SEVEN_BOSS_NUM)
                        HandleGameObject(ObjectGuid.Empty, true, go);
                    else
                        HandleGameObject(ObjectGuid.Empty, false, go);

                    break;
                case GameObjectIds.LYCEUM:
                    _goLyceumGUID = go.GUID;

                    break;
                case GameObjectIds.SF_S:
                    _goSfsguid = go.GUID;

                    break;
                case GameObjectIds.SF_N:
                    _goSfnguid = go.GUID;

                    break;
                case GameObjectIds.GOLEM_ROOM_N:
                    _goGolemNguid = go.GUID;

                    break;
                case GameObjectIds.GOLEM_ROOM_S:
                    _goGolemSguid = go.GUID;

                    break;
                case GameObjectIds.THRONE_ROOM:
                    _goThroneGUID = go.GUID;

                    break;
                case GameObjectIds.CHEST_SEVEN:
                    _goChestGUID = go.GUID;

                    break;
                case GameObjectIds.SPECTRAL_CHALICE:
                    _goSpectralChaliceGUID = go.GUID;

                    break;
            }
        }

        public override void SetGuidData(uint type, ObjectGuid data)
        {
            switch (type)
            {
                case DataTypes.DATA_EVENSTARTER:
                    _tombEventStarterGUID = data;

                    if (_tombEventStarterGUID.IsEmpty)
                        TombOfSevenReset(); //reset
                    else
                        TombOfSevenStart(); //start

                    break;
            }
        }

        public override void SetData(uint type, uint data)
        {
            switch (type)
            {
                case DataTypes.TYPE_RING_OF_LAW:
                    SetBossState(0, (EncounterState)data);

                    break;
                case DataTypes.TYPE_VAULT:
                    SetBossState(1, (EncounterState)data);

                    break;
                case DataTypes.TYPE_BAR:
                    if (data == (uint)EncounterState.Special)
                        ++_barAleCount;
                    else
                        SetBossState(2, (EncounterState)data);

                    break;
                case DataTypes.TYPE_TOMB_OF_SEVEN:
                    SetBossState(3, (EncounterState)data);

                    break;
                case DataTypes.TYPE_LYCEUM:
                    SetBossState(4, (EncounterState)data);

                    break;
                case DataTypes.TYPE_IRON_HALL:
                    SetBossState(5, (EncounterState)data);

                    break;
                case DataTypes.DATA_GHOSTKILL:
                    _ghostKillCount += data;

                    break;
            }
        }

        public override uint GetData(uint type)
        {
            switch (type)
            {
                case DataTypes.TYPE_RING_OF_LAW:
                    return (uint)GetBossState(0);
                case DataTypes.TYPE_VAULT:
                    return (uint)GetBossState(1);
                case DataTypes.TYPE_BAR:
                    if (GetBossState(2) == EncounterState.InProgress &&
                        _barAleCount == 3)
                        return (uint)EncounterState.Special;
                    else
                        return (uint)GetBossState(2);
                case DataTypes.TYPE_TOMB_OF_SEVEN:
                    return (uint)GetBossState(3);
                case DataTypes.TYPE_LYCEUM:
                    return (uint)GetBossState(4);
                case DataTypes.TYPE_IRON_HALL:
                    return (uint)GetBossState(5);
                case DataTypes.DATA_GHOSTKILL:
                    return _ghostKillCount;
            }

            return 0;
        }

        public override ObjectGuid GetGuidData(uint data)
        {
            switch (data)
            {
                case DataTypes.DATA_EMPEROR:
                    return _emperorGUID;
                case DataTypes.DATA_PHALANX:
                    return _phalanxGUID;
                case DataTypes.DATA_MOIRA:
                    return _moiraGUID;
                case DataTypes.DATA_COREN:
                    return _corenGUID;
                case DataTypes.DATA_ARENA1:
                    return _goArena1GUID;
                case DataTypes.DATA_ARENA2:
                    return _goArena2GUID;
                case DataTypes.DATA_ARENA3:
                    return _goArena3GUID;
                case DataTypes.DATA_ARENA4:
                    return _goArena4GUID;
                case DataTypes.DATA_GO_BAR_KEG:
                    return _goBarKegGUID;
                case DataTypes.DATA_GO_BAR_KEG_TRAP:
                    return _goBarKegTrapGUID;
                case DataTypes.DATA_GO_BAR_DOOR:
                    return _goBarDoorGUID;
                case DataTypes.DATA_EVENSTARTER:
                    return _tombEventStarterGUID;
                case DataTypes.DATA_SF_BRAZIER_N:
                    return _goSfnguid;
                case DataTypes.DATA_SF_BRAZIER_S:
                    return _goSfsguid;
                case DataTypes.DATA_THRONE_DOOR:
                    return _goThroneGUID;
                case DataTypes.DATA_GOLEM_DOOR_N:
                    return _goGolemNguid;
                case DataTypes.DATA_GOLEM_DOOR_S:
                    return _goGolemSguid;
                case DataTypes.DATA_GO_CHALICE:
                    return _goSpectralChaliceGUID;
            }

            return ObjectGuid.Empty;
        }

        public override void Update(uint diff)
        {
            if (!_tombEventStarterGUID.IsEmpty &&
                _ghostKillCount < MiscConst.TOMB_OF_SEVEN_BOSS_NUM)
            {
                if (_tombTimer <= diff)
                {
                    _tombTimer = MiscConst.TIMER_TOMB_OF_THE_SEVEN;

                    if (_tombEventCounter < MiscConst.TOMB_OF_SEVEN_BOSS_NUM)
                    {
                        TombOfSevenEvent();
                        ++_tombEventCounter;
                    }

                    // Check Killed bosses
                    for (byte i = 0; i < MiscConst.TOMB_OF_SEVEN_BOSS_NUM; ++i)
                    {
                        var boss = Instance.GetCreature(_tombBossGuiDs[i]);

                        if (boss)
                            if (!boss.IsAlive)
                                _ghostKillCount = i + 1u;
                    }
                }
                else
                    _tombTimer -= diff;
            }

            if (_ghostKillCount >= MiscConst.TOMB_OF_SEVEN_BOSS_NUM &&
                !_tombEventStarterGUID.IsEmpty)
                TombOfSevenEnd();
        }

        private void TombOfSevenEvent()
        {
            if (_ghostKillCount < MiscConst.TOMB_OF_SEVEN_BOSS_NUM &&
                !_tombBossGuiDs[_tombEventCounter].IsEmpty)
            {
                var boss = Instance.GetCreature(_tombBossGuiDs[_tombEventCounter]);

                if (boss)
                {
                    boss.Faction = (uint)FactionTemplates.DarkIronDwarves;
                    boss.SetImmuneToPC(false);
                    var target = boss.SelectNearestTarget(500);

                    if (target)
                        boss.AI.AttackStart(target);
                }
            }
        }

        private void TombOfSevenReset()
        {
            HandleGameObject(_goTombExitGUID, false); //event reseted, close exit door
            HandleGameObject(_goTombEnterGUID, true); //event reseted, open entrance door

            for (byte i = 0; i < MiscConst.TOMB_OF_SEVEN_BOSS_NUM; ++i)
            {
                var boss = Instance.GetCreature(_tombBossGuiDs[i]);

                if (boss)
                {
                    if (!boss.IsAlive)
                        boss.Respawn();
                    else
                        boss.Faction = (uint)FactionTemplates.Friendly;
                }
            }

            _ghostKillCount = 0;
            _tombEventStarterGUID.Clear();
            _tombEventCounter = 0;
            _tombTimer = MiscConst.TIMER_TOMB_OF_THE_SEVEN;
            SetData(DataTypes.TYPE_TOMB_OF_SEVEN, (uint)EncounterState.NotStarted);
        }

        private void TombOfSevenStart()
        {
            HandleGameObject(_goTombExitGUID, false);  //event started, close exit door
            HandleGameObject(_goTombEnterGUID, false); //event started, close entrance door
            SetData(DataTypes.TYPE_TOMB_OF_SEVEN, (uint)EncounterState.InProgress);
        }

        private void TombOfSevenEnd()
        {
            DoRespawnGameObject(_goChestGUID, TimeSpan.FromHours(24));
            HandleGameObject(_goTombExitGUID, true);  //event done, open exit door
            HandleGameObject(_goTombEnterGUID, true); //event done, open entrance door
            _tombEventStarterGUID.Clear();
            SetData(DataTypes.TYPE_TOMB_OF_SEVEN, (uint)EncounterState.Done);
        }
    }
}