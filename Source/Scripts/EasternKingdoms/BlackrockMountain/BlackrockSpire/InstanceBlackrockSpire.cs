// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.BaseScripts;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Forged.MapServer.Scripting.Interfaces.IMap;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire;

internal struct DataTypes
{
    public const uint HIGHLORD_OMOKK = 0;
    public const uint SHADOW_HUNTER_VOSHGAJIN = 1;
    public const uint WARMASTER_VOONE = 2;
    public const uint MOTHER_SMOLDERWEB = 3;
    public const uint UROK_DOOMHOWL = 4;
    public const uint QUARTERMASTER_ZIGRIS = 5;
    public const uint GIZRUL_THE_SLAVENER = 6;
    public const uint HALYCON = 7;
    public const uint OVERLORD_WYRMTHALAK = 8;
    public const uint PYROGAURD_EMBERSEER = 9;
    public const uint WARCHIEF_REND_BLACKHAND = 10;
    public const uint GYTH = 11;
    public const uint THE_BEAST = 12;
    public const uint GENERAL_DRAKKISATH = 13;

    public const uint LORD_VALTHALAK = 14;

    // Extra
    public const uint DRAGONSPIRE_ROOM = 15;
    public const uint HALL_RUNE1 = 16;
    public const uint HALL_RUNE2 = 17;
    public const uint HALL_RUNE3 = 18;
    public const uint HALL_RUNE4 = 19;
    public const uint HALL_RUNE5 = 20;
    public const uint HALL_RUNE6 = 21;
    public const uint HALL_RUNE7 = 22;
    public const uint SCARSHIELD_INFILTRATOR = 23;
    public const uint BLACKHAND_INCARCERATOR = 24;
}

internal struct CreaturesIds
{
    public const uint HIGHLORD_OMOKK = 9196;
    public const uint SHADOW_HUNTER_VOSHGAJIN = 9236;
    public const uint WARMASTER_VOONE = 9237;
    public const uint MOTHER_SMOLDERWEB = 10596;
    public const uint UROK_DOOMHOWL = 10584;
    public const uint QUARTERMASTER_ZIGRIS = 9736;
    public const uint GIZRUL_THE_SLAVENER = 10268;
    public const uint HALYCON = 10220;
    public const uint OVERLORD_WYRMTHALAK = 9568;
    public const uint PYROGAURD_EMBERSEER = 9816;
    public const uint WARCHIEF_REND_BLACKHAND = 10429;
    public const uint GYTH = 10339;
    public const uint THE_BEAST = 10430;
    public const uint GENERAL_DRAKKISATH = 10363;
    public const uint BLACKHAND_DREADWEAVER = 9817;
    public const uint BLACKHAND_SUMMONER = 9818;
    public const uint BLACKHAND_VETERAN = 9819;
    public const uint BLACKHAND_INCARCERATOR = 10316;
    public const uint LORD_VICTOR_NEFARIUS = 10162;
    public const uint SCARSHIELD_INFILTRATOR = 10299;
}

internal struct GameObjectsIds
{
    public const uint WHELP_SPAWNER = 175622; // trap spawned by public const uint  Id 175124

    // Doors
    public const uint EMBERSEER_IN = 175244;  // First door to Pyroguard Emberseer
    public const uint DOORS = 175705;        // Second door to Pyroguard Emberseer
    public const uint EMBERSEER_OUT = 175153; // Door after Pyroguard Emberseer event
    public const uint GYTH_ENTRY_DOOR = 164726;
    public const uint GYTH_COMBAT_DOOR = 175185;
    public const uint GYTH_EXIT_DOOR = 175186;
    public const uint DRAKKISATH_DOOR1 = 175946;

    public const uint DRAKKISATH_DOOR2 = 175947;

    // Runes in drapublic const uint nspire hall
    public const uint HALL_RUNE1 = 175197;
    public const uint HALL_RUNE2 = 175199;
    public const uint HALL_RUNE3 = 175195;
    public const uint HALL_RUNE4 = 175200;
    public const uint HALL_RUNE5 = 175198;
    public const uint HALL_RUNE6 = 175196;

    public const uint HALL_RUNE7 = 175194;

    // Runes in emberseers room
    public const uint EMBERSEER_RUNE1 = 175266;
    public const uint EMBERSEER_RUNE2 = 175267;
    public const uint EMBERSEER_RUNE3 = 175268;
    public const uint EMBERSEER_RUNE4 = 175269;
    public const uint EMBERSEER_RUNE5 = 175270;
    public const uint EMBERSEER_RUNE6 = 175271;

    public const uint EMBERSEER_RUNE7 = 175272;

    // For Gyth event
    public const uint DR_PORTCULLIS = 175185;
    public const uint PORTCULLIS_ACTIVE = 164726;
    public const uint PORTCULLIS_TOBOSSROOMS = 175186;
}

internal struct BrsMiscConst
{
    public const uint SPELL_SUMMON_ROOKERY_WHELP = 15745;
    public const uint EVENT_UROK_DOOMHOWL = 4845;
    public const uint EVENT_PYROGUARD_EMBERSEER = 4884;
    public const uint AREATRIGGER = 1;
    public const uint AREATRIGGER_DRAGONSPIRE_HALL = 2046;
    public const uint AREATRIGGER_BLACKROCK_STADIUM = 2026;

    public const uint ENCOUNTER_COUNT = 23;

    //uint const DragonspireRunes[7] = { GoHallRune1, GoHallRune2, GoHallRune3, GoHallRune4, GoHallRune5, GoHallRune6, GoHallRune7 }

    public static uint[] DragonspireMobs =
    {
        CreaturesIds.BLACKHAND_DREADWEAVER, CreaturesIds.BLACKHAND_SUMMONER, CreaturesIds.BLACKHAND_VETERAN
    };

    public static DoorData[] DoorData =
    {
        new(GameObjectsIds.DOORS, DataTypes.PYROGAURD_EMBERSEER, DoorType.Passage), new(GameObjectsIds.EMBERSEER_OUT, DataTypes.PYROGAURD_EMBERSEER, DoorType.Passage), new(GameObjectsIds.DRAKKISATH_DOOR1, DataTypes.GENERAL_DRAKKISATH, DoorType.Passage), new(GameObjectsIds.DRAKKISATH_DOOR2, DataTypes.GENERAL_DRAKKISATH, DoorType.Passage), new(GameObjectsIds.PORTCULLIS_ACTIVE, DataTypes.WARCHIEF_REND_BLACKHAND, DoorType.Passage), new(GameObjectsIds.PORTCULLIS_TOBOSSROOMS, DataTypes.WARCHIEF_REND_BLACKHAND, DoorType.Passage)
    };
}

internal struct EventIds
{
    public const uint DARGONSPIRE_ROOM_STORE = 1;
    public const uint DARGONSPIRE_ROOM_CHECK = 2;
    public const uint UROK_DOOMHOWL_SPAWNS1 = 3;
    public const uint UROK_DOOMHOWL_SPAWNS2 = 4;
    public const uint UROK_DOOMHOWL_SPAWNS3 = 5;
    public const uint UROK_DOOMHOWL_SPAWNS4 = 6;
    public const uint UROK_DOOMHOWL_SPAWNS5 = 7;
    public const uint UROK_DOOMHOWL_SPAWN_IN = 8;
}

[Script]
internal class InstanceBlackrockSpire : InstanceMapScript, IInstanceMapGetInstanceScript
{
    public InstanceBlackrockSpire() : base(nameof(InstanceBlackrockSpire), 229) { }

    public InstanceScript GetInstanceScript(InstanceMap map)
    {
        return new InstanceBlackrockSpireMapScript(map);
    }

    private class InstanceBlackrockSpireMapScript : InstanceScript
    {
        private readonly List<ObjectGuid> _incarceratorList = new();
        private readonly ObjectGuid[] _goEmberseerrunes = new ObjectGuid[7];
        private readonly ObjectGuid[] _goRoomrunes = new ObjectGuid[7];
        private readonly List<ObjectGuid>[] _runecreaturelist = new List<ObjectGuid>[7];
        private ObjectGuid _generalDrakkisath;
        private ObjectGuid _gizrultheSlavener;
        private ObjectGuid _goDoors;
        private ObjectGuid _goEmberseerin;
        private ObjectGuid _goEmberseerout;
        private ObjectGuid _goPortcullisActive;
        private ObjectGuid _goPortcullisTobossrooms;
        private ObjectGuid _gyth;
        private ObjectGuid _halycon;

        private ObjectGuid _highlordOmokk;
        private ObjectGuid _lordVictorNefarius;
        private ObjectGuid _motherSmolderweb;
        private ObjectGuid _overlordWyrmthalak;
        private ObjectGuid _pyroguardEmberseer;
        private ObjectGuid _quartermasterZigris;
        private ObjectGuid _scarshieldInfiltrator;
        private ObjectGuid _shadowHunterVoshgajin;
        private ObjectGuid _theBeast;
        private ObjectGuid _urokDoomhowl;
        private ObjectGuid _warchiefRendBlackhand;
        private ObjectGuid _warMasterVoone;

        public InstanceBlackrockSpireMapScript(InstanceMap map) : base(map)
        {
            SetHeaders("BRSv1");
            SetBossNumber(BrsMiscConst.ENCOUNTER_COUNT);
            LoadDoorData(BrsMiscConst.DoorData);

            for (byte i = 0; i < 7; ++i)
                _runecreaturelist[i] = new List<ObjectGuid>();
        }

        public override void OnCreatureCreate(Creature creature)
        {
            switch (creature.Entry)
            {
                case CreaturesIds.HIGHLORD_OMOKK:
                    _highlordOmokk = creature.GUID;

                    break;
                case CreaturesIds.SHADOW_HUNTER_VOSHGAJIN:
                    _shadowHunterVoshgajin = creature.GUID;

                    break;
                case CreaturesIds.WARMASTER_VOONE:
                    _warMasterVoone = creature.GUID;

                    break;
                case CreaturesIds.MOTHER_SMOLDERWEB:
                    _motherSmolderweb = creature.GUID;

                    break;
                case CreaturesIds.UROK_DOOMHOWL:
                    _urokDoomhowl = creature.GUID;

                    break;
                case CreaturesIds.QUARTERMASTER_ZIGRIS:
                    _quartermasterZigris = creature.GUID;

                    break;
                case CreaturesIds.GIZRUL_THE_SLAVENER:
                    _gizrultheSlavener = creature.GUID;

                    break;
                case CreaturesIds.HALYCON:
                    _halycon = creature.GUID;

                    break;
                case CreaturesIds.OVERLORD_WYRMTHALAK:
                    _overlordWyrmthalak = creature.GUID;

                    break;
                case CreaturesIds.PYROGAURD_EMBERSEER:
                    _pyroguardEmberseer = creature.GUID;

                    if (GetBossState(DataTypes.PYROGAURD_EMBERSEER) == EncounterState.Done)
                        creature.DespawnOrUnsummon(TimeSpan.FromSeconds(0), TimeSpan.FromDays(7));

                    break;
                case CreaturesIds.WARCHIEF_REND_BLACKHAND:
                    _warchiefRendBlackhand = creature.GUID;

                    if (GetBossState(DataTypes.GYTH) == EncounterState.Done)
                        creature.DespawnOrUnsummon(TimeSpan.FromSeconds(0), TimeSpan.FromDays(7));

                    break;
                case CreaturesIds.GYTH:
                    _gyth = creature.GUID;

                    break;
                case CreaturesIds.THE_BEAST:
                    _theBeast = creature.GUID;

                    break;
                case CreaturesIds.GENERAL_DRAKKISATH:
                    _generalDrakkisath = creature.GUID;

                    break;
                case CreaturesIds.LORD_VICTOR_NEFARIUS:
                    _lordVictorNefarius = creature.GUID;

                    if (GetBossState(DataTypes.GYTH) == EncounterState.Done)
                        creature.DespawnOrUnsummon(TimeSpan.FromSeconds(0), TimeSpan.FromDays(7));

                    break;
                case CreaturesIds.SCARSHIELD_INFILTRATOR:
                    _scarshieldInfiltrator = creature.GUID;

                    break;
                case CreaturesIds.BLACKHAND_INCARCERATOR:
                    _incarceratorList.Add(creature.GUID);

                    break;
            }
        }

        public override void OnGameObjectCreate(GameObject go)
        {
            base.OnGameObjectCreate(go);

            switch (go.Entry)
            {
                case GameObjectsIds.WHELP_SPAWNER:
                    go.SpellFactory.CastSpell(BrsMiscConst.SPELL_SUMMON_ROOKERY_WHELP);

                    break;
                case GameObjectsIds.EMBERSEER_IN:
                    _goEmberseerin = go.GUID;

                    if (GetBossState(DataTypes.DRAGONSPIRE_ROOM) == EncounterState.Done)
                        HandleGameObject(ObjectGuid.Empty, true, go);

                    break;
                case GameObjectsIds.DOORS:
                    _goDoors = go.GUID;

                    if (GetBossState(DataTypes.DRAGONSPIRE_ROOM) == EncounterState.Done)
                        HandleGameObject(ObjectGuid.Empty, true, go);

                    break;
                case GameObjectsIds.EMBERSEER_OUT:
                    _goEmberseerout = go.GUID;

                    if (GetBossState(DataTypes.PYROGAURD_EMBERSEER) == EncounterState.Done)
                        HandleGameObject(ObjectGuid.Empty, true, go);

                    break;
                case GameObjectsIds.HALL_RUNE1:
                    _goRoomrunes[0] = go.GUID;

                    if (GetBossState(DataTypes.HALL_RUNE1) == EncounterState.Done)
                        HandleGameObject(ObjectGuid.Empty, false, go);

                    break;
                case GameObjectsIds.HALL_RUNE2:
                    _goRoomrunes[1] = go.GUID;

                    if (GetBossState(DataTypes.HALL_RUNE2) == EncounterState.Done)
                        HandleGameObject(ObjectGuid.Empty, false, go);

                    break;
                case GameObjectsIds.HALL_RUNE3:
                    _goRoomrunes[2] = go.GUID;

                    if (GetBossState(DataTypes.HALL_RUNE3) == EncounterState.Done)
                        HandleGameObject(ObjectGuid.Empty, false, go);

                    break;
                case GameObjectsIds.HALL_RUNE4:
                    _goRoomrunes[3] = go.GUID;

                    if (GetBossState(DataTypes.HALL_RUNE4) == EncounterState.Done)
                        HandleGameObject(ObjectGuid.Empty, false, go);

                    break;
                case GameObjectsIds.HALL_RUNE5:
                    _goRoomrunes[4] = go.GUID;

                    if (GetBossState(DataTypes.HALL_RUNE5) == EncounterState.Done)
                        HandleGameObject(ObjectGuid.Empty, false, go);

                    break;
                case GameObjectsIds.HALL_RUNE6:
                    _goRoomrunes[5] = go.GUID;

                    if (GetBossState(DataTypes.HALL_RUNE6) == EncounterState.Done)
                        HandleGameObject(ObjectGuid.Empty, false, go);

                    break;
                case GameObjectsIds.HALL_RUNE7:
                    _goRoomrunes[6] = go.GUID;

                    if (GetBossState(DataTypes.HALL_RUNE7) == EncounterState.Done)
                        HandleGameObject(ObjectGuid.Empty, false, go);

                    break;
                case GameObjectsIds.EMBERSEER_RUNE1:
                    _goEmberseerrunes[0] = go.GUID;

                    if (GetBossState(DataTypes.PYROGAURD_EMBERSEER) == EncounterState.Done)
                        HandleGameObject(ObjectGuid.Empty, false, go);

                    break;
                case GameObjectsIds.EMBERSEER_RUNE2:
                    _goEmberseerrunes[1] = go.GUID;

                    if (GetBossState(DataTypes.PYROGAURD_EMBERSEER) == EncounterState.Done)
                        HandleGameObject(ObjectGuid.Empty, false, go);

                    break;
                case GameObjectsIds.EMBERSEER_RUNE3:
                    _goEmberseerrunes[2] = go.GUID;

                    if (GetBossState(DataTypes.PYROGAURD_EMBERSEER) == EncounterState.Done)
                        HandleGameObject(ObjectGuid.Empty, false, go);

                    break;
                case GameObjectsIds.EMBERSEER_RUNE4:
                    _goEmberseerrunes[3] = go.GUID;

                    if (GetBossState(DataTypes.PYROGAURD_EMBERSEER) == EncounterState.Done)
                        HandleGameObject(ObjectGuid.Empty, false, go);

                    break;
                case GameObjectsIds.EMBERSEER_RUNE5:
                    _goEmberseerrunes[4] = go.GUID;

                    if (GetBossState(DataTypes.PYROGAURD_EMBERSEER) == EncounterState.Done)
                        HandleGameObject(ObjectGuid.Empty, false, go);

                    break;
                case GameObjectsIds.EMBERSEER_RUNE6:
                    _goEmberseerrunes[5] = go.GUID;

                    if (GetBossState(DataTypes.PYROGAURD_EMBERSEER) == EncounterState.Done)
                        HandleGameObject(ObjectGuid.Empty, false, go);

                    break;
                case GameObjectsIds.EMBERSEER_RUNE7:
                    _goEmberseerrunes[6] = go.GUID;

                    if (GetBossState(DataTypes.PYROGAURD_EMBERSEER) == EncounterState.Done)
                        HandleGameObject(ObjectGuid.Empty, false, go);

                    break;
                case GameObjectsIds.PORTCULLIS_ACTIVE:
                    _goPortcullisActive = go.GUID;

                    if (GetBossState(DataTypes.GYTH) == EncounterState.Done)
                        HandleGameObject(ObjectGuid.Empty, true, go);

                    break;
                case GameObjectsIds.PORTCULLIS_TOBOSSROOMS:
                    _goPortcullisTobossrooms = go.GUID;

                    if (GetBossState(DataTypes.GYTH) == EncounterState.Done)
                        HandleGameObject(ObjectGuid.Empty, true, go);

                    break;
            }
        }

        public override bool SetBossState(uint type, EncounterState state)
        {
            if (!base.SetBossState(type, state))
                return false;

            switch (type)
            {
                case DataTypes.HIGHLORD_OMOKK:
                case DataTypes.SHADOW_HUNTER_VOSHGAJIN:
                case DataTypes.WARMASTER_VOONE:
                case DataTypes.MOTHER_SMOLDERWEB:
                case DataTypes.UROK_DOOMHOWL:
                case DataTypes.QUARTERMASTER_ZIGRIS:
                case DataTypes.GIZRUL_THE_SLAVENER:
                case DataTypes.HALYCON:
                case DataTypes.OVERLORD_WYRMTHALAK:
                case DataTypes.PYROGAURD_EMBERSEER:
                case DataTypes.WARCHIEF_REND_BLACKHAND:
                case DataTypes.GYTH:
                case DataTypes.THE_BEAST:
                case DataTypes.GENERAL_DRAKKISATH:
                case DataTypes.DRAGONSPIRE_ROOM:
                    break;
            }

            return true;
        }

        public override void ProcessEvent(WorldObject obj, uint eventId, WorldObject invoker)
        {
            switch (eventId)
            {
                case BrsMiscConst.EVENT_PYROGUARD_EMBERSEER:
                    if (GetBossState(DataTypes.PYROGAURD_EMBERSEER) == EncounterState.NotStarted)
                    {
                        var emberseer = Instance.GetCreature(_pyroguardEmberseer);

                        if (emberseer)
                            emberseer.AI.SetData(1, 1);
                    }

                    break;
                case BrsMiscConst.EVENT_UROK_DOOMHOWL:
                    if (GetBossState(CreaturesIds.UROK_DOOMHOWL) == EncounterState.NotStarted) { }

                    break;
            }
        }

        public override void SetData(uint type, uint data)
        {
            switch (type)
            {
                case BrsMiscConst.AREATRIGGER:
                    if (data == BrsMiscConst.AREATRIGGER_DRAGONSPIRE_HALL)
                        if (GetBossState(DataTypes.DRAGONSPIRE_ROOM) != EncounterState.Done)
                            _events.ScheduleEvent(EventIds.DARGONSPIRE_ROOM_STORE, TimeSpan.FromSeconds(1));

                    break;
                case DataTypes.BLACKHAND_INCARCERATOR:
                    foreach (var itr in _incarceratorList)
                    {
                        var creature = Instance.GetCreature(itr);

                        if (creature)
                            creature.Respawn();
                    }

                    break;
            }
        }

        public override ObjectGuid GetGuidData(uint type)
        {
            switch (type)
            {
                case DataTypes.HIGHLORD_OMOKK:
                    return _highlordOmokk;
                case DataTypes.SHADOW_HUNTER_VOSHGAJIN:
                    return _shadowHunterVoshgajin;
                case DataTypes.WARMASTER_VOONE:
                    return _warMasterVoone;
                case DataTypes.MOTHER_SMOLDERWEB:
                    return _motherSmolderweb;
                case DataTypes.UROK_DOOMHOWL:
                    return _urokDoomhowl;
                case DataTypes.QUARTERMASTER_ZIGRIS:
                    return _quartermasterZigris;
                case DataTypes.GIZRUL_THE_SLAVENER:
                    return _gizrultheSlavener;
                case DataTypes.HALYCON:
                    return _halycon;
                case DataTypes.OVERLORD_WYRMTHALAK:
                    return _overlordWyrmthalak;
                case DataTypes.PYROGAURD_EMBERSEER:
                    return _pyroguardEmberseer;
                case DataTypes.WARCHIEF_REND_BLACKHAND:
                    return _warchiefRendBlackhand;
                case DataTypes.GYTH:
                    return _gyth;
                case DataTypes.THE_BEAST:
                    return _theBeast;
                case DataTypes.GENERAL_DRAKKISATH:
                    return _generalDrakkisath;
                case DataTypes.SCARSHIELD_INFILTRATOR:
                    return _scarshieldInfiltrator;
                case GameObjectsIds.EMBERSEER_IN:
                    return _goEmberseerin;
                case GameObjectsIds.DOORS:
                    return _goDoors;
                case GameObjectsIds.EMBERSEER_OUT:
                    return _goEmberseerout;
                case GameObjectsIds.HALL_RUNE1:
                    return _goRoomrunes[0];
                case GameObjectsIds.HALL_RUNE2:
                    return _goRoomrunes[1];
                case GameObjectsIds.HALL_RUNE3:
                    return _goRoomrunes[2];
                case GameObjectsIds.HALL_RUNE4:
                    return _goRoomrunes[3];
                case GameObjectsIds.HALL_RUNE5:
                    return _goRoomrunes[4];
                case GameObjectsIds.HALL_RUNE6:
                    return _goRoomrunes[5];
                case GameObjectsIds.HALL_RUNE7:
                    return _goRoomrunes[6];
                case GameObjectsIds.EMBERSEER_RUNE1:
                    return _goEmberseerrunes[0];
                case GameObjectsIds.EMBERSEER_RUNE2:
                    return _goEmberseerrunes[1];
                case GameObjectsIds.EMBERSEER_RUNE3:
                    return _goEmberseerrunes[2];
                case GameObjectsIds.EMBERSEER_RUNE4:
                    return _goEmberseerrunes[3];
                case GameObjectsIds.EMBERSEER_RUNE5:
                    return _goEmberseerrunes[4];
                case GameObjectsIds.EMBERSEER_RUNE6:
                    return _goEmberseerrunes[5];
                case GameObjectsIds.EMBERSEER_RUNE7:
                    return _goEmberseerrunes[6];
                case GameObjectsIds.PORTCULLIS_ACTIVE:
                    return _goPortcullisActive;
                case GameObjectsIds.PORTCULLIS_TOBOSSROOMS:
                    return _goPortcullisTobossrooms;
            }

            return ObjectGuid.Empty;
        }

        public override void Update(uint diff)
        {
            _events.Update(diff);

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case EventIds.DARGONSPIRE_ROOM_STORE:
                        Dragonspireroomstore();
                        _events.ScheduleEvent(EventIds.DARGONSPIRE_ROOM_CHECK, TimeSpan.FromSeconds(3));

                        break;
                    case EventIds.DARGONSPIRE_ROOM_CHECK:
                        Dragonspireroomcheck();

                        if (GetBossState(DataTypes.DRAGONSPIRE_ROOM) != EncounterState.Done)
                            _events.ScheduleEvent(EventIds.DARGONSPIRE_ROOM_CHECK, TimeSpan.FromSeconds(3));

                        break;
                }
            });
        }

        private void Dragonspireroomstore()
        {
            for (byte i = 0; i < 7; ++i)
            {
                // Refresh the creature list
                _runecreaturelist[i].Clear();

                var rune = Instance.GetGameObject(_goRoomrunes[i]);

                if (rune)
                    for (byte j = 0; j < 3; ++j)
                    {
                        var creatureList = rune.GetCreatureListWithEntryInGrid(BrsMiscConst.DragonspireMobs[j], 15.0f);

                        foreach (var creature in creatureList)
                            if (creature)
                                _runecreaturelist[i].Add(creature.GUID);
                    }
            }
        }

        private void Dragonspireroomcheck()
        {
            Creature mob = null;
            GameObject rune = null;

            for (byte i = 0; i < 7; ++i)
            {
                var mobAlive = false;
                rune = Instance.GetGameObject(_goRoomrunes[i]);

                if (!rune)
                    continue;

                if (rune.GoState == GameObjectState.Active)
                    foreach (var guid in _runecreaturelist[i])
                    {
                        mob = Instance.GetCreature(guid);

                        if (mob && mob.IsAlive)
                            mobAlive = true;
                    }

                if (!mobAlive &&
                    rune.GoState == GameObjectState.Active)
                {
                    HandleGameObject(ObjectGuid.Empty, false, rune);

                    switch (rune.Entry)
                    {
                        case GameObjectsIds.HALL_RUNE1:
                            SetBossState(DataTypes.HALL_RUNE1, EncounterState.Done);

                            break;
                        case GameObjectsIds.HALL_RUNE2:
                            SetBossState(DataTypes.HALL_RUNE2, EncounterState.Done);

                            break;
                        case GameObjectsIds.HALL_RUNE3:
                            SetBossState(DataTypes.HALL_RUNE3, EncounterState.Done);

                            break;
                        case GameObjectsIds.HALL_RUNE4:
                            SetBossState(DataTypes.HALL_RUNE4, EncounterState.Done);

                            break;
                        case GameObjectsIds.HALL_RUNE5:
                            SetBossState(DataTypes.HALL_RUNE5, EncounterState.Done);

                            break;
                        case GameObjectsIds.HALL_RUNE6:
                            SetBossState(DataTypes.HALL_RUNE6, EncounterState.Done);

                            break;
                        case GameObjectsIds.HALL_RUNE7:
                            SetBossState(DataTypes.HALL_RUNE7, EncounterState.Done);

                            break;
                    }
                }
            }

            if (GetBossState(DataTypes.HALL_RUNE1) == EncounterState.Done &&
                GetBossState(DataTypes.HALL_RUNE2) == EncounterState.Done &&
                GetBossState(DataTypes.HALL_RUNE3) == EncounterState.Done &&
                GetBossState(DataTypes.HALL_RUNE4) == EncounterState.Done &&
                GetBossState(DataTypes.HALL_RUNE5) == EncounterState.Done &&
                GetBossState(DataTypes.HALL_RUNE6) == EncounterState.Done &&
                GetBossState(DataTypes.HALL_RUNE7) == EncounterState.Done)
            {
                SetBossState(DataTypes.DRAGONSPIRE_ROOM, EncounterState.Done);
                var door1 = Instance.GetGameObject(_goEmberseerin);

                if (door1)
                    HandleGameObject(ObjectGuid.Empty, true, door1);

                var door2 = Instance.GetGameObject(_goDoors);

                if (door2)
                    HandleGameObject(ObjectGuid.Empty, true, door2);
            }
        }
    }
}

[Script]
internal class AtDragonspireHall : ScriptObjectAutoAddDBBound, IAreaTriggerOnTrigger
{
    public AtDragonspireHall() : base("at_dragonspire_hall") { }

    public bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
    {
        if (player && player.IsAlive)
        {
            var instance = player.InstanceScript;

            if (instance != null)
            {
                instance.SetData(BrsMiscConst.AREATRIGGER, BrsMiscConst.AREATRIGGER_DRAGONSPIRE_HALL);

                return true;
            }
        }

        return false;
    }
}

[Script]
internal class AtBlackrockStadium : ScriptObjectAutoAddDBBound, IAreaTriggerOnTrigger
{
    public AtBlackrockStadium() : base("at_blackrock_stadium") { }

    public bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
    {
        if (player && player.IsAlive)
        {
            var instance = player.InstanceScript;

            if (instance == null)
                return false;

            var rend = player.FindNearestCreature(CreaturesIds.WARCHIEF_REND_BLACKHAND, 50.0f);

            if (rend)
            {
                rend.AI.SetData(BrsMiscConst.AREATRIGGER, BrsMiscConst.AREATRIGGER_BLACKROCK_STADIUM);

                return true;
            }
        }

        return false;
    }
}

[Script]
internal class AtNearbyScarshieldInfiltrator : ScriptObjectAutoAddDBBound, IAreaTriggerOnTrigger
{
    public AtNearbyScarshieldInfiltrator() : base("at_nearby_scarshield_infiltrator") { }

    public bool OnTrigger(Player player, AreaTriggerRecord at)
    {
        if (player.IsAlive)
        {
            var instance = player.InstanceScript;

            if (instance != null)
            {
                var infiltrator = ObjectAccessor.GetCreature(player, instance.GetGuidData(DataTypes.SCARSHIELD_INFILTRATOR));

                if (infiltrator)
                {
                    if (player.Level >= 57)
                        infiltrator.AI.SetData(1, 1);
                    else if (infiltrator.Entry == CreaturesIds.SCARSHIELD_INFILTRATOR)
                        infiltrator.AI.Talk(0, player);

                    return true;
                }
            }
        }

        return false;
    }
}