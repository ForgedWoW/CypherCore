﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Framework.Constants;
using Framework.Database;
using Framework.GameMath;
using Game.AI;
using Game.BattleGrounds;
using Game.Collision;
using Game.DataStorage;
using Game.Groups;
using Game.Loots;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;

namespace Game.Entities
{
    public class GameObject : WorldObject
    {
        private class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
        {
            public GameObjectFieldData GameObjectMask = new();
            public ObjectFieldData ObjectMask = new();
            public GameObject Owner;

            public ValuesUpdateForPlayerWithMaskSender(GameObject owner)
            {
                Owner = owner;
            }

            public void Invoke(Player player)
            {
                UpdateData udata = new(Owner.GetMapId());

                Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetUpdateMask(), GameObjectMask.GetUpdateMask(), player);

                udata.BuildPacket(out UpdateObject packet);
                player.SendPacket(packet);
            }
        }

        public GameObject() : base(false)
        {
            ObjectTypeMask |= TypeMask.GameObject;
            ObjectTypeId = TypeId.GameObject;

            UpdateFlag.Stationary = true;
            UpdateFlag.Rotation = true;

            _respawnDelayTime = 300;
            _despawnDelay = 0;
            _lootState = LootState.NotReady;
            _spawnedByDefault = true;

            ResetLootMode(); // restore default loot mode
            StationaryPosition = new Position();

            _gameObjectData = new GameObjectFieldData();
        }

        public override void Dispose()
        {
            _AI = null;
            _model = null;

            base.Dispose();
        }

        public bool AIM_Initialize()
        {
            _AI = AISelector.SelectGameObjectAI(this);

            if (_AI == null)
                return false;

            _AI.InitializeAI();

            return true;
        }

        public string GetAIName()
        {
            GameObjectTemplate got = Global.ObjectMgr.GetGameObjectTemplate(GetEntry());

            if (got != null)
                return got.AIName;

            return "";
        }

        public override void CleanupsBeforeDelete(bool finalCleanup)
        {
            base.CleanupsBeforeDelete(finalCleanup);

            RemoveFromOwner();
        }

        public override void AddToWorld()
        {
            //- Register the gameobject for Guid lookup
            if (!IsInWorld)
            {
                ZoneScript?.OnGameObjectCreate(this);

                GetMap().GetObjectsStore().Add(GetGUID(), this);

                if (_spawnId != 0)
                    GetMap().GetGameObjectBySpawnIdStore().Add(_spawnId, this);

                // The State can be changed after GameObject.Create but before GameObject.AddToWorld
                bool toggledState = GetGoType() == GameObjectTypes.Chest ? GetLootState() == LootState.Ready : (GetGoState() == GameObjectState.Ready || IsTransport());

                if (_model != null)
                {
                    Transport trans = ToTransport();

                    if (trans)
                        trans.SetDelayedAddModelToMap();
                    else
                        GetMap().InsertGameObjectModel(_model);
                }

                EnableCollision(toggledState);
                base.AddToWorld();
            }
        }

        public override void RemoveFromWorld()
        {
            //- Remove the gameobject from the accessor
            if (IsInWorld)
            {
                ZoneScript?.OnGameObjectRemove(this);

                RemoveFromOwner();

                if (_model != null)
                    if (GetMap().ContainsGameObjectModel(_model))
                        GetMap().RemoveGameObjectModel(_model);

                // If linked trap exists, despawn it
                GameObject linkedTrap = GetLinkedTrap();

                linkedTrap?.DespawnOrUnsummon();

                base.RemoveFromWorld();

                if (_spawnId != 0)
                    GetMap().GetGameObjectBySpawnIdStore().Remove(_spawnId, this);

                GetMap().GetObjectsStore().Remove(GetGUID());
            }
        }

        public static GameObject CreateGameObject(uint entry, Map map, Position pos, Quaternion rotation, uint animProgress, GameObjectState goState, uint artKit = 0)
        {
            GameObjectTemplate goInfo = Global.ObjectMgr.GetGameObjectTemplate(entry);

            if (goInfo == null)
                return null;

            GameObject go = new();

            if (!go.Create(entry, map, pos, rotation, animProgress, goState, artKit, false, 0))
                return null;

            return go;
        }

        public static GameObject CreateGameObjectFromDB(ulong spawnId, Map map, bool addToMap = true)
        {
            GameObject go = new();

            if (!go.LoadFromDB(spawnId, map, addToMap))
                return null;

            return go;
        }

        public override void Update(uint diff)
        {
            Events.Update(diff);

            base.Update(diff);

            if (GetAI() != null)
                GetAI().UpdateAI(diff);
            else if (!AIM_Initialize())
                Log.outError(LogFilter.Server, "Could not initialize GameObjectAI");

            if (_despawnDelay != 0)
            {
                if (_despawnDelay > diff)
                {
                    _despawnDelay -= diff;
                }
                else
                {
                    _despawnDelay = 0;
                    DespawnOrUnsummon(TimeSpan.FromMilliseconds(0), _despawnRespawnTime);
                }
            }

            _goTypeImpl?.Update(diff);

            if (_perPlayerState != null)
                foreach (var (guid, playerState) in _perPlayerState.ToList())
                {
                    if (playerState.ValidUntil > GameTime.GetSystemTime())
                        continue;

                    Player seer = Global.ObjAccessor.GetPlayer(this, guid);
                    bool needsStateUpdate = playerState.State != GetGoState();
                    bool despawned = playerState.Despawned;

                    _perPlayerState.Remove(guid);

                    if (seer)
                    {
                        if (despawned)
                        {
                            seer.UpdateVisibilityOf(this);
                        }
                        else if (needsStateUpdate)
                        {
                            ObjectFieldData objMask = new();
                            GameObjectFieldData goMask = new();
                            goMask.MarkChanged(_gameObjectData.State);

                            UpdateData udata = new(GetMapId());
                            BuildValuesUpdateForPlayerWithMask(udata, objMask.GetUpdateMask(), goMask.GetUpdateMask(), seer);
                            udata.BuildPacket(out UpdateObject packet);
                            seer.SendPacket(packet);
                        }
                    }
                }

            switch (_lootState)
            {
                case LootState.NotReady:
                    {
                        switch (GetGoType())
                        {
                            case GameObjectTypes.Trap:
                                {
                                    // Arming Time for GAMEOBJECT_TYPE_TRAP (6)
                                    GameObjectTemplate goInfo = GetGoInfo();

                                    // Bombs
                                    Unit owner = GetOwner();

                                    if (goInfo.Trap.charges == 2)
                                        _cooldownTime = GameTime.GetGameTimeMS() + 10 * Time.InMilliseconds; // Hardcoded tooltip value
                                    else if (owner)
                                        if (owner.IsInCombat())
                                            _cooldownTime = GameTime.GetGameTimeMS() + goInfo.Trap.startDelay * Time.InMilliseconds;

                                    _lootState = LootState.Ready;

                                    break;
                                }
                            case GameObjectTypes.FishingNode:
                                {
                                    // fishing code (bobber ready)
                                    if (GameTime.GetGameTime() > _respawnTime - 5)
                                    {
                                        // splash bobber (bobber ready now)
                                        Unit caster = GetOwner();

                                        if (caster != null &&
                                            caster.IsTypeId(TypeId.Player))
                                        {
                                            SetGoState(GameObjectState.Active);
                                            ReplaceAllFlags(GameObjectFlags.NoDespawn);

                                            UpdateData udata = new(caster.GetMapId());
                                            UpdateObject packet;
                                            BuildValuesUpdateBlockForPlayer(udata, caster.ToPlayer());
                                            udata.BuildPacket(out packet);
                                            caster.ToPlayer().SendPacket(packet);

                                            SendCustomAnim(GetGoAnimProgress());
                                        }

                                        _lootState = LootState.Ready; // can be successfully open with some chance
                                    }

                                    return;
                                }
                            case GameObjectTypes.Chest:
                                if (_restockTime > GameTime.GetGameTime())
                                    return;

                                // If there is no restock timer, or if the restock timer passed, the chest becomes ready to loot
                                _restockTime = 0;
                                _lootState = LootState.Ready;
                                ClearLoot();
                                UpdateDynamicFlagsForNearbyPlayers();

                                break;
                            default:
                                _lootState = LootState.Ready; // for other GOis same switched without delay to GO_READY

                                break;
                        }
                    }

                    goto case LootState.Ready;
                case LootState.Ready:
                    {
                        if (_respawnCompatibilityMode)
                            if (_respawnTime > 0) // timer on
                            {
                                long now = GameTime.GetGameTime();

                                if (_respawnTime <= now) // timer expired
                                {
                                    ObjectGuid dbtableHighGuid = ObjectGuid.Create(HighGuid.GameObject, GetMapId(), GetEntry(), _spawnId);
                                    long linkedRespawntime = GetMap().GetLinkedRespawnTime(dbtableHighGuid);

                                    if (linkedRespawntime != 0) // Can't respawn, the master is dead
                                    {
                                        ObjectGuid targetGuid = Global.ObjectMgr.GetLinkedRespawnGuid(dbtableHighGuid);

                                        if (targetGuid == dbtableHighGuid) // if linking self, never respawn (check delayed to next day)
                                            SetRespawnTime(Time.Week);
                                        else
                                            _respawnTime = (now > linkedRespawntime ? now : linkedRespawntime) + RandomHelper.IRand(5, Time.Minute); // else copy Time from master and add a little

                                        SaveRespawnTime();

                                        return;
                                    }

                                    _respawnTime = 0;
                                    _SkillupList.Clear();
                                    _usetimes = 0;

                                    switch (GetGoType())
                                    {
                                        case GameObjectTypes.FishingNode: //  can't fish now
                                            {
                                                Unit caster = GetOwner();

                                                if (caster != null &&
                                                    caster.IsTypeId(TypeId.Player))
                                                {
                                                    caster.ToPlayer().RemoveGameObject(this, false);
                                                    caster.ToPlayer().SendPacket(new FishEscaped());
                                                }

                                                // can be delete
                                                _lootState = LootState.JustDeactivated;

                                                return;
                                            }
                                        case GameObjectTypes.Door:
                                        case GameObjectTypes.Button:
                                            //we need to open doors if they are closed (add there another condition if this code breaks some usage, but it need to be here for Battlegrounds)
                                            if (GetGoState() != GameObjectState.Ready)
                                                ResetDoorOrButton();

                                            break;
                                        case GameObjectTypes.FishingHole:
                                            // Initialize a new max fish Count on respawn
                                            _goValue.FishingHole.MaxOpens = RandomHelper.URand(GetGoInfo().FishingHole.minRestock, GetGoInfo().FishingHole.maxRestock);

                                            break;
                                        default:
                                            break;
                                    }

                                    if (!_spawnedByDefault) // despawn timer
                                    {
                                        // can be despawned or destroyed
                                        SetLootState(LootState.JustDeactivated);

                                        return;
                                    }

                                    // Call AI Reset (required for example in SmartAI to clear one Time events)
                                    GetAI()?.Reset();

                                    // respawn timer
                                    uint poolid = GetGameObjectData() != null ? GetGameObjectData().poolId : 0;

                                    if (poolid != 0)
                                        Global.PoolMgr.UpdatePool<GameObject>(GetMap().GetPoolData(), poolid, GetSpawnId());
                                    else
                                        GetMap().AddToMap(this);
                                }
                            }

                        // Set respawn timer
                        if (!_respawnCompatibilityMode &&
                            _respawnTime > 0)
                            SaveRespawnTime();

                        if (IsSpawned())
                        {
                            GameObjectTemplate goInfo = GetGoInfo();
                            uint max_charges;

                            if (goInfo.type == GameObjectTypes.Trap)
                            {
                                if (GameTime.GetGameTimeMS() < _cooldownTime)
                                    break;

                                // Type 2 (bomb) does not need to be triggered by a unit and despawns after casting its spell.
                                if (goInfo.Trap.charges == 2)
                                {
                                    SetLootState(LootState.Activated);

                                    break;
                                }

                                // Type 0 despawns after being triggered, Type 1 does not.
                                // @todo This is activation radius. Casting radius must be selected from spell 
                                float radius;

                                if (goInfo.Trap.radius == 0f)
                                {
                                    // Battlegroundgameobjects have data2 == 0 && data5 == 3
                                    if (goInfo.Trap.cooldown != 3)
                                        break;

                                    radius = 3.0f;
                                }
                                else
                                {
                                    radius = goInfo.Trap.radius / 2.0f;
                                }

                                Unit target;

                                // @todo this hack with search required until GO casting not implemented
                                if (GetOwner() != null)
                                {
                                    // Hunter trap: Search units which are unfriendly to the trap's owner
                                    var checker = new NearestAttackableNoTotemUnitInObjectRangeCheck(this, radius);
                                    var searcher = new UnitLastSearcher(this, checker);
                                    Cell.VisitAllObjects(this, searcher, radius);
                                    target = searcher.GetTarget();
                                }
                                else
                                {
                                    // Environmental trap: Any player
                                    var check = new AnyPlayerInObjectRangeCheck(this, radius);
                                    var searcher = new PlayerSearcher(this, check);
                                    Cell.VisitWorldObjects(this, searcher, radius);
                                    target = searcher.GetTarget();
                                }

                                if (target)
                                    SetLootState(LootState.Activated, target);
                            }
                            else if (goInfo.type == GameObjectTypes.CapturePoint)
                            {
                                bool hordeCapturing = _goValue.CapturePoint.State == BattlegroundCapturePointState.ContestedHorde;
                                bool allianceCapturing = _goValue.CapturePoint.State == BattlegroundCapturePointState.ContestedAlliance;

                                if (hordeCapturing || allianceCapturing)
                                {
                                    if (_goValue.CapturePoint.AssaultTimer <= diff)
                                    {
                                        _goValue.CapturePoint.State = hordeCapturing ? BattlegroundCapturePointState.HordeCaptured : BattlegroundCapturePointState.AllianceCaptured;

                                        if (hordeCapturing)
                                        {
                                            _goValue.CapturePoint.State = BattlegroundCapturePointState.HordeCaptured;
                                            BattlegroundMap map = GetMap().ToBattlegroundMap();

                                            if (map != null)
                                            {
                                                Battleground bg = map.GetBG();

                                                if (bg != null)
                                                {
                                                    if (goInfo.CapturePoint.CaptureEventHorde != 0)
                                                        GameEvents.Trigger(goInfo.CapturePoint.CaptureEventHorde, this, this);

                                                    bg.SendBroadcastText(GetGoInfo().CapturePoint.CaptureBroadcastHorde, ChatMsg.BgSystemHorde);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            _goValue.CapturePoint.State = BattlegroundCapturePointState.AllianceCaptured;
                                            BattlegroundMap map = GetMap().ToBattlegroundMap();

                                            if (map != null)
                                            {
                                                Battleground bg = map.GetBG();

                                                if (bg != null)
                                                {
                                                    if (goInfo.CapturePoint.CaptureEventAlliance != 0)
                                                        GameEvents.Trigger(goInfo.CapturePoint.CaptureEventAlliance, this, this);

                                                    bg.SendBroadcastText(GetGoInfo().CapturePoint.CaptureBroadcastAlliance, ChatMsg.BgSystemAlliance);
                                                }
                                            }
                                        }

                                        _goValue.CapturePoint.LastTeamCapture = hordeCapturing ? TeamId.Horde : TeamId.Alliance;
                                        UpdateCapturePoint();
                                    }
                                    else
                                    {
                                        _goValue.CapturePoint.AssaultTimer -= diff;
                                    }
                                }
                            }
                            else if ((max_charges = goInfo.GetCharges()) != 0)
                            {
                                if (_usetimes >= max_charges)
                                {
                                    _usetimes = 0;
                                    SetLootState(LootState.JustDeactivated); // can be despawned or destroyed
                                }
                            }
                        }

                        break;
                    }
                case LootState.Activated:
                    {
                        switch (GetGoType())
                        {
                            case GameObjectTypes.Door:
                            case GameObjectTypes.Button:
                                if (_cooldownTime != 0 &&
                                    GameTime.GetGameTimeMS() >= _cooldownTime)
                                    ResetDoorOrButton();

                                break;
                            case GameObjectTypes.Goober:
                                if (GameTime.GetGameTimeMS() >= _cooldownTime)
                                {
                                    RemoveFlag(GameObjectFlags.InUse);

                                    SetLootState(LootState.JustDeactivated);
                                    _cooldownTime = 0;
                                }

                                break;
                            case GameObjectTypes.Chest:
                                loot?.Update();

                                foreach (var (_, loot) in _personalLoot)
                                    loot.Update();

                                // Non-consumable chest was partially looted and restock Time passed, restock all loot now
                                if (GetGoInfo().Chest.consumable == 0 &&
                                    GetGoInfo().Chest.chestRestockTime != 0 &&
                                    GameTime.GetGameTime() >= _restockTime)
                                {
                                    _restockTime = 0;
                                    _lootState = LootState.Ready;
                                    ClearLoot();
                                    UpdateDynamicFlagsForNearbyPlayers();
                                }

                                break;
                            case GameObjectTypes.Trap:
                                {
                                    GameObjectTemplate goInfo = GetGoInfo();
                                    Unit target = Global.ObjAccessor.GetUnit(this, _lootStateUnitGUID);

                                    if (goInfo.Trap.charges == 2 &&
                                        goInfo.Trap.spell != 0)
                                    {
                                        //todo NULL Target won't work for Target Type 1
                                        CastSpell(null, goInfo.Trap.spell);
                                        SetLootState(LootState.JustDeactivated);
                                    }
                                    else if (target)
                                    {
                                        // Some traps do not have a spell but should be triggered
                                        CastSpellExtraArgs args = new();
                                        args.SetOriginalCaster(GetOwnerGUID());

                                        if (goInfo.Trap.spell != 0)
                                            CastSpell(target, goInfo.Trap.spell, args);

                                        // Template value or 4 seconds
                                        _cooldownTime = (GameTime.GetGameTimeMS() + (goInfo.Trap.cooldown != 0 ? goInfo.Trap.cooldown : 4u)) * Time.InMilliseconds;

                                        if (goInfo.Trap.charges == 1)
                                            SetLootState(LootState.JustDeactivated);
                                        else if (goInfo.Trap.charges == 0)
                                            SetLootState(LootState.Ready);

                                        // Battleground gameobjects have data2 == 0 && data5 == 3
                                        if (goInfo.Trap.radius == 0 &&
                                            goInfo.Trap.cooldown == 3)
                                        {
                                            Player player = target.ToPlayer();

                                            if (player)
                                            {
                                                Battleground bg = player.GetBattleground();

                                                if (bg)
                                                    bg.HandleTriggerBuff(GetGUID());
                                            }
                                        }
                                    }

                                    break;
                                }
                            default:
                                break;
                        }

                        break;
                    }
                case LootState.JustDeactivated:
                    {
                        // If nearby linked trap exists, despawn it
                        GameObject linkedTrap = GetLinkedTrap();

                        if (linkedTrap)
                            linkedTrap.DespawnOrUnsummon();

                        //if Gameobject should cast spell, then this, but some GOs (Type = 10) should be destroyed
                        if (GetGoType() == GameObjectTypes.Goober)
                        {
                            uint spellId = GetGoInfo().Goober.spell;

                            if (spellId != 0)
                            {
                                foreach (var id in _unique_users)
                                {
                                    // _unique_users can contain only player GUIDs
                                    Player owner = Global.ObjAccessor.GetPlayer(this, id);

                                    owner?.CastSpell(owner, spellId, false);
                                }

                                _unique_users.Clear();
                                _usetimes = 0;
                            }

                            // Only goobers with a lock Id or a reset Time may reset their go State
                            if (GetGoInfo().GetLockId() != 0 ||
                                GetGoInfo().GetAutoCloseTime() != 0)
                                SetGoState(GameObjectState.Ready);

                            //any return here in case Battleground traps
                            GameObjectOverride goOverride = GetGameObjectOverride();

                            if (goOverride != null &&
                                goOverride.Flags.HasFlag(GameObjectFlags.NoDespawn))
                                return;
                        }

                        ClearLoot();

                        // Do not delete chests or goobers that are not consumed on loot, while still allowing them to despawn when they expire if summoned
                        bool isSummonedAndExpired = (GetOwner() != null || GetSpellId() != 0) && _respawnTime == 0;

                        if ((GetGoType() == GameObjectTypes.Chest || GetGoType() == GameObjectTypes.Goober) &&
                            !GetGoInfo().IsDespawnAtAction() &&
                            !isSummonedAndExpired)
                        {
                            if (GetGoType() == GameObjectTypes.Chest &&
                                GetGoInfo().Chest.chestRestockTime > 0)
                            {
                                // Start restock timer when the chest is fully looted
                                _restockTime = GameTime.GetGameTime() + GetGoInfo().Chest.chestRestockTime;
                                SetLootState(LootState.NotReady);
                                UpdateDynamicFlagsForNearbyPlayers();
                            }
                            else
                            {
                                SetLootState(LootState.Ready);
                            }

                            UpdateObjectVisibility();

                            return;
                        }
                        else if (!GetOwnerGUID().IsEmpty() ||
                                 GetSpellId() != 0)
                        {
                            SetRespawnTime(0);
                            Delete();

                            return;
                        }

                        SetLootState(LootState.NotReady);

                        //burning Flags in some Battlegrounds, if you find better condition, just add it
                        if (GetGoInfo().IsDespawnAtAction() ||
                            GetGoAnimProgress() > 0)
                        {
                            SendGameObjectDespawn();
                            //reset Flags
                            GameObjectOverride goOverride = GetGameObjectOverride();

                            if (goOverride != null)
                                ReplaceAllFlags(goOverride.Flags);
                        }

                        if (_respawnDelayTime == 0)
                            return;

                        if (!_spawnedByDefault)
                        {
                            _respawnTime = 0;

                            if (_spawnId != 0)
                                UpdateObjectVisibilityOnDestroy();
                            else
                                Delete();

                            return;
                        }

                        uint respawnDelay = _respawnDelayTime;
                        uint scalingMode = WorldConfig.GetUIntValue(WorldCfg.RespawnDynamicMode);

                        if (scalingMode != 0)
                            GetMap().ApplyDynamicModeRespawnScaling(this, _spawnId, ref respawnDelay, scalingMode);

                        _respawnTime = GameTime.GetGameTime() + respawnDelay;

                        // if option not set then object will be saved at grid unload
                        // Otherwise just save respawn Time to map object memory
                        SaveRespawnTime();

                        if (_respawnCompatibilityMode)
                            UpdateObjectVisibilityOnDestroy();
                        else
                            AddObjectToRemoveList();

                        break;
                    }
            }
        }

        public GameObjectOverride GetGameObjectOverride()
        {
            if (_spawnId != 0)
            {
                GameObjectOverride goOverride = Global.ObjectMgr.GetGameObjectOverride(_spawnId);

                if (goOverride != null)
                    return goOverride;
            }

            return _goTemplateAddon;
        }

        public void Refresh()
        {
            // not refresh despawned not casted GO (despawned casted GO destroyed in all cases anyway)
            if (_respawnTime > 0 && _spawnedByDefault)
                return;

            if (IsSpawned())
                GetMap().AddToMap(this);
        }

        public void AddUniqueUse(Player player)
        {
            AddUse();
            _unique_users.Add(player.GetGUID());
        }

        public void DespawnOrUnsummon(TimeSpan delay = default, TimeSpan forceRespawnTime = default)
        {
            if (delay > TimeSpan.Zero)
            {
                if (_despawnDelay == 0 ||
                    _despawnDelay > delay.TotalMilliseconds)
                {
                    _despawnDelay = (uint)delay.TotalMilliseconds;
                    _despawnRespawnTime = forceRespawnTime;
                }
            }
            else
            {
                if (_goData != null)
                {
                    uint respawnDelay = (uint)((forceRespawnTime > TimeSpan.Zero) ? forceRespawnTime.TotalSeconds : _respawnDelayTime);
                    SaveRespawnTime(respawnDelay);
                }

                Delete();
            }
        }

        public void Delete()
        {
            SetLootState(LootState.NotReady);
            RemoveFromOwner();

            if (_goInfo.type == GameObjectTypes.CapturePoint)
                SendMessageToSet(new CapturePointRemoved(GetGUID()), true);

            SendGameObjectDespawn();

            if (_goInfo.type != GameObjectTypes.Transport)
                SetGoState(GameObjectState.Ready);

            GameObjectOverride goOverride = GetGameObjectOverride();

            if (goOverride != null)
                ReplaceAllFlags(goOverride.Flags);

            uint poolid = GetGameObjectData() != null ? GetGameObjectData().poolId : 0;

            if (poolid != 0)
                Global.PoolMgr.UpdatePool<GameObject>(GetMap().GetPoolData(), poolid, GetSpawnId());
            else
                AddObjectToRemoveList();
        }

        public void SendGameObjectDespawn()
        {
            GameObjectDespawn packet = new();
            packet.ObjectGUID = GetGUID();
            SendMessageToSet(packet, true);
        }

        public Loot GetFishLoot(Player lootOwner)
        {
            uint defaultzone = 1;

            Loot fishLoot = new(GetMap(), GetGUID(), LootType.Fishing, null);

            uint areaId = GetAreaId();
            AreaTableRecord areaEntry;

            while ((areaEntry = CliDB.AreaTableStorage.LookupByKey(areaId)) != null)
            {
                fishLoot.FillLoot(areaId, LootStorage.Fishing, lootOwner, true, true);

                if (!fishLoot.IsLooted())
                    break;

                areaId = areaEntry.ParentAreaID;
            }

            if (fishLoot.IsLooted())
                fishLoot.FillLoot(defaultzone, LootStorage.Fishing, lootOwner, true, true);

            return fishLoot;
        }

        public Loot GetFishLootJunk(Player lootOwner)
        {
            uint defaultzone = 1;

            Loot fishLoot = new(GetMap(), GetGUID(), LootType.FishingJunk, null);

            uint areaId = GetAreaId();
            AreaTableRecord areaEntry;

            while ((areaEntry = CliDB.AreaTableStorage.LookupByKey(areaId)) != null)
            {
                fishLoot.FillLoot(areaId, LootStorage.Fishing, lootOwner, true, true, LootModes.JunkFish);

                if (!fishLoot.IsLooted())
                    break;

                areaId = areaEntry.ParentAreaID;
            }

            if (fishLoot.IsLooted())
                fishLoot.FillLoot(defaultzone, LootStorage.Fishing, lootOwner, true, true, LootModes.JunkFish);

            return fishLoot;
        }

        public void SaveToDB()
        {
            // this should only be used when the gameobject has already been loaded
            // preferably after adding to map, because mapid may not be valid otherwise
            GameObjectData data = Global.ObjectMgr.GetGameObjectData(_spawnId);

            if (data == null)
            {
                Log.outError(LogFilter.Maps, "GameObject.SaveToDB failed, cannot get gameobject _data!");

                return;
            }

            uint mapId = GetMapId();
            ITransport transport = GetTransport();

            if (transport != null)
                if (transport.GetMapIdForSpawning() >= 0)
                    mapId = (uint)transport.GetMapIdForSpawning();

            SaveToDB(mapId, data.SpawnDifficulties);
        }

        public void SaveToDB(uint mapid, List<Difficulty> spawnDifficulties)
        {
            GameObjectTemplate goI = GetGoInfo();

            if (goI == null)
                return;

            if (_spawnId == 0)
                _spawnId = Global.ObjectMgr.GenerateGameObjectSpawnId();

            // update in loaded _data (changing _data only in this place)
            GameObjectData data = Global.ObjectMgr.NewOrExistGameObjectData(_spawnId);

            if (data.SpawnId == 0)
                data.SpawnId = _spawnId;

            Cypher.Assert(data.SpawnId == _spawnId);

            data.Id = GetEntry();
            data.MapId = GetMapId();
            data.SpawnPoint.Relocate(this);
            data.Rotation = _localRotation;
            data.spawntimesecs = (int)(_spawnedByDefault ? _respawnDelayTime : -_respawnDelayTime);
            data.Animprogress = GetGoAnimProgress();
            data.GoState = GetGoState();
            data.SpawnDifficulties = spawnDifficulties;
            data.ArtKit = (byte)GetGoArtKit();

            if (data.spawnGroupData == null)
                data.spawnGroupData = Global.ObjectMgr.GetDefaultSpawnGroup();

            data.PhaseId = GetDBPhase() > 0 ? (uint)GetDBPhase() : data.PhaseId;
            data.PhaseGroup = GetDBPhase() < 0 ? (uint)-GetDBPhase() : data.PhaseGroup;

            // Update in DB
            byte index = 0;
            PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_GAMEOBJECT);
            stmt.AddValue(0, _spawnId);
            DB.World.Execute(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.INS_GAMEOBJECT);
            stmt.AddValue(index++, _spawnId);
            stmt.AddValue(index++, GetEntry());
            stmt.AddValue(index++, mapid);
            stmt.AddValue(index++, data.SpawnDifficulties.Empty() ? "" : string.Join(",", data.SpawnDifficulties));
            stmt.AddValue(index++, data.PhaseId);
            stmt.AddValue(index++, data.PhaseGroup);
            stmt.AddValue(index++, GetPositionX());
            stmt.AddValue(index++, GetPositionY());
            stmt.AddValue(index++, GetPositionZ());
            stmt.AddValue(index++, GetOrientation());
            stmt.AddValue(index++, _localRotation.X);
            stmt.AddValue(index++, _localRotation.Y);
            stmt.AddValue(index++, _localRotation.Z);
            stmt.AddValue(index++, _localRotation.W);
            stmt.AddValue(index++, _respawnDelayTime);
            stmt.AddValue(index++, GetGoAnimProgress());
            stmt.AddValue(index++, (byte)GetGoState());
            DB.World.Execute(stmt);
        }

        public override bool LoadFromDB(ulong spawnId, Map map, bool addToMap, bool unused = true)
        {
            GameObjectData data = Global.ObjectMgr.GetGameObjectData(spawnId);

            if (data == null)
            {
                Log.outError(LogFilter.Maps, "Gameobject (SpawnId: {0}) not found in table `gameobject`, can't load. ", spawnId);

                return false;
            }

            uint entry = data.Id;

            uint animprogress = data.Animprogress;
            GameObjectState go_state = data.GoState;
            uint artKit = data.ArtKit;

            _spawnId = spawnId;
            _respawnCompatibilityMode = ((data.spawnGroupData.flags & SpawnGroupFlags.CompatibilityMode) != 0);

            if (!Create(entry, map, data.SpawnPoint, data.Rotation, animprogress, go_state, artKit, !_respawnCompatibilityMode, spawnId))
                return false;

            PhasingHandler.InitDbPhaseShift(GetPhaseShift(), data.PhaseUseFlags, data.PhaseId, data.PhaseGroup);
            PhasingHandler.InitDbVisibleMapId(GetPhaseShift(), data.terrainSwapMap);

            if (data.spawntimesecs >= 0)
            {
                _spawnedByDefault = true;

                if (!GetGoInfo().GetDespawnPossibility() &&
                    !GetGoInfo().IsDespawnAtAction())
                {
                    SetFlag(GameObjectFlags.NoDespawn);
                    _respawnDelayTime = 0;
                    _respawnTime = 0;
                }
                else
                {
                    _respawnDelayTime = (uint)data.spawntimesecs;
                    _respawnTime = GetMap().GetGORespawnTime(_spawnId);

                    // ready to respawn
                    if (_respawnTime != 0 &&
                        _respawnTime <= GameTime.GetGameTime())
                    {
                        _respawnTime = 0;
                        GetMap().RemoveRespawnTime(SpawnObjectType.GameObject, _spawnId);
                    }
                }
            }
            else
            {
                if (!_respawnCompatibilityMode)
                {
                    Log.outWarn(LogFilter.Sql, $"GameObject {entry} (SpawnID {spawnId}) is not spawned by default, but tries to use a non-hack spawn system. This will not work. Defaulting to compatibility mode.");
                    _respawnCompatibilityMode = true;
                }

                _spawnedByDefault = false;
                _respawnDelayTime = (uint)-data.spawntimesecs;
                _respawnTime = 0;
            }

            _goData = data;

            if (addToMap && !GetMap().AddToMap(this))
                return false;

            return true;
        }

        public static bool DeleteFromDB(ulong spawnId)
        {
            GameObjectData data = Global.ObjectMgr.GetGameObjectData(spawnId);

            if (data == null)
                return false;

            SQLTransaction trans = new();

            Global.MapMgr.DoForAllMapsWithMapId(data.MapId,
                                                map =>
                                                {
                                                    // despawn all active objects, and remove their respawns
                                                    List<GameObject> toUnload = new();

                                                    foreach (var creature in map.GetGameObjectBySpawnIdStore().LookupByKey(spawnId))
                                                        toUnload.Add(creature);

                                                    foreach (GameObject obj in toUnload)
                                                        map.AddObjectToRemoveList(obj);

                                                    map.RemoveRespawnTime(SpawnObjectType.GameObject, spawnId, trans);
                                                });

            // delete _data from memory
            Global.ObjectMgr.DeleteGameObjectData(spawnId);

            trans = new SQLTransaction();

            // ... and the database
            PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_GAMEOBJECT);
            stmt.AddValue(0, spawnId);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_EVENT_GAMEOBJECT);
            stmt.AddValue(0, spawnId);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN);
            stmt.AddValue(0, spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.GOToGO);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN);
            stmt.AddValue(0, spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.GOToCreature);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN_MASTER);
            stmt.AddValue(0, spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.GOToGO);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_LINKED_RESPAWN_MASTER);
            stmt.AddValue(0, spawnId);
            stmt.AddValue(1, (uint)CreatureLinkedRespawnType.CreatureToGO);
            trans.Append(stmt);

            stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_GAMEOBJECT_ADDON);
            stmt.AddValue(0, spawnId);
            trans.Append(stmt);

            DB.World.CommitTransaction(trans);

            return true;
        }

        public override bool HasQuest(uint questId)
        {
            return Global.ObjectMgr.GetGOQuestRelations(GetEntry()).HasQuest(questId);
        }

        public override bool HasInvolvedQuest(uint questId)
        {
            return Global.ObjectMgr.GetGOQuestInvolvedRelations(GetEntry()).HasQuest(questId);
        }

        public bool IsTransport()
        {
            // If something is marked as a Transport, don't transmit an out of range packet for it.
            GameObjectTemplate gInfo = GetGoInfo();

            if (gInfo == null)
                return false;

            return gInfo.type == GameObjectTypes.Transport || gInfo.type == GameObjectTypes.MapObjTransport;
        }

        // is Dynamic Transport = non-stop Transport
        public bool IsDynTransport()
        {
            // If something is marked as a Transport, don't transmit an out of range packet for it.
            GameObjectTemplate gInfo = GetGoInfo();

            if (gInfo == null)
                return false;

            return gInfo.type == GameObjectTypes.MapObjTransport || gInfo.type == GameObjectTypes.Transport;
        }

        public bool IsDestructibleBuilding()
        {
            GameObjectTemplate gInfo = GetGoInfo();

            if (gInfo == null)
                return false;

            return gInfo.type == GameObjectTypes.DestructibleBuilding;
        }

        public Transport ToTransport()
        {
            return GetGoInfo().type == GameObjectTypes.MapObjTransport ? (this as Transport) : null;
        }

        public void SaveRespawnTime(uint forceDelay = 0)
        {
            if (_goData != null &&
                (forceDelay != 0 || _respawnTime > GameTime.GetGameTime()) &&
                _spawnedByDefault)
            {
                if (_respawnCompatibilityMode)
                {
                    RespawnInfo ri = new();
                    ri.type = SpawnObjectType.GameObject;
                    ri.spawnId = _spawnId;
                    ri.respawnTime = _respawnTime;
                    GetMap().SaveRespawnInfoDB(ri);

                    return;
                }

                long thisRespawnTime = forceDelay != 0 ? GameTime.GetGameTime() + forceDelay : _respawnTime;
                GetMap().SaveRespawnTime(SpawnObjectType.GameObject, _spawnId, GetEntry(), thisRespawnTime, GridDefines.ComputeGridCoord(GetPositionX(), GetPositionY()).GetId());
            }
        }

        public override bool IsNeverVisibleFor(WorldObject seer)
        {
            if (base.IsNeverVisibleFor(seer))
                return true;

            if (GetGoInfo().GetServerOnly() != 0)
                return true;

            if (GetDisplayId() == 0)
                return true;

            return false;
        }

        public override bool IsAlwaysVisibleFor(WorldObject seer)
        {
            if (base.IsAlwaysVisibleFor(seer))
                return true;

            if (IsTransport() ||
                IsDestructibleBuilding())
                return true;

            if (seer == null)
                return false;

            // Always seen by owner and friendly units
            ObjectGuid guid = GetOwnerGUID();

            if (!guid.IsEmpty())
            {
                if (seer.GetGUID() == guid)
                    return true;

                Unit owner = GetOwner();

                if (owner != null &&
                    seer.IsTypeMask(TypeMask.Unit) &&
                    owner.IsFriendlyTo(seer.ToUnit()))
                    return true;
            }

            return false;
        }

        public override bool IsInvisibleDueToDespawn(WorldObject seer)
        {
            if (base.IsInvisibleDueToDespawn(seer))
                return true;

            // Despawned
            if (!IsSpawned())
                return true;

            if (_perPlayerState != null)
            {
                PerPlayerState state = _perPlayerState.LookupByKey(seer.GetGUID());

                if (state != null &&
                    state.Despawned)
                    return true;
            }

            return false;
        }

        public void Respawn()
        {
            if (_spawnedByDefault && _respawnTime > 0)
            {
                _respawnTime = GameTime.GetGameTime();
                GetMap().Respawn(SpawnObjectType.GameObject, _spawnId);
            }
        }

        public bool ActivateToQuest(Player target)
        {
            if (target.HasQuestForGO((int)GetEntry()))
                return true;

            if (!Global.ObjectMgr.IsGameObjectForQuests(GetEntry()))
                return false;

            switch (GetGoType())
            {
                case GameObjectTypes.QuestGiver:
                    QuestGiverStatus questStatus = target.GetQuestDialogStatus(this);

                    if (questStatus != QuestGiverStatus.None &&
                        questStatus != QuestGiverStatus.Future)
                        return true;

                    break;
                case GameObjectTypes.Chest:
                    {
                        // Chests become inactive while not ready to be looted
                        if (GetLootState() == LootState.NotReady)
                            return false;

                        // scan GO chest with loot including quest items
                        if (target.GetQuestStatus(GetGoInfo().Chest.questID) == QuestStatus.Incomplete ||
                            LootStorage.Gameobject.HaveQuestLootForPlayer(GetGoInfo().Chest.chestLoot, target) ||
                            LootStorage.Gameobject.HaveQuestLootForPlayer(GetGoInfo().Chest.chestPersonalLoot, target) ||
                            LootStorage.Gameobject.HaveQuestLootForPlayer(GetGoInfo().Chest.chestPushLoot, target))
                        {
                            Battleground bg = target.GetBattleground();

                            if (bg)
                                return bg.CanActivateGO((int)GetEntry(), (uint)bg.GetPlayerTeam(target.GetGUID()));

                            return true;
                        }

                        break;
                    }
                case GameObjectTypes.Generic:
                    {
                        if (target.GetQuestStatus(GetGoInfo().Generic.questID) == QuestStatus.Incomplete)
                            return true;

                        break;
                    }
                case GameObjectTypes.Goober:
                    {
                        if (target.GetQuestStatus(GetGoInfo().Goober.questID) == QuestStatus.Incomplete)
                            return true;

                        break;
                    }
                default:
                    break;
            }

            return false;
        }

        public void TriggeringLinkedGameObject(uint trapEntry, Unit target)
        {
            GameObjectTemplate trapInfo = Global.ObjectMgr.GetGameObjectTemplate(trapEntry);

            if (trapInfo == null ||
                trapInfo.type != GameObjectTypes.Trap)
                return;

            SpellInfo trapSpell = Global.SpellMgr.GetSpellInfo(trapInfo.Trap.spell, GetMap().GetDifficultyID());

            if (trapSpell == null) // checked at load already
                return;

            GameObject trapGO = GetLinkedTrap();

            if (trapGO)
                trapGO.CastSpell(target, trapSpell.Id);
        }

        public void ResetDoorOrButton()
        {
            if (_lootState == LootState.Ready ||
                _lootState == LootState.JustDeactivated)
                return;

            RemoveFlag(GameObjectFlags.InUse);
            SetGoState(_prevGoState);

            SetLootState(LootState.JustDeactivated);
            _cooldownTime = 0;
        }

        public void UseDoorOrButton(uint time_to_restore = 0, bool alternative = false, Unit user = null)
        {
            if (_lootState != LootState.Ready)
                return;

            if (time_to_restore == 0)
                time_to_restore = GetGoInfo().GetAutoCloseTime();

            SwitchDoorOrButton(true, alternative);
            SetLootState(LootState.Activated, user);

            _cooldownTime = time_to_restore != 0 ? GameTime.GetGameTimeMS() + time_to_restore : 0;
        }

        public void ActivateObject(GameObjectActions action, int param, WorldObject spellCaster = null, uint spellId = 0, int effectIndex = -1)
        {
            Unit unitCaster = spellCaster ? spellCaster.ToUnit() : null;

            switch (action)
            {
                case GameObjectActions.None:
                    Log.outFatal(LogFilter.Spells, $"Spell {spellId} has Action Type NONE in effect {effectIndex}");

                    break;
                case GameObjectActions.AnimateCustom0:
                case GameObjectActions.AnimateCustom1:
                case GameObjectActions.AnimateCustom2:
                case GameObjectActions.AnimateCustom3:
                    SendCustomAnim((uint)(action - GameObjectActions.AnimateCustom0));

                    break;
                case GameObjectActions.Disturb: // What's the difference with Open?
                    if (unitCaster)
                        Use(unitCaster);

                    break;
                case GameObjectActions.Unlock:
                    RemoveFlag(GameObjectFlags.Locked);

                    break;
                case GameObjectActions.Lock:
                    SetFlag(GameObjectFlags.Locked);

                    break;
                case GameObjectActions.Open:
                    if (unitCaster)
                        Use(unitCaster);

                    break;
                case GameObjectActions.OpenAndUnlock:
                    if (unitCaster)
                        UseDoorOrButton(0, false, unitCaster);

                    RemoveFlag(GameObjectFlags.Locked);

                    break;
                case GameObjectActions.Close:
                    ResetDoorOrButton();

                    break;
                case GameObjectActions.ToggleOpen:
                    // No use cases, implementation unknown
                    break;
                case GameObjectActions.Destroy:
                    if (unitCaster)
                        UseDoorOrButton(0, true, unitCaster);

                    break;
                case GameObjectActions.Rebuild:
                    ResetDoorOrButton();

                    break;
                case GameObjectActions.Creation:
                    // No use cases, implementation unknown
                    break;
                case GameObjectActions.Despawn:
                    DespawnOrUnsummon();

                    break;
                case GameObjectActions.MakeInert:
                    SetFlag(GameObjectFlags.NotSelectable);

                    break;
                case GameObjectActions.MakeActive:
                    RemoveFlag(GameObjectFlags.NotSelectable);

                    break;
                case GameObjectActions.CloseAndLock:
                    ResetDoorOrButton();
                    SetFlag(GameObjectFlags.Locked);

                    break;
                case GameObjectActions.UseArtKit0:
                case GameObjectActions.UseArtKit1:
                case GameObjectActions.UseArtKit2:
                case GameObjectActions.UseArtKit3:
                case GameObjectActions.UseArtKit4:
                    {
                        GameObjectTemplateAddon templateAddon = GetTemplateAddon();

                        uint artKitIndex = action != GameObjectActions.UseArtKit4 ? (uint)(action - GameObjectActions.UseArtKit0) : 4;

                        uint artKitValue = 0;

                        if (templateAddon != null)
                            artKitValue = templateAddon.ArtKits[artKitIndex];

                        if (artKitValue == 0)
                            Log.outError(LogFilter.Sql, $"GameObject {GetEntry()} hit by spell {spellId} needs `artkit{artKitIndex}` in `gameobject_template_addon`");
                        else
                            SetGoArtKit(artKitValue);

                        break;
                    }
                case GameObjectActions.GoTo1stFloor:
                case GameObjectActions.GoTo2ndFloor:
                case GameObjectActions.GoTo3rdFloor:
                case GameObjectActions.GoTo4thFloor:
                case GameObjectActions.GoTo5thFloor:
                case GameObjectActions.GoTo6thFloor:
                case GameObjectActions.GoTo7thFloor:
                case GameObjectActions.GoTo8thFloor:
                case GameObjectActions.GoTo9thFloor:
                case GameObjectActions.GoTo10thFloor:
                    if (GetGoType() == GameObjectTypes.Transport)
                        SetGoState((GameObjectState)action);
                    else
                        Log.outError(LogFilter.Spells, $"Spell {spellId} targeted non-Transport gameobject for Transport only Action \"Go to Floor\" {action} in effect {effectIndex}");

                    break;
                case GameObjectActions.PlayAnimKit:
                    SetAnimKitId((ushort)param, false);

                    break;
                case GameObjectActions.OpenAndPlayAnimKit:
                    if (unitCaster)
                        UseDoorOrButton(0, false, unitCaster);

                    SetAnimKitId((ushort)param, false);

                    break;
                case GameObjectActions.CloseAndPlayAnimKit:
                    ResetDoorOrButton();
                    SetAnimKitId((ushort)param, false);

                    break;
                case GameObjectActions.PlayOneShotAnimKit:
                    SetAnimKitId((ushort)param, true);

                    break;
                case GameObjectActions.StopAnimKit:
                    SetAnimKitId(0, false);

                    break;
                case GameObjectActions.OpenAndStopAnimKit:
                    if (unitCaster)
                        UseDoorOrButton(0, false, unitCaster);

                    SetAnimKitId(0, false);

                    break;
                case GameObjectActions.CloseAndStopAnimKit:
                    ResetDoorOrButton();
                    SetAnimKitId(0, false);

                    break;
                case GameObjectActions.PlaySpellVisual:
                    SetSpellVisualId((uint)param, spellCaster.GetGUID());

                    break;
                case GameObjectActions.StopSpellVisual:
                    SetSpellVisualId(0);

                    break;
                default:
                    Log.outError(LogFilter.Spells, $"Spell {spellId} has unhandled Action {action} in effect {effectIndex}");

                    break;
            }
        }

        public void SetGoArtKit(uint kit)
        {
            SetUpdateFieldValue(Values.ModifyValue(_gameObjectData).ModifyValue(_gameObjectData.ArtKit), kit);
            GameObjectData data = Global.ObjectMgr.GetGameObjectData(_spawnId);

            if (data != null)
                data.ArtKit = kit;
        }

        public void SetGoArtKit(uint artkit, GameObject go, uint lowguid)
        {
            GameObjectData data = null;

            if (go != null)
            {
                go.SetGoArtKit(artkit);
                data = go.GetGameObjectData();
            }
            else if (lowguid != 0)
            {
                data = Global.ObjectMgr.GetGameObjectData(lowguid);
            }

            if (data != null)
                data.ArtKit = artkit;
        }

        public void Use(Unit user)
        {
            // by default spell caster is user
            Unit spellCaster = user;
            uint spellId = 0;
            bool triggered = false;

            Player playerUser = user.ToPlayer();

            if (playerUser != null)
            {
                if (_goInfo.GetNoDamageImmune() != 0 &&
                    playerUser.HasUnitFlag(UnitFlags.Immune))
                    return;

                if (!_goInfo.IsUsableMounted())
                    playerUser.RemoveAurasByType(AuraType.Mounted);

                playerUser.PlayerTalkClass.ClearMenus();

                if (GetAI().OnGossipHello(playerUser))
                    return;
            }

            // If cooldown _data present in template
            uint cooldown = GetGoInfo().GetCooldown();

            if (cooldown != 0)
            {
                if (_cooldownTime > GameTime.GetGameTime())
                    return;

                _cooldownTime = GameTime.GetGameTimeMS() + cooldown * Time.InMilliseconds;
            }

            switch (GetGoType())
            {
                case GameObjectTypes.Door:   //0
                case GameObjectTypes.Button: //1
                                             //doors/buttons never really despawn, only reset to default State/Flags
                    UseDoorOrButton(0, false, user);

                    return;
                case GameObjectTypes.QuestGiver: //2
                    {
                        if (!user.IsTypeId(TypeId.Player))
                            return;

                        Player player = user.ToPlayer();

                        player.PrepareGossipMenu(this, GetGoInfo().QuestGiver.gossipID, true);
                        player.SendPreparedGossip(this);

                        return;
                    }
                case GameObjectTypes.Chest: //3
                    {
                        Player player = user.ToPlayer();

                        if (!player)
                            return;

                        Battleground bg = player.GetBattleground();

                        if (bg != null &&
                            !bg.CanActivateGO((int)GetEntry(), (uint)bg.GetPlayerTeam(user.GetGUID())))
                            return;

                        GameObjectTemplate info = GetGoInfo();

                        if (loot == null &&
                            info.GetLootId() != 0)
                        {
                            if (info.GetLootId() != 0)
                            {
                                Group group = player.GetGroup();
                                bool groupRules = group != null && info.Chest.usegrouplootrules != 0;

                                loot = new Loot(GetMap(), GetGUID(), LootType.Chest, groupRules ? group : null);
                                loot.SetDungeonEncounterId(info.Chest.DungeonEncounter);
                                loot.FillLoot(info.GetLootId(), LootStorage.Gameobject, player, !groupRules, false, GetLootMode(), GetMap().GetDifficultyLootItemContext());

                                if (GetLootMode() > 0)
                                {
                                    GameObjectTemplateAddon addon = GetTemplateAddon();

                                    if (addon != null)
                                        loot.GenerateMoneyLoot(addon.Mingold, addon.Maxgold);
                                }
                            }

                            /// @todo possible must be moved to loot release (in different from linked triggering)
                            if (info.Chest.triggeredEvent != 0)
                                GameEvents.Trigger(info.Chest.triggeredEvent, player, this);

                            // triggering linked GO
                            uint trapEntry = info.Chest.linkedTrap;

                            if (trapEntry != 0)
                                TriggeringLinkedGameObject(trapEntry, player);
                        }
                        else if (!_personalLoot.ContainsKey(player.GetGUID()))
                        {
                            if (info.Chest.chestPersonalLoot != 0)
                            {
                                GameObjectTemplateAddon addon = GetTemplateAddon();

                                if (info.Chest.DungeonEncounter != 0)
                                {
                                    List<Player> tappers = new();

                                    foreach (ObjectGuid tapperGuid in GetTapList())
                                    {
                                        Player tapper = Global.ObjAccessor.GetPlayer(this, tapperGuid);

                                        if (tapper != null)
                                            tappers.Add(tapper);
                                    }

                                    if (tappers.Empty())
                                        tappers.Add(player);

                                    _personalLoot = LootManager.GenerateDungeonEncounterPersonalLoot(info.Chest.DungeonEncounter,
                                                                                                     info.Chest.chestPersonalLoot,
                                                                                                     LootStorage.Gameobject,
                                                                                                     LootType.Chest,
                                                                                                     this,
                                                                                                     addon != null ? addon.Mingold : 0,
                                                                                                     addon != null ? addon.Maxgold : 0,
                                                                                                     (ushort)GetLootMode(),
                                                                                                     GetMap().GetDifficultyLootItemContext(),
                                                                                                     tappers);
                                }
                                else
                                {
                                    Loot loot = new(GetMap(), GetGUID(), LootType.Chest, null);
                                    _personalLoot[player.GetGUID()] = loot;

                                    loot.SetDungeonEncounterId(info.Chest.DungeonEncounter);
                                    loot.FillLoot(info.Chest.chestPersonalLoot, LootStorage.Gameobject, player, true, false, GetLootMode(), GetMap().GetDifficultyLootItemContext());

                                    if (GetLootMode() > 0 &&
                                        addon != null)
                                        loot.GenerateMoneyLoot(addon.Mingold, addon.Maxgold);
                                }
                            }
                        }

                        if (!_unique_users.Contains(player.GetGUID()) &&
                            info.GetLootId() == 0)
                        {
                            if (info.Chest.chestPushLoot != 0)
                            {
                                Loot pushLoot = new(GetMap(), GetGUID(), LootType.Chest, null);
                                pushLoot.FillLoot(info.Chest.chestPushLoot, LootStorage.Gameobject, player, true, false, GetLootMode(), GetMap().GetDifficultyLootItemContext());
                                pushLoot.AutoStore(player, ItemConst.NullBag, ItemConst.NullSlot);
                            }

                            if (info.Chest.triggeredEvent != 0)
                                GameEvents.Trigger(info.Chest.triggeredEvent, player, this);

                            // triggering linked GO
                            uint trapEntry = info.Chest.linkedTrap;

                            if (trapEntry != 0)
                                TriggeringLinkedGameObject(trapEntry, player);

                            AddUniqueUse(player);
                        }

                        if (GetLootState() != LootState.Activated)
                            SetLootState(LootState.Activated, player);

                        // Send loot
                        Loot playerLoot = GetLootForPlayer(player);

                        if (playerLoot != null)
                            player.SendLoot(playerLoot);

                        break;
                    }
                case GameObjectTypes.Trap: //6
                    {
                        GameObjectTemplate goInfo = GetGoInfo();

                        if (goInfo.Trap.spell != 0)
                            CastSpell(user, goInfo.Trap.spell);

                        _cooldownTime = GameTime.GetGameTimeMS() + (goInfo.Trap.cooldown != 0 ? goInfo.Trap.cooldown : 4) * Time.InMilliseconds; // template or 4 seconds

                        if (goInfo.Trap.charges == 1) // Deactivate after trigger
                            SetLootState(LootState.JustDeactivated);

                        return;
                    }
                //Sitting: Wooden bench, chairs enzz
                case GameObjectTypes.Chair: //7
                    {
                        GameObjectTemplate info = GetGoInfo();

                        if (ChairListSlots.Empty()) // this is called once at first chair use to make list of available slots
                        {
                            if (info.Chair.chairslots > 0) // sometimes chairs in DB have error in fields and we dont know number of slots
                                for (uint i = 0; i < info.Chair.chairslots; ++i)
                                    ChairListSlots[i] = default; // Last user of current Slot set to 0 (none sit here yet)
                            else
                                ChairListSlots[0] = default; // error in DB, make one default Slot
                        }

                        // a chair may have n slots. we have to calculate their positions and teleport the player to the nearest one
                        float lowestDist = SharedConst.DefaultVisibilityDistance;

                        uint nearest_slot = 0;
                        float x_lowest = GetPositionX();
                        float y_lowest = GetPositionY();

                        // the object orientation + 1/2 pi
                        // every Slot will be on that straight line
                        float orthogonalOrientation = GetOrientation() + MathFunctions.PI * 0.5f;
                        // find nearest Slot
                        bool found_free_slot = false;

                        foreach (var (slot, sittingUnit) in ChairListSlots.ToList())
                        {
                            // the distance between this Slot and the center of the go - imagine a 1D space
                            float relativeDistance = (info.size * slot) - (info.size * (info.Chair.chairslots - 1) / 2.0f);

                            float x_i = (float)(GetPositionX() + relativeDistance * Math.Cos(orthogonalOrientation));
                            float y_i = (float)(GetPositionY() + relativeDistance * Math.Sin(orthogonalOrientation));

                            if (!sittingUnit.IsEmpty())
                            {
                                Unit chairUser = Global.ObjAccessor.GetUnit(this, sittingUnit);

                                if (chairUser != null)
                                {
                                    if (chairUser.IsSitState() &&
                                        chairUser.GetStandState() != UnitStandStateType.Sit &&
                                        chairUser.GetExactDist2d(x_i, y_i) < 0.1f)
                                        continue; // This Seat is already occupied by ChairUser. NOTE: Not sure if the ChairUser.getStandState() != UNIT_STAND_STATE_SIT check is required.

                                    ChairListSlots[slot].Clear(); // This Seat is unoccupied.
                                }
                                else
                                {
                                    ChairListSlots[slot].Clear(); // The Seat may of had an occupant, but they're offline.
                                }
                            }

                            found_free_slot = true;

                            // calculate the distance between the player and this Slot
                            float thisDistance = user.GetDistance2d(x_i, y_i);

                            if (thisDistance <= lowestDist)
                            {
                                nearest_slot = slot;
                                lowestDist = thisDistance;
                                x_lowest = x_i;
                                y_lowest = y_i;
                            }
                        }

                        if (found_free_slot)
                        {
                            var guid = ChairListSlots.LookupByKey(nearest_slot);

                            if (guid.IsEmpty())
                            {
                                ChairListSlots[nearest_slot] = user.GetGUID(); //this Slot in now used by player
                                user.NearTeleportTo(x_lowest, y_lowest, GetPositionZ(), GetOrientation());
                                user.SetStandState(UnitStandStateType.SitLowChair + (byte)info.Chair.chairheight);

                                if (info.Chair.triggeredEvent != 0)
                                    GameEvents.Trigger(info.Chair.triggeredEvent, user, this);

                                return;
                            }
                        }

                        return;
                    }
                case GameObjectTypes.SpellFocus: //8
                    {
                        // triggering linked GO
                        uint trapEntry = GetGoInfo().SpellFocus.linkedTrap;

                        if (trapEntry != 0)
                            TriggeringLinkedGameObject(trapEntry, user);

                        break;
                    }
                //big gun, its a spell/aura
                case GameObjectTypes.Goober: //10
                    {
                        GameObjectTemplate info = GetGoInfo();
                        Player player = user.ToPlayer();

                        if (player != null)
                        {
                            if (info.Goober.pageID != 0) // show page...
                            {
                                PageTextPkt data = new();
                                data.GameObjectGUID = GetGUID();
                                player.SendPacket(data);
                            }
                            else if (info.Goober.gossipID != 0)
                            {
                                player.PrepareGossipMenu(this, info.Goober.gossipID);
                                player.SendPreparedGossip(this);
                            }

                            if (info.Goober.eventID != 0)
                            {
                                Log.outDebug(LogFilter.Scripts, "Goober ScriptStart Id {0} for GO entry {1} (GUID {2}).", info.Goober.eventID, GetEntry(), GetSpawnId());
                                GameEvents.Trigger(info.Goober.eventID, player, this);
                            }

                            // possible quest objective for active quests
                            if (info.Goober.questID != 0 &&
                                Global.ObjectMgr.GetQuestTemplate(info.Goober.questID) != null)
                                //Quest require to be active for GO using
                                if (player.GetQuestStatus(info.Goober.questID) != QuestStatus.Incomplete)
                                    break;

                            Group group = player.GetGroup();

                            if (group)
                                for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
                                {
                                    Player member = refe.GetSource();

                                    if (member)
                                        if (member.IsAtGroupRewardDistance(this))
                                            member.KillCreditGO(info.entry, GetGUID());
                                }
                            else
                                player.KillCreditGO(info.entry, GetGUID());
                        }

                        uint trapEntry = info.Goober.linkedTrap;

                        if (trapEntry != 0)
                            TriggeringLinkedGameObject(trapEntry, user);

                        if (info.Goober.AllowMultiInteract != 0 &&
                            player != null)
                        {
                            if (info.IsDespawnAtAction())
                                DespawnForPlayer(player, TimeSpan.FromSeconds(_respawnDelayTime));
                            else
                                SetGoStateFor(GameObjectState.Active, player);
                        }
                        else
                        {
                            SetFlag(GameObjectFlags.InUse);
                            SetLootState(LootState.Activated, user);

                            // this appear to be ok, however others exist in addition to this that should have custom (ex: 190510, 188692, 187389)
                            if (info.Goober.customAnim != 0)
                                SendCustomAnim(GetGoAnimProgress());
                            else
                                SetGoState(GameObjectState.Active);

                            _cooldownTime = GameTime.GetGameTimeMS() + info.GetAutoCloseTime();
                        }

                        // cast this spell later if provided
                        spellId = info.Goober.spell;

                        if (info.Goober.playerCast == 0)
                            spellCaster = null;

                        break;
                    }
                case GameObjectTypes.Camera: //13
                    {
                        GameObjectTemplate info = GetGoInfo();

                        if (info == null)
                            return;

                        if (!user.IsTypeId(TypeId.Player))
                            return;

                        Player player = user.ToPlayer();

                        if (info.Camera._camera != 0)
                            player.SendCinematicStart(info.Camera._camera);

                        if (info.Camera.eventID != 0)
                            GameEvents.Trigger(info.Camera.eventID, player, this);

                        return;
                    }
                //fishing bobber
                case GameObjectTypes.FishingNode: //17
                    {
                        Player player = user.ToPlayer();

                        if (player == null)
                            return;

                        if (player.GetGUID() != GetOwnerGUID())
                            return;

                        switch (GetLootState())
                        {
                            case LootState.Ready: // ready for loot
                                {
                                    uint zone, subzone;
                                    GetZoneAndAreaId(out zone, out subzone);

                                    int zone_skill = Global.ObjectMgr.GetFishingBaseSkillLevel(subzone);

                                    if (zone_skill == 0)
                                        zone_skill = Global.ObjectMgr.GetFishingBaseSkillLevel(zone);

                                    //provide error, no fishable zone or area should be 0
                                    if (zone_skill == 0)
                                        Log.outError(LogFilter.Sql, "Fishable areaId {0} are not properly defined in `skill_fishing_base_level`.", subzone);

                                    int skill = player.GetSkillValue(SkillType.Fishing);

                                    int chance;

                                    if (skill < zone_skill)
                                    {
                                        chance = (int)(Math.Pow((double)skill / zone_skill, 2) * 100);

                                        if (chance < 1)
                                            chance = 1;
                                    }
                                    else
                                    {
                                        chance = 100;
                                    }

                                    int roll = RandomHelper.IRand(1, 100);

                                    Log.outDebug(LogFilter.Server, "Fishing check (skill: {0} zone min skill: {1} chance {2} roll: {3}", skill, zone_skill, chance, roll);

                                    player.UpdateFishingSkill();

                                    // @todo find reasonable value for fishing hole search
                                    GameObject fishingPool = LookupFishingHoleAround(20.0f + SharedConst.ContactDistance);

                                    // If fishing skill is high enough, or if fishing on a pool, send correct loot.
                                    // Fishing pools have no skill requirement as of patch 3.3.0 (undocumented change).
                                    if (chance >= roll || fishingPool)
                                    {
                                        // @todo I do not understand this hack. Need some explanation.
                                        // prevent removing GO at spell cancel
                                        RemoveFromOwner();
                                        SetOwnerGUID(player.GetGUID());

                                        if (fishingPool)
                                        {
                                            fishingPool.Use(player);
                                            SetLootState(LootState.JustDeactivated);
                                        }
                                        else
                                        {
                                            loot = GetFishLoot(player);
                                            player.SendLoot(loot);
                                        }
                                    }
                                    else // If fishing skill is too low, send junk loot.
                                    {
                                        loot = GetFishLootJunk(player);
                                        player.SendLoot(loot);
                                    }

                                    break;
                                }
                            case LootState.JustDeactivated: // nothing to do, will be deleted at next update
                                break;
                            default:
                                {
                                    SetLootState(LootState.JustDeactivated);
                                    player.SendPacket(new FishNotHooked());

                                    break;
                                }
                        }

                        player.FinishSpell(CurrentSpellTypes.Channeled);

                        return;
                    }

                case GameObjectTypes.Ritual: //18
                    {
                        if (!user.IsTypeId(TypeId.Player))
                            return;

                        Player player = user.ToPlayer();

                        Unit owner = GetOwner();

                        GameObjectTemplate info = GetGoInfo();

                        // ritual owner is set for GO's without owner (not summoned)
                        if (_ritualOwner == null &&
                            owner == null)
                            _ritualOwner = player;

                        if (owner != null)
                        {
                            if (!owner.IsTypeId(TypeId.Player))
                                return;

                            // accept only use by player from same group as owner, excluding owner itself (unique use already added in spell effect)
                            if (player == owner.ToPlayer() ||
                                (info.Ritual.castersGrouped != 0 && !player.IsInSameRaidWith(owner.ToPlayer())))
                                return;

                            // expect owner to already be channeling, so if not...
                            if (owner.GetCurrentSpell(CurrentSpellTypes.Channeled) == null)
                                return;

                            // in case summoning ritual caster is GO creator
                            spellCaster = owner;
                        }
                        else
                        {
                            if (player != _ritualOwner &&
                                (info.Ritual.castersGrouped != 0 && !player.IsInSameRaidWith(_ritualOwner)))
                                return;

                            spellCaster = player;
                        }

                        AddUniqueUse(player);

                        if (info.Ritual.animSpell != 0)
                        {
                            player.CastSpell(player, info.Ritual.animSpell, true);

                            // for this case, summoningRitual.spellId is always triggered
                            triggered = true;
                        }

                        // full amount unique participants including original summoner
                        if (GetUniqueUseCount() == info.Ritual.casters)
                        {
                            if (_ritualOwner != null)
                                spellCaster = _ritualOwner;

                            spellId = info.Ritual.spell;

                            if (spellId == 62330) // GO store nonexistent spell, replace by expected
                            {
                                // spell have reagent and mana cost but it not expected use its
                                // it triggered spell in fact casted at currently channeled GO
                                spellId = 61993;
                                triggered = true;
                            }

                            // Cast casterTargetSpell at a random GO user
                            // on the current DB there is only one gameobject that uses this (Ritual of Doom)
                            // and its required Target number is 1 (outter for loop will run once)
                            if (info.Ritual.casterTargetSpell != 0 &&
                                info.Ritual.casterTargetSpell != 1) // No idea why this field is a bool in some cases
                                for (uint i = 0; i < info.Ritual.casterTargetSpellTargets; i++)
                                {
                                    // _unique_users can contain only player GUIDs
                                    Player target = Global.ObjAccessor.GetPlayer(this, _unique_users.SelectRandom());

                                    if (target != null)
                                        spellCaster.CastSpell(target, info.Ritual.casterTargetSpell, true);
                                }

                            // finish owners spell
                            owner?.FinishSpell(CurrentSpellTypes.Channeled);

                            // can be deleted now, if
                            if (info.Ritual.ritualPersistent == 0)
                            {
                                SetLootState(LootState.JustDeactivated);
                            }
                            else
                            {
                                // reset ritual for this GO
                                _ritualOwner = null;
                                _unique_users.Clear();
                                _usetimes = 0;
                            }
                        }
                        else
                        {
                            return;
                        }

                        // go to end function to spell casting
                        break;
                    }
                case GameObjectTypes.SpellCaster: //22
                    {
                        GameObjectTemplate info = GetGoInfo();

                        if (info == null)
                            return;

                        if (info.SpellCaster.partyOnly != 0)
                        {
                            Unit caster = GetOwner();

                            if (caster == null ||
                                !caster.IsTypeId(TypeId.Player))
                                return;

                            if (!user.IsTypeId(TypeId.Player) ||
                                !user.ToPlayer().IsInSameRaidWith(caster.ToPlayer()))
                                return;
                        }

                        user.RemoveAurasByType(AuraType.Mounted);
                        spellId = info.SpellCaster.spell;

                        AddUse();

                        break;
                    }
                case GameObjectTypes.MeetingStone: //23
                    {
                        GameObjectTemplate info = GetGoInfo();

                        if (!user.IsTypeId(TypeId.Player))
                            return;

                        Player player = user.ToPlayer();

                        Player targetPlayer = Global.ObjAccessor.FindPlayer(player.GetTarget());

                        // accept only use by player from same raid as caster, except caster itself
                        if (targetPlayer == null ||
                            targetPlayer == player ||
                            !targetPlayer.IsInSameRaidWith(player))
                            return;

                        //required lvl checks!
                        var userLevels = Global.DB2Mgr.GetContentTuningData(info.ContentTuningId, player.PlayerData.CtrOptions.GetValue().ContentTuningConditionMask);

                        if (userLevels.HasValue)
                            if (player.GetLevel() < userLevels.Value.MaxLevel)
                                return;

                        var targetLevels = Global.DB2Mgr.GetContentTuningData(info.ContentTuningId, targetPlayer.PlayerData.CtrOptions.GetValue().ContentTuningConditionMask);

                        if (targetLevels.HasValue)
                            if (targetPlayer.GetLevel() < targetLevels.Value.MaxLevel)
                                return;

                        if (info.entry == 194097)
                            spellId = 61994; // Ritual of Summoning
                        else
                            spellId = 23598; // 59782;                            // Summoning Stone Effect

                        break;
                    }

                case GameObjectTypes.FlagStand: // 24
                    {
                        if (!user.IsTypeId(TypeId.Player))
                            return;

                        Player player = user.ToPlayer();

                        if (player.CanUseBattlegroundObject(this))
                        {
                            // in Battlegroundcheck
                            Battleground bg = player.GetBattleground();

                            if (!bg)
                                return;

                            if (player.GetVehicle() != null)
                                return;

                            player.RemoveAurasByType(AuraType.ModStealth);
                            player.RemoveAurasByType(AuraType.ModInvisibility);
                            // BG flag click
                            // AB:
                            // 15001
                            // 15002
                            // 15003
                            // 15004
                            // 15005
                            bg.EventPlayerClickedOnFlag(player, this);

                            return; //we don;t need to delete flag ... it is despawned!
                        }

                        break;
                    }

                case GameObjectTypes.FishingHole: // 25
                    {
                        if (!user.IsTypeId(TypeId.Player))
                            return;

                        Player player = user.ToPlayer();

                        Loot loot = new(GetMap(), GetGUID(), LootType.Fishinghole, null);
                        loot.FillLoot(GetGoInfo().GetLootId(), LootStorage.Gameobject, player, true);
                        _personalLoot[player.GetGUID()] = loot;

                        player.SendLoot(loot);
                        player.UpdateCriteria(CriteriaType.CatchFishInFishingHole, GetGoInfo().entry);

                        return;
                    }

                case GameObjectTypes.FlagDrop: // 26
                    {
                        if (!user.IsTypeId(TypeId.Player))
                            return;

                        Player player = user.ToPlayer();

                        if (player.CanUseBattlegroundObject(this))
                        {
                            // in Battlegroundcheck
                            Battleground bg = player.GetBattleground();

                            if (!bg)
                                return;

                            if (player.GetVehicle() != null)
                                return;

                            player.RemoveAurasByType(AuraType.ModStealth);
                            player.RemoveAurasByType(AuraType.ModInvisibility);
                            // BG flag dropped
                            // WS:
                            // 179785 - Silverwing Flag
                            // 179786 - Warsong Flag
                            // EotS:
                            // 184142 - Netherstorm Flag
                            GameObjectTemplate info = GetGoInfo();

                            if (info != null)
                            {
                                switch (info.entry)
                                {
                                    case 179785: // Silverwing Flag
                                    case 179786: // Warsong Flag
                                        if (bg.GetTypeID(true) == BattlegroundTypeId.WS)
                                            bg.EventPlayerClickedOnFlag(player, this);

                                        break;
                                    case 184142: // Netherstorm Flag
                                        if (bg.GetTypeID(true) == BattlegroundTypeId.EY)
                                            bg.EventPlayerClickedOnFlag(player, this);

                                        break;
                                }

                                if (info.FlagDrop.eventID != 0)
                                    GameEvents.Trigger(info.FlagDrop.eventID, player, this);
                            }

                            //this cause to call return, all Flags must be deleted here!!
                            spellId = 0;
                            Delete();
                        }

                        break;
                    }
                case GameObjectTypes.BarberChair: //32
                    {
                        GameObjectTemplate info = GetGoInfo();

                        if (info == null)
                            return;

                        if (!user.IsTypeId(TypeId.Player))
                            return;

                        Player player = user.ToPlayer();

                        player.SendPacket(new EnableBarberShop());

                        // fallback, will always work
                        player.TeleportTo(GetMapId(), GetPositionX(), GetPositionY(), GetPositionZ(), GetOrientation(), (TeleportToOptions.NotLeaveTransport | TeleportToOptions.NotLeaveCombat | TeleportToOptions.NotUnSummonPet));
                        player.SetStandState((UnitStandStateType.SitLowChair + (byte)info.BarberChair.chairheight), info.BarberChair.SitAnimKit);

                        return;
                    }
                case GameObjectTypes.NewFlag:
                    {
                        GameObjectTemplate info = GetGoInfo();

                        if (info == null)
                            return;

                        if (!user.IsPlayer())
                            return;

                        spellId = info.NewFlag.pickupSpell;

                        break;
                    }
                case GameObjectTypes.ItemForge:
                    {
                        GameObjectTemplate info = GetGoInfo();

                        if (info == null)
                            return;

                        if (!user.IsTypeId(TypeId.Player))
                            return;

                        Player player = user.ToPlayer();
                        PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(info.ItemForge.conditionID1);

                        if (playerCondition != null)
                            if (!ConditionManager.IsPlayerMeetingCondition(player, playerCondition))
                                return;

                        switch (info.ItemForge.ForgeType)
                        {
                            case 0: // Artifact Forge
                            case 1: // Relic Forge
                                {
                                    Aura artifactAura = player.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);
                                    Item item = artifactAura != null ? player.GetItemByGuid(artifactAura.GetCastItemGUID()) : null;

                                    if (!item)
                                    {
                                        player.SendPacket(new DisplayGameError(GameError.MustEquipArtifact));

                                        return;
                                    }

                                    OpenArtifactForge openArtifactForge = new();
                                    openArtifactForge.ArtifactGUID = item.GetGUID();
                                    openArtifactForge.ForgeGUID = GetGUID();
                                    player.SendPacket(openArtifactForge);

                                    break;
                                }
                            case 2: // Heart Forge
                                {
                                    Item item = player.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);

                                    if (!item)
                                        return;

                                    GameObjectInteraction openHeartForge = new();
                                    openHeartForge.ObjectGUID = GetGUID();
                                    openHeartForge.InteractionType = PlayerInteractionType.AzeriteForge;
                                    player.SendPacket(openHeartForge);

                                    break;
                                }
                            default:
                                break;
                        }

                        break;
                    }
                case GameObjectTypes.UILink:
                    {
                        Player player = user.ToPlayer();

                        if (!player)
                            return;

                        GameObjectInteraction gameObjectUILink = new();
                        gameObjectUILink.ObjectGUID = GetGUID();

                        switch (GetGoInfo().UILink.UILinkType)
                        {
                            case 0:
                                gameObjectUILink.InteractionType = PlayerInteractionType.AdventureJournal;

                                break;
                            case 1:
                                gameObjectUILink.InteractionType = PlayerInteractionType.ObliterumForge;

                                break;
                            case 2:
                                gameObjectUILink.InteractionType = PlayerInteractionType.ScrappingMachine;

                                break;
                            case 3:
                                gameObjectUILink.InteractionType = PlayerInteractionType.ItemInteraction;

                                break;
                            default:
                                break;
                        }

                        player.SendPacket(gameObjectUILink);

                        return;
                    }
                case GameObjectTypes.GatheringNode: //50
                    {
                        Player player = user.ToPlayer();

                        if (player == null)
                            return;

                        GameObjectTemplate info = GetGoInfo();

                        if (!_personalLoot.ContainsKey(player.GetGUID()))
                        {
                            if (info.GatheringNode.chestLoot != 0)
                            {
                                Loot newLoot = new(GetMap(), GetGUID(), LootType.Chest, null);
                                _personalLoot[player.GetGUID()] = newLoot;

                                newLoot.FillLoot(info.GatheringNode.chestLoot, LootStorage.Gameobject, player, true, false, GetLootMode(), GetMap().GetDifficultyLootItemContext());
                            }

                            if (info.GatheringNode.triggeredEvent != 0)
                                GameEvents.Trigger(info.GatheringNode.triggeredEvent, player, this);

                            // triggering linked GO
                            uint trapEntry = info.GatheringNode.linkedTrap;

                            if (trapEntry != 0)
                                TriggeringLinkedGameObject(trapEntry, player);

                            if (info.GatheringNode.xpDifficulty != 0 &&
                                info.GatheringNode.xpDifficulty < 10)
                            {
                                QuestXPRecord questXp = CliDB.QuestXPStorage.LookupByKey(player.GetLevel());

                                if (questXp != null)
                                {
                                    uint xp = Quest.RoundXPValue(questXp.Difficulty[info.GatheringNode.xpDifficulty]);

                                    if (xp != 0)
                                        player.GiveXP(xp, null);
                                }
                            }

                            spellId = info.GatheringNode.spell;
                        }

                        if (_personalLoot.Count >= info.GatheringNode.MaxNumberofLoots)
                        {
                            SetGoState(GameObjectState.Active);
                            SetDynamicFlag(GameObjectDynamicLowFlags.NoInterract);
                        }

                        if (GetLootState() != LootState.Activated)
                        {
                            SetLootState(LootState.Activated, player);

                            if (info.GatheringNode.ObjectDespawnDelay != 0)
                                DespawnOrUnsummon(TimeSpan.FromSeconds(info.GatheringNode.ObjectDespawnDelay));
                        }

                        // Send loot
                        Loot loot = GetLootForPlayer(player);

                        if (loot != null)
                            player.SendLoot(loot);

                        break;
                    }
                default:
                    if (GetGoType() >= GameObjectTypes.Max)
                        Log.outError(LogFilter.Server,
                                     "GameObject.Use(): unit (Type: {0}, Guid: {1}, Name: {2}) tries to use object (Guid: {3}, entry: {4}, Name: {5}) of unknown Type ({6})",
                                     user.GetTypeId(),
                                     user.GetGUID().ToString(),
                                     user.GetName(),
                                     GetGUID().ToString(),
                                     GetEntry(),
                                     GetGoInfo().name,
                                     GetGoType());

                    break;
            }

            if (spellId == 0)
                return;

            if (!Global.SpellMgr.HasSpellInfo(spellId, GetMap().GetDifficultyID()))
            {
                if (!user.IsTypeId(TypeId.Player) ||
                    !Global.OutdoorPvPMgr.HandleCustomSpell(user.ToPlayer(), spellId, this))
                    Log.outError(LogFilter.Server, "WORLD: unknown spell Id {0} at use Action for gameobject (Entry: {1} GoType: {2})", spellId, GetEntry(), GetGoType());
                else
                    Log.outDebug(LogFilter.Outdoorpvp, "WORLD: {0} non-dbc spell was handled by OutdoorPvP", spellId);

                return;
            }

            Player player1 = user.ToPlayer();

            if (player1)
                Global.OutdoorPvPMgr.HandleCustomSpell(player1, spellId, this);

            if (spellCaster != null)
                spellCaster.CastSpell(user, spellId, triggered);
            else
                CastSpell(user, spellId);
        }

        public void SendCustomAnim(uint anim)
        {
            GameObjectCustomAnim customAnim = new();
            customAnim.ObjectGUID = GetGUID();
            customAnim.CustomAnim = anim;
            SendMessageToSet(customAnim, true);
        }

        public bool IsInRange(float x, float y, float z, float radius)
        {
            GameObjectDisplayInfoRecord info = CliDB.GameObjectDisplayInfoStorage.LookupByKey(_goInfo.displayId);

            if (info == null)
                return IsWithinDist3d(x, y, z, radius);

            float sinA = (float)Math.Sin(GetOrientation());
            float cosA = (float)Math.Cos(GetOrientation());
            float dx = x - GetPositionX();
            float dy = y - GetPositionY();
            float dz = z - GetPositionZ();
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            //! Check if the distance between the 2 objects is 0, can happen if both objects are on the same position.
            //! The code below this check wont crash if dist is 0 because 0/0 in float operations is valid, and returns infinite
            if (MathFunctions.fuzzyEq(dist, 0.0f))
                return true;

            float sinB = dx / dist;
            float cosB = dy / dist;
            dx = dist * (cosA * cosB + sinA * sinB);
            dy = dist * (cosA * sinB - sinA * cosB);

            return dx < info.GeoBoxMax.X + radius && dx > info.GeoBoxMin.X - radius && dy < info.GeoBoxMax.Y + radius && dy > info.GeoBoxMin.Y - radius && dz < info.GeoBoxMax.Z + radius && dz > info.GeoBoxMin.Z - radius;
        }

        public uint GetScriptId()
        {
            GameObjectData gameObjectData = GetGameObjectData();

            if (gameObjectData != null)
            {
                uint scriptId = gameObjectData.ScriptId;

                if (scriptId != 0)
                    return scriptId;
            }

            return GetGoInfo().ScriptId;
        }

        public override string GetName(Locale locale = Locale.enUS)
        {
            if (locale != Locale.enUS)
            {
                GameObjectLocale cl = Global.ObjectMgr.GetGameObjectLocale(GetEntry());

                if (cl != null)
                    if (cl.Name.Length > (int)locale &&
                        !cl.Name[(int)locale].IsEmpty())
                        return cl.Name[(int)locale];
            }

            return base.GetName(locale);
        }

        public void UpdatePackedRotation()
        {
            const int PACK_YZ = 1 << 20;
            const int PACK_X = PACK_YZ << 1;

            const int PACK_YZ_MASK = (PACK_YZ << 1) - 1;
            const int PACK_X_MASK = (PACK_X << 1) - 1;

            sbyte w_sign = (sbyte)(_localRotation.W >= 0.0f ? 1 : -1);
            long x = ((int)(_localRotation.X * PACK_X) * w_sign) & PACK_X_MASK;
            long y = ((int)(_localRotation.Y * PACK_YZ) * w_sign) & PACK_YZ_MASK;
            long z = ((int)(_localRotation.Z * PACK_YZ) * w_sign) & PACK_YZ_MASK;
            _packedRotation = z | (y << 21) | (x << 42);
        }

        public void SetLocalRotation(float qx, float qy, float qz, float qw)
        {
            Quaternion rotation = new(qx, qy, qz, qw);
            rotation = Quaternion.Multiply(rotation, 1.0f / MathF.Sqrt(Quaternion.Dot(rotation, rotation)));

            _localRotation.X = rotation.X;
            _localRotation.Y = rotation.Y;
            _localRotation.Z = rotation.Z;
            _localRotation.W = rotation.W;
            UpdatePackedRotation();
        }

        public void SetParentRotation(Quaternion rotation)
        {
            SetUpdateFieldValue(Values.ModifyValue(_gameObjectData).ModifyValue(_gameObjectData.ParentRotation), rotation);
        }

        public void SetLocalRotationAngles(float z_rot, float y_rot, float x_rot)
        {
            Quaternion quat = Quaternion.CreateFromRotationMatrix(Extensions.fromEulerAnglesZYX(z_rot, y_rot, x_rot));
            SetLocalRotation(quat.X, quat.Y, quat.Z, quat.W);
        }

        public Quaternion GetWorldRotation()
        {
            Quaternion localRotation = GetLocalRotation();

            Transport transport = GetTransport<Transport>();

            if (transport != null)
            {
                Quaternion worldRotation = transport.GetWorldRotation();

                Quaternion worldRotationQuat = new(worldRotation.X, worldRotation.Y, worldRotation.Z, worldRotation.W);
                Quaternion localRotationQuat = new(localRotation.X, localRotation.Y, localRotation.Z, localRotation.W);

                Quaternion resultRotation = localRotationQuat * worldRotationQuat;

                return resultRotation;
            }

            return localRotation;
        }

        public override string GetDebugInfo()
        {
            return $"{base.GetDebugInfo()}\nSpawnId: {GetSpawnId()} GoState: {GetGoState()} ScriptId: {GetScriptId()} AIName: {GetAIName()}";
        }

        public bool IsAtInteractDistance(Player player, SpellInfo spell = null)
        {
            if (spell != null ||
                (spell = GetSpellForLock(player)) != null)
            {
                float maxRange = spell.GetMaxRange(spell.IsPositive());

                if (GetGoType() == GameObjectTypes.SpellFocus)
                    return maxRange * maxRange >= GetExactDistSq(player);

                if (CliDB.GameObjectDisplayInfoStorage.ContainsKey(GetGoInfo().displayId))
                    return IsAtInteractDistance(player, maxRange);
            }

            return IsAtInteractDistance(player, GetInteractionDistance());
        }

        public bool IsWithinDistInMap(Player player)
        {
            return IsInMap(player) && InSamePhase(player) && IsAtInteractDistance(player);
        }

        public SpellInfo GetSpellForLock(Player player)
        {
            if (!player)
                return null;

            uint lockId = GetGoInfo().GetLockId();

            if (lockId == 0)
                return null;

            var lockEntry = CliDB.LockStorage.LookupByKey(lockId);

            if (lockEntry == null)
                return null;

            for (byte i = 0; i < SharedConst.MaxLockCase; ++i)
            {
                if (lockEntry.LockType[i] == 0)
                    continue;

                if (lockEntry.LockType[i] == (byte)LockKeyType.Spell)
                {
                    SpellInfo spell = Global.SpellMgr.GetSpellInfo((uint)lockEntry.Index[i], GetMap().GetDifficultyID());

                    if (spell != null)
                        return spell;
                }

                if (lockEntry.LockType[i] != (byte)LockKeyType.Skill)
                    break;

                foreach (var playerSpell in player.GetSpellMap())
                {
                    SpellInfo spell = Global.SpellMgr.GetSpellInfo(playerSpell.Key, GetMap().GetDifficultyID());

                    if (spell != null)
                        foreach (var effect in spell.GetEffects())
                            if (effect.Effect == SpellEffectName.OpenLock &&
                                effect.MiscValue == lockEntry.Index[i])
                                if (effect.CalcValue(player) >= lockEntry.Skill[i])
                                    return spell;
                }
            }

            return null;
        }

        public void ModifyHealth(int change, WorldObject attackerOrHealer = null, uint spellId = 0)
        {
            if (_goValue.Building.MaxHealth == 0 ||
                change == 0)
                return;

            // prevent double destructions of the same object
            if (change < 0 &&
                _goValue.Building.Health == 0)
                return;

            if (_goValue.Building.Health + change <= 0)
                _goValue.Building.Health = 0;
            else if (_goValue.Building.Health + change >= _goValue.Building.MaxHealth)
                _goValue.Building.Health = _goValue.Building.MaxHealth;
            else
                _goValue.Building.Health += (uint)change;

            // Set the health bar, value = 255 * healthPct;
            SetGoAnimProgress(_goValue.Building.Health * 255 / _goValue.Building.MaxHealth);

            // dealing Damage, send packet
            Player player = attackerOrHealer?.GetCharmerOrOwnerPlayerOrPlayerItself();

            if (player != null)
            {
                DestructibleBuildingDamage packet = new();
                packet.Caster = attackerOrHealer.GetGUID(); // todo: this can be a GameObject
                packet.Target = GetGUID();
                packet.Damage = -change;
                packet.Owner = player.GetGUID();
                packet.SpellID = spellId;
                player.SendPacket(packet);
            }

            if (change < 0 &&
                GetGoInfo().DestructibleBuilding.DamageEvent != 0)
                GameEvents.Trigger(GetGoInfo().DestructibleBuilding.DamageEvent, attackerOrHealer, this);

            GameObjectDestructibleState newState = GetDestructibleState();

            if (_goValue.Building.Health == 0)
                newState = GameObjectDestructibleState.Destroyed;
            else if (_goValue.Building.Health < _goValue.Building.MaxHealth / 2)
                newState = GameObjectDestructibleState.Damaged;
            else if (_goValue.Building.Health == _goValue.Building.MaxHealth)
                newState = GameObjectDestructibleState.Intact;

            if (newState == GetDestructibleState())
                return;

            SetDestructibleState(newState, attackerOrHealer, false);
        }

        public void SetDestructibleState(GameObjectDestructibleState state, WorldObject attackerOrHealer = null, bool setHealth = false)
        {
            // the user calling this must know he is already operating on destructible gameobject
            Cypher.Assert(GetGoType() == GameObjectTypes.DestructibleBuilding);

            switch (state)
            {
                case GameObjectDestructibleState.Intact:
                    RemoveFlag(GameObjectFlags.Damaged | GameObjectFlags.Destroyed);
                    SetDisplayId(_goInfo.displayId);

                    if (setHealth)
                    {
                        _goValue.Building.Health = _goValue.Building.MaxHealth;
                        SetGoAnimProgress(255);
                    }

                    EnableCollision(true);

                    break;
                case GameObjectDestructibleState.Damaged:
                    {
                        if (GetGoInfo().DestructibleBuilding.DamagedEvent != 0)
                            GameEvents.Trigger(GetGoInfo().DestructibleBuilding.DamagedEvent, attackerOrHealer, this);

                        GetAI().Damaged(attackerOrHealer, _goInfo.DestructibleBuilding.DamagedEvent);

                        RemoveFlag(GameObjectFlags.Destroyed);
                        SetFlag(GameObjectFlags.Damaged);

                        uint modelId = _goInfo.displayId;
                        DestructibleModelDataRecord modelData = CliDB.DestructibleModelDataStorage.LookupByKey(_goInfo.DestructibleBuilding.DestructibleModelRec);

                        if (modelData != null)
                            if (modelData.State1Wmo != 0)
                                modelId = modelData.State1Wmo;

                        SetDisplayId(modelId);

                        if (setHealth)
                        {
                            _goValue.Building.Health = 10000; //m_goInfo.DestructibleBuilding.damagedNumHits;
                            uint maxHealth = _goValue.Building.MaxHealth;

                            // in this case current health is 0 anyway so just prevent crashing here
                            if (maxHealth == 0)
                                maxHealth = 1;

                            SetGoAnimProgress(_goValue.Building.Health * 255 / maxHealth);
                        }

                        break;
                    }
                case GameObjectDestructibleState.Destroyed:
                    {
                        if (GetGoInfo().DestructibleBuilding.DestroyedEvent != 0)
                            GameEvents.Trigger(GetGoInfo().DestructibleBuilding.DestroyedEvent, attackerOrHealer, this);

                        GetAI().Destroyed(attackerOrHealer, _goInfo.DestructibleBuilding.DestroyedEvent);

                        Player player = attackerOrHealer?.GetCharmerOrOwnerPlayerOrPlayerItself();

                        if (player)
                        {
                            Battleground bg = player.GetBattleground();

                            bg?.DestroyGate(player, this);
                        }

                        RemoveFlag(GameObjectFlags.Damaged);
                        SetFlag(GameObjectFlags.Destroyed);

                        uint modelId = _goInfo.displayId;
                        DestructibleModelDataRecord modelData = CliDB.DestructibleModelDataStorage.LookupByKey(_goInfo.DestructibleBuilding.DestructibleModelRec);

                        if (modelData != null)
                            if (modelData.State2Wmo != 0)
                                modelId = modelData.State2Wmo;

                        SetDisplayId(modelId);

                        if (setHealth)
                        {
                            _goValue.Building.Health = 0;
                            SetGoAnimProgress(0);
                        }

                        EnableCollision(false);

                        break;
                    }
                case GameObjectDestructibleState.Rebuilding:
                    {
                        if (GetGoInfo().DestructibleBuilding.RebuildingEvent != 0)
                            GameEvents.Trigger(GetGoInfo().DestructibleBuilding.RebuildingEvent, attackerOrHealer, this);

                        RemoveFlag(GameObjectFlags.Damaged | GameObjectFlags.Destroyed);

                        uint modelId = _goInfo.displayId;
                        DestructibleModelDataRecord modelData = CliDB.DestructibleModelDataStorage.LookupByKey(_goInfo.DestructibleBuilding.DestructibleModelRec);

                        if (modelData != null)
                            if (modelData.State3Wmo != 0)
                                modelId = modelData.State3Wmo;

                        SetDisplayId(modelId);

                        // restores to full health
                        if (setHealth)
                        {
                            _goValue.Building.Health = _goValue.Building.MaxHealth;
                            SetGoAnimProgress(255);
                        }

                        EnableCollision(true);

                        break;
                    }
            }
        }

        public void SetLootState(LootState state, Unit unit = null)
        {
            _lootState = state;
            _lootStateUnitGUID = unit ? unit.GetGUID() : ObjectGuid.Empty;
            GetAI().OnLootStateChanged((uint)state, unit);

            // Start restock timer if the chest is partially looted or not looted at all
            if (GetGoType() == GameObjectTypes.Chest &&
                state == LootState.Activated &&
                GetGoInfo().Chest.chestRestockTime > 0 &&
                _restockTime == 0)
                _restockTime = GameTime.GetGameTime() + GetGoInfo().Chest.chestRestockTime;

            // only set collision for doors on SetGoState
            if (GetGoType() == GameObjectTypes.Door)
                return;

            if (_model != null)
            {
                bool collision = false;

                // Use the current go State
                if ((GetGoState() != GameObjectState.Ready && (state == LootState.Activated || state == LootState.JustDeactivated)) ||
                    state == LootState.Ready)
                    collision = !collision;

                EnableCollision(collision);
            }
        }

        public bool IsFullyLooted()
        {
            if (loot != null &&
                !loot.IsLooted())
                return false;

            foreach (var (_, loot) in _personalLoot)
                if (!loot.IsLooted())
                    return false;

            return true;
        }

        public void OnLootRelease(Player looter)
        {
            switch (GetGoType())
            {
                case GameObjectTypes.Chest:
                    {
                        GameObjectTemplate goInfo = GetGoInfo();

                        if (goInfo.Chest.consumable == 0 &&
                            goInfo.Chest.chestPersonalLoot != 0)
                            DespawnForPlayer(looter,
                                             goInfo.Chest.chestRestockTime != 0
                                                 ? TimeSpan.FromSeconds(goInfo.Chest.chestRestockTime)
                                                 : TimeSpan.FromSeconds(_respawnDelayTime)); // not hiding this object permanently to prevent infinite growth of _perPlayerState

                        // while also maintaining some sort of cheater protection (not getting rid of entries on logout)
                        break;
                    }
                case GameObjectTypes.GatheringNode:
                    {
                        SetGoStateFor(GameObjectState.Active, looter);

                        ObjectFieldData objMask = new();
                        GameObjectFieldData goMask = new();
                        objMask.MarkChanged(objMask.DynamicFlags);

                        UpdateData udata = new(GetMapId());
                        BuildValuesUpdateForPlayerWithMask(udata, objMask.GetUpdateMask(), goMask.GetUpdateMask(), looter);
                        udata.BuildPacket(out UpdateObject packet);
                        looter.SendPacket(packet);

                        break;
                    }
            }
        }

        public void SetGoState(GameObjectState state)
        {
            GameObjectState oldState = GetGoState();
            SetUpdateFieldValue(Values.ModifyValue(_gameObjectData).ModifyValue(_gameObjectData.State), (sbyte)state);

            GetAI()?.OnStateChanged(state);

            _goTypeImpl?.OnStateChanged(oldState, state);

            if (_model != null &&
                !IsTransport())
            {
                if (!IsInWorld)
                    return;

                // startOpen determines whether we are going to add or remove the LoS on activation
                bool collision = false;

                if (state == GameObjectState.Ready)
                    collision = !collision;

                EnableCollision(collision);
            }
        }

        public GameObjectState GetGoStateFor(ObjectGuid viewer)
        {
            if (_perPlayerState != null)
            {
                PerPlayerState state = _perPlayerState.LookupByKey(viewer);

                if (state != null &&
                    state.State.HasValue)
                    return state.State.Value;
            }

            return GetGoState();
        }

        public void SetDisplayId(uint displayid)
        {
            SetUpdateFieldValue(Values.ModifyValue(_gameObjectData).ModifyValue(_gameObjectData.DisplayID), displayid);
            UpdateModel();
        }

        public byte GetNameSetId()
        {
            switch (GetGoType())
            {
                case GameObjectTypes.DestructibleBuilding:
                    DestructibleModelDataRecord modelData = CliDB.DestructibleModelDataStorage.LookupByKey(_goInfo.DestructibleBuilding.DestructibleModelRec);

                    if (modelData != null)
                        switch (GetDestructibleState())
                        {
                            case GameObjectDestructibleState.Intact:
                                return modelData.State0NameSet;
                            case GameObjectDestructibleState.Damaged:
                                return modelData.State1NameSet;
                            case GameObjectDestructibleState.Destroyed:
                                return modelData.State2NameSet;
                            case GameObjectDestructibleState.Rebuilding:
                                return modelData.State3NameSet;
                            default:
                                break;
                        }

                    break;
                case GameObjectTypes.GarrisonBuilding:
                case GameObjectTypes.GarrisonPlot:
                case GameObjectTypes.PhaseableMo:
                    var flags = (GameObjectFlags)(uint)_gameObjectData.Flags;

                    return (byte)(((int)flags >> 8) & 0xF);
                default:
                    break;
            }

            return 0;
        }

        public bool IsLootAllowedFor(Player player)
        {
            Loot loot = GetLootForPlayer(player);

            if (loot != null) // check only if loot was already generated
            {
                if (loot.IsLooted()) // nothing to loot or everything looted.
                    return false;

                if (!loot.HasAllowedLooter(GetGUID()) ||
                    (!loot.HasItemForAll() && !loot.HasItemFor(player))) // no loot in chest for this player
                    return false;
            }

            if (HasLootRecipient())
                return _tapList.Contains(player.GetGUID()); // if go doesnt have group bound it means it was solo killed by someone else

            return true;
        }

        public override Loot GetLootForPlayer(Player player)
        {
            if (_personalLoot.Empty())
                return loot;

            return _personalLoot.LookupByKey(player.GetGUID());
        }

        public void SetLinkedTrap(GameObject linkedTrap)
        {
            _linkedTrap = linkedTrap.GetGUID();
        }

        public GameObject GetLinkedTrap()
        {
            return ObjectAccessor.GetGameObject(this, _linkedTrap);
        }

        public override void BuildValuesCreate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

            buffer.WriteUInt8((byte)flags);
            ObjectData.WriteCreate(buffer, flags, this, target);
            _gameObjectData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

            buffer.WriteUInt32(Values.GetChangedObjectTypeMask());

            if (Values.HasChanged(TypeId.Object))
                ObjectData.WriteUpdate(buffer, flags, this, target);

            if (Values.HasChanged(TypeId.GameObject))
                _gameObjectData.WriteUpdate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedGameObjectMask, Player target)
        {
            UpdateMask valuesMask = new((int)TypeId.Max);

            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            if (requestedGameObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.GameObject);

            WorldPacket buffer = new();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                ObjectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.GameObject])
                _gameObjectData.WriteUpdate(buffer, requestedGameObjectMask, true, this, target);

            WorldPacket buffer1 = new();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }

        public override void ClearUpdateMask(bool remove)
        {
            Values.ClearChangesMask(_gameObjectData);
            base.ClearUpdateMask(remove);
        }

        public List<uint> GetPauseTimes()
        {
            GameObjectType.Transport transport = _goTypeImpl as GameObjectType.Transport;

            if (transport != null)
                return transport.GetPauseTimes();

            return null;
        }

        public void SetPathProgressForClient(float progress)
        {
            DoWithSuppressingObjectUpdates(() =>
                                           {
                                               ObjectFieldData dynflagMask = new();
                                               dynflagMask.MarkChanged(ObjectData.DynamicFlags);
                                               bool marked = (ObjectData.GetUpdateMask() & dynflagMask.GetUpdateMask()).IsAnySet();

                                               uint dynamicFlags = (uint)GetDynamicFlags();
                                               dynamicFlags &= 0xFFFF; // remove high bits
                                               dynamicFlags |= (uint)(progress * 65535.0f) << 16;
                                               ReplaceAllDynamicFlags((GameObjectDynamicLowFlags)dynamicFlags);

                                               if (!marked)
                                                   ObjectData.ClearChanged(ObjectData.DynamicFlags);
                                           });
        }

        public void GetRespawnPosition(out float x, out float y, out float z, out float ori)
        {
            if (_goData != null)
                _goData.SpawnPoint.GetPosition(out x, out y, out z, out ori);
            else
                GetPosition(out x, out y, out z, out ori);
        }

        public ITransport ToTransportBase()
        {
            switch (GetGoType())
            {
                case GameObjectTypes.Transport:
                    return (GameObjectType.Transport)_goTypeImpl;
                case GameObjectTypes.MapObjTransport:
                    return (Transport)this;
                default:
                    break;
            }

            return null;
        }

        public void AfterRelocation()
        {
            UpdateModelPosition();
            UpdatePositionData();

            _goTypeImpl?.OnRelocated();

            UpdateObjectVisibility(false);
        }

        public float GetInteractionDistance()
        {
            if (GetGoInfo().GetInteractRadiusOverride() != 0)
                return (float)GetGoInfo().GetInteractRadiusOverride() / 100.0f;

            switch (GetGoType())
            {
                case GameObjectTypes.AreaDamage:
                    return 0.0f;
                case GameObjectTypes.QuestGiver:
                case GameObjectTypes.Text:
                case GameObjectTypes.FlagStand:
                case GameObjectTypes.FlagDrop:
                case GameObjectTypes.MiniGame:
                    return 5.5555553f;
                case GameObjectTypes.Chair:
                case GameObjectTypes.BarberChair:
                    return 3.0f;
                case GameObjectTypes.FishingNode:
                    return 100.0f;
                case GameObjectTypes.FishingHole:
                    return 20.0f + SharedConst.ContactDistance; // max spell range
                case GameObjectTypes.Camera:
                case GameObjectTypes.MapObject:
                case GameObjectTypes.DungeonDifficulty:
                case GameObjectTypes.DestructibleBuilding:
                case GameObjectTypes.Door:
                    return 5.0f;
                // Following values are not blizzlike
                case GameObjectTypes.GuildBank:
                case GameObjectTypes.Mailbox:
                    // Successful mailbox interaction is rather Critical to the client, failing it will start a minute-long cooldown until the next mail query may be executed.
                    // And since movement info update is not sent with mailbox interaction query, server may find the player outside of interaction range. Thus we increase it.
                    return 10.0f; // 5.0f is blizzlike
                default:
                    return SharedConst.InteractionDistance;
            }
        }

        public void UpdateModelPosition()
        {
            if (_model == null)
                return;

            if (GetMap().ContainsGameObjectModel(_model))
            {
                GetMap().RemoveGameObjectModel(_model);
                _model.UpdatePosition();
                GetMap().InsertGameObjectModel(_model);
            }
        }

        public void SetAnimKitId(ushort animKitId, bool oneshot)
        {
            if (_animKitId == animKitId)
                return;

            if (animKitId != 0 &&
                !CliDB.AnimKitStorage.ContainsKey(animKitId))
                return;

            if (!oneshot)
                _animKitId = animKitId;
            else
                _animKitId = 0;

            GameObjectActivateAnimKit activateAnimKit = new();
            activateAnimKit.ObjectGUID = GetGUID();
            activateAnimKit.AnimKitID = animKitId;
            activateAnimKit.Maintain = !oneshot;
            SendMessageToSet(activateAnimKit, true);
        }

        public void SetSpellVisualId(uint spellVisualId, ObjectGuid activatorGuid = default)
        {
            SetUpdateFieldValue(Values.ModifyValue(_gameObjectData).ModifyValue(_gameObjectData.SpellVisualID), spellVisualId);

            GameObjectPlaySpellVisual packet = new();
            packet.ObjectGUID = GetGUID();
            packet.ActivatorGUID = activatorGuid;
            packet.SpellVisualID = spellVisualId;
            SendMessageToSet(packet, true);
        }

        public void AssaultCapturePoint(Player player)
        {
            if (!CanInteractWithCapturePoint(player))
                return;

            GameObjectAI ai = GetAI();

            if (ai != null)
                if (ai.OnCapturePointAssaulted(player))
                    return;

            // only supported in battlegrounds
            Battleground battleground = null;
            BattlegroundMap map = GetMap().ToBattlegroundMap();

            if (map != null)
            {
                Battleground bg = map.GetBG();

                if (bg != null)
                    battleground = bg;
            }

            if (!battleground)
                return;

            // Cancel current timer
            _goValue.CapturePoint.AssaultTimer = 0;

            if (player.GetBGTeam() == Team.Horde)
            {
                if (_goValue.CapturePoint.LastTeamCapture == TeamId.Horde)
                {
                    // defended. capture instantly.
                    _goValue.CapturePoint.State = BattlegroundCapturePointState.HordeCaptured;
                    battleground.SendBroadcastText(GetGoInfo().CapturePoint.DefendedBroadcastHorde, ChatMsg.BgSystemHorde, player);
                    UpdateCapturePoint();

                    if (GetGoInfo().CapturePoint.DefendedEventHorde != 0)
                        GameEvents.Trigger(GetGoInfo().CapturePoint.DefendedEventHorde, player, this);

                    return;
                }

                switch (_goValue.CapturePoint.State)
                {
                    case BattlegroundCapturePointState.Neutral:
                    case BattlegroundCapturePointState.AllianceCaptured:
                    case BattlegroundCapturePointState.ContestedAlliance:
                        _goValue.CapturePoint.State = BattlegroundCapturePointState.ContestedHorde;
                        battleground.SendBroadcastText(GetGoInfo().CapturePoint.AssaultBroadcastHorde, ChatMsg.BgSystemHorde, player);
                        UpdateCapturePoint();

                        if (GetGoInfo().CapturePoint.ContestedEventHorde != 0)
                            GameEvents.Trigger(GetGoInfo().CapturePoint.ContestedEventHorde, player, this);

                        _goValue.CapturePoint.AssaultTimer = GetGoInfo().CapturePoint.CaptureTime;

                        break;
                    default:
                        break;
                }
            }
            else
            {
                if (_goValue.CapturePoint.LastTeamCapture == TeamId.Alliance)
                {
                    // defended. capture instantly.
                    _goValue.CapturePoint.State = BattlegroundCapturePointState.AllianceCaptured;
                    battleground.SendBroadcastText(GetGoInfo().CapturePoint.DefendedBroadcastAlliance, ChatMsg.BgSystemAlliance, player);
                    UpdateCapturePoint();

                    if (GetGoInfo().CapturePoint.DefendedEventAlliance != 0)
                        GameEvents.Trigger(GetGoInfo().CapturePoint.DefendedEventAlliance, player, this);

                    return;
                }

                switch (_goValue.CapturePoint.State)
                {
                    case BattlegroundCapturePointState.Neutral:
                    case BattlegroundCapturePointState.HordeCaptured:
                    case BattlegroundCapturePointState.ContestedHorde:
                        _goValue.CapturePoint.State = BattlegroundCapturePointState.ContestedAlliance;
                        battleground.SendBroadcastText(GetGoInfo().CapturePoint.AssaultBroadcastAlliance, ChatMsg.BgSystemAlliance, player);
                        UpdateCapturePoint();

                        if (GetGoInfo().CapturePoint.ContestedEventAlliance != 0)
                            GameEvents.Trigger(GetGoInfo().CapturePoint.ContestedEventAlliance, player, this);

                        _goValue.CapturePoint.AssaultTimer = GetGoInfo().CapturePoint.CaptureTime;

                        break;
                    default:
                        break;
                }
            }
        }

        public bool CanInteractWithCapturePoint(Player target)
        {
            if (_goInfo.type != GameObjectTypes.CapturePoint)
                return false;

            if (_goValue.CapturePoint.State == BattlegroundCapturePointState.Neutral)
                return true;

            if (target.GetBGTeam() == Team.Horde)
                return _goValue.CapturePoint.State == BattlegroundCapturePointState.ContestedAlliance || _goValue.CapturePoint.State == BattlegroundCapturePointState.AllianceCaptured;

            // For Alliance players
            return _goValue.CapturePoint.State == BattlegroundCapturePointState.ContestedHorde || _goValue.CapturePoint.State == BattlegroundCapturePointState.HordeCaptured;
        }

        public bool MeetsInteractCondition(Player user)
        {
            if (_goInfo.GetConditionID1() == 0)
                return true;

            PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(_goInfo.GetConditionID1());

            if (playerCondition != null)
                if (!ConditionManager.IsPlayerMeetingCondition(user, playerCondition))
                    return false;

            return true;
        }

        public override ushort GetAIAnimKitId()
        {
            return _animKitId;
        }

        public uint GetWorldEffectID()
        {
            return _worldEffectID;
        }

        public void SetWorldEffectID(uint worldEffectID)
        {
            _worldEffectID = worldEffectID;
        }

        public GameObjectTemplate GetGoInfo()
        {
            return _goInfo;
        }

        public GameObjectTemplateAddon GetTemplateAddon()
        {
            return _goTemplateAddon;
        }

        public GameObjectData GetGameObjectData()
        {
            return _goData;
        }

        public GameObjectValue GetGoValue()
        {
            return _goValue;
        }

        public ulong GetSpawnId()
        {
            return _spawnId;
        }

        public Quaternion GetLocalRotation()
        {
            return _localRotation;
        }

        public long GetPackedLocalRotation()
        {
            return _packedRotation;
        }

        public void SetOwnerGUID(ObjectGuid owner)
        {
            // _owner already found and different than expected owner - remove object from old owner
            if (!owner.IsEmpty() &&
                !GetOwnerGUID().IsEmpty() &&
                GetOwnerGUID() != owner)
                Cypher.Assert(false);

            _spawnedByDefault = false; // all object with owner is despawned after delay
            SetUpdateFieldValue(Values.ModifyValue(_gameObjectData).ModifyValue(_gameObjectData.CreatedBy), owner);
        }

        public override ObjectGuid GetOwnerGUID()
        {
            return _gameObjectData.CreatedBy;
        }

        public void SetSpellId(uint id)
        {
            _spawnedByDefault = false; // all summoned object is despawned after delay
            _spellId = id;
        }

        public uint GetSpellId()
        {
            return _spellId;
        }

        public long GetRespawnTime()
        {
            return _respawnTime;
        }

        public long GetRespawnTimeEx()
        {
            long now = GameTime.GetGameTime();

            if (_respawnTime > now)
                return _respawnTime;
            else
                return now;
        }

        public void SetRespawnTime(int respawn)
        {
            _respawnTime = respawn > 0 ? GameTime.GetGameTime() + respawn : 0;
            _respawnDelayTime = (uint)(respawn > 0 ? respawn : 0);

            if (respawn != 0 &&
                !_spawnedByDefault)
                UpdateObjectVisibility(true);
        }

        public bool IsSpawned()
        {
            return _respawnDelayTime == 0 ||
                   (_respawnTime > 0 && !_spawnedByDefault) ||
                   (_respawnTime == 0 && _spawnedByDefault);
        }

        public bool IsSpawnedByDefault()
        {
            return _spawnedByDefault;
        }

        public void SetSpawnedByDefault(bool b)
        {
            _spawnedByDefault = b;
        }

        public uint GetRespawnDelay()
        {
            return _respawnDelayTime;
        }

        public bool HasFlag(GameObjectFlags flags)
        {
            return (_gameObjectData.Flags & (uint)flags) != 0;
        }

        public void SetFlag(GameObjectFlags flags)
        {
            SetUpdateFieldFlagValue(Values.ModifyValue(_gameObjectData).ModifyValue(_gameObjectData.Flags), (uint)flags);
        }

        public void RemoveFlag(GameObjectFlags flags)
        {
            RemoveUpdateFieldFlagValue(Values.ModifyValue(_gameObjectData).ModifyValue(_gameObjectData.Flags), (uint)flags);
        }

        public void ReplaceAllFlags(GameObjectFlags flags)
        {
            SetUpdateFieldValue(Values.ModifyValue(_gameObjectData).ModifyValue(_gameObjectData.Flags), (uint)flags);
        }

        public void SetLevel(uint level)
        {
            SetUpdateFieldValue(Values.ModifyValue(_gameObjectData).ModifyValue(_gameObjectData.Level), level);
        }

        public GameObjectDynamicLowFlags GetDynamicFlags()
        {
            return (GameObjectDynamicLowFlags)(uint)ObjectData.DynamicFlags;
        }

        public bool HasDynamicFlag(GameObjectDynamicLowFlags flag)
        {
            return (ObjectData.DynamicFlags & (uint)flag) != 0;
        }

        public void SetDynamicFlag(GameObjectDynamicLowFlags flag)
        {
            SetUpdateFieldFlagValue(Values.ModifyValue(ObjectData).ModifyValue(ObjectData.DynamicFlags), (uint)flag);
        }

        public void RemoveDynamicFlag(GameObjectDynamicLowFlags flag)
        {
            RemoveUpdateFieldFlagValue(Values.ModifyValue(ObjectData).ModifyValue(ObjectData.DynamicFlags), (uint)flag);
        }

        public void ReplaceAllDynamicFlags(GameObjectDynamicLowFlags flag)
        {
            SetUpdateFieldValue(Values.ModifyValue(ObjectData).ModifyValue(ObjectData.DynamicFlags), (uint)flag);
        }

        public GameObjectTypes GetGoType()
        {
            return (GameObjectTypes)(sbyte)_gameObjectData.TypeID;
        }

        public void SetGoType(GameObjectTypes type)
        {
            SetUpdateFieldValue(Values.ModifyValue(_gameObjectData).ModifyValue(_gameObjectData.TypeID), (sbyte)type);
        }

        public GameObjectState GetGoState()
        {
            return (GameObjectState)(sbyte)_gameObjectData.State;
        }

        public void SetGoAnimProgress(uint animprogress)
        {
            SetUpdateFieldValue(Values.ModifyValue(_gameObjectData).ModifyValue(_gameObjectData.PercentHealth), (byte)animprogress);
        }

        public LootState GetLootState()
        {
            return _lootState;
        }

        public LootModes GetLootMode()
        {
            return _LootMode;
        }

        public void AddToSkillupList(ObjectGuid PlayerGuid)
        {
            _SkillupList.Add(PlayerGuid);
        }

        public bool IsInSkillupList(ObjectGuid PlayerGuid)
        {
            foreach (var i in _SkillupList)
                if (i == PlayerGuid)
                    return true;

            return false;
        }

        public void AddUse()
        {
            ++_usetimes;
        }

        public uint GetUseCount()
        {
            return _usetimes;
        }

        public override uint GetLevelForTarget(WorldObject target)
        {
            Unit owner = GetOwner();

            if (owner != null)
                return owner.GetLevelForTarget(target);

            if (GetGoType() == GameObjectTypes.Trap)
            {
                Player player = target.ToPlayer();

                if (player != null)
                {
                    var userLevels = Global.DB2Mgr.GetContentTuningData(GetGoInfo().ContentTuningId, player.PlayerData.CtrOptions.GetValue().ContentTuningConditionMask);

                    if (userLevels.HasValue)
                        return (byte)Math.Clamp(player.GetLevel(), userLevels.Value.MinLevel, userLevels.Value.MaxLevel);
                }

                Unit targetUnit = target.ToUnit();

                if (targetUnit != null)
                    return targetUnit.GetLevel();
            }

            return 1;
        }

        public GameObjectAI GetAI()
        {
            return _AI;
        }

        public T GetAI<T>() where T : GameObjectAI
        {
            return (T)_AI;
        }

        public uint GetDisplayId()
        {
            return _gameObjectData.DisplayID;
        }

        public override uint GetFaction()
        {
            return _gameObjectData.FactionTemplate;
        }

        public override void SetFaction(uint faction)
        {
            SetUpdateFieldValue(Values.ModifyValue(_gameObjectData).ModifyValue(_gameObjectData.FactionTemplate), faction);
        }

        public override float GetStationaryX()
        {
            return StationaryPosition.GetPositionX();
        }

        public override float GetStationaryY()
        {
            return StationaryPosition.GetPositionY();
        }

        public override float GetStationaryZ()
        {
            return StationaryPosition.GetPositionZ();
        }

        public override float GetStationaryO()
        {
            return StationaryPosition.GetOrientation();
        }

        public Position GetStationaryPosition()
        {
            return StationaryPosition;
        }

        public void RelocateStationaryPosition(float x, float y, float z, float o)
        {
            StationaryPosition.Relocate(x, y, z, o);
        }

        //! Object distance/size - overridden from Object._IsWithinDist. Needs to take in account proper GO size.
        public override bool _IsWithinDist(WorldObject obj, float dist2compare, bool is3D, bool incOwnRadius, bool incTargetRadius)
        {
            //! Following check does check 3d distance
            return IsInRange(obj.GetPositionX(), obj.GetPositionY(), obj.GetPositionZ(), dist2compare);
        }

        public void CreateModel()
        {
            _model = GameObjectModel.Create(new GameObjectModelOwnerImpl(this));

            if (_model != null &&
                _model.IsMapObject())
                SetFlag(GameObjectFlags.MapObject);
        }

        public bool GetRespawnCompatibilityMode()
        {
            return _respawnCompatibilityMode;
        }

        private void RemoveFromOwner()
        {
            ObjectGuid ownerGUID = GetOwnerGUID();

            if (ownerGUID.IsEmpty())
                return;

            Unit owner = Global.ObjAccessor.GetUnit(this, ownerGUID);

            if (owner)
            {
                owner.RemoveGameObject(this, false);
                Cypher.Assert(GetOwnerGUID().IsEmpty());

                return;
            }

            // This happens when a mage portal is despawned after the caster changes map (for example using the portal)
            Log.outDebug(LogFilter.Server,
                         "Removed GameObject (GUID: {0} Entry: {1} SpellId: {2} LinkedGO: {3}) that just lost any reference to the owner {4} GO list",
                         GetGUID().ToString(),
                         GetGoInfo().entry,
                         _spellId,
                         GetGoInfo().GetLinkedGameObjectEntry(),
                         ownerGUID.ToString());

            SetOwnerGUID(ObjectGuid.Empty);
        }

        private bool Create(uint entry, Map map, Position pos, Quaternion rotation, uint animProgress, GameObjectState goState, uint artKit, bool dynamic, ulong spawnid)
        {
            Cypher.Assert(map);
            SetMap(map);

            Relocate(pos);
            StationaryPosition.Relocate(pos);

            if (!IsPositionValid())
            {
                Log.outError(LogFilter.Server, "Gameobject (Spawn Id: {0} Entry: {1}) not created. Suggested coordinates isn't valid (X: {2} Y: {3})", GetSpawnId(), entry, pos.GetPositionX(), pos.GetPositionY());

                return false;
            }

            // Set if this object can handle dynamic spawns
            if (!dynamic)
                SetRespawnCompatibilityMode();

            UpdatePositionData();

            SetZoneScript();

            if (ZoneScript != null)
            {
                entry = ZoneScript.GetGameObjectEntry(_spawnId, entry);

                if (entry == 0)
                    return false;
            }

            GameObjectTemplate goInfo = Global.ObjectMgr.GetGameObjectTemplate(entry);

            if (goInfo == null)
            {
                Log.outError(LogFilter.Sql, "Gameobject (Spawn Id: {0} Entry: {1}) not created: non-existing entry in `gameobject_template`. Map: {2} (X: {3} Y: {4} Z: {5})", GetSpawnId(), entry, map.GetId(), pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ());

                return false;
            }

            if (goInfo.type == GameObjectTypes.MapObjTransport)
            {
                Log.outError(LogFilter.Sql, "Gameobject (Spawn Id: {0} Entry: {1}) not created: gameobject Type GAMEOBJECT_TYPE_MAP_OBJ_TRANSPORT cannot be manually created.", GetSpawnId(), entry);

                return false;
            }

            ObjectGuid guid;

            if (goInfo.type != GameObjectTypes.Transport)
            {
                guid = ObjectGuid.Create(HighGuid.GameObject, map.GetId(), goInfo.entry, map.GenerateLowGuid(HighGuid.GameObject));
            }
            else
            {
                guid = ObjectGuid.Create(HighGuid.Transport, map.GenerateLowGuid(HighGuid.Transport));
                UpdateFlag.ServerTime = true;
            }

            _Create(guid);

            _goInfo = goInfo;
            _goTemplateAddon = Global.ObjectMgr.GetGameObjectTemplateAddon(entry);

            if (goInfo.type >= GameObjectTypes.Max)
            {
                Log.outError(LogFilter.Sql, "Gameobject (Spawn Id: {0} Entry: {1}) not created: non-existing GO Type '{2}' in `gameobject_template`. It will crash client if created.", GetSpawnId(), entry, goInfo.type);

                return false;
            }

            SetLocalRotation(rotation.X, rotation.Y, rotation.Z, rotation.W);
            GameObjectAddon gameObjectAddon = Global.ObjectMgr.GetGameObjectAddon(GetSpawnId());

            // For most of gameobjects is (0, 0, 0, 1) quaternion, there are only some transports with not standard rotation
            Quaternion parentRotation = Quaternion.Identity;

            if (gameObjectAddon != null)
                parentRotation = gameObjectAddon.ParentRotation;

            SetParentRotation(parentRotation);

            SetObjectScale(goInfo.size);

            GameObjectOverride goOverride = GetGameObjectOverride();

            if (goOverride != null)
            {
                SetFaction(goOverride.Faction);
                ReplaceAllFlags(goOverride.Flags);
            }

            if (_goTemplateAddon != null)
            {
                if (_goTemplateAddon.WorldEffectID != 0)
                {
                    UpdateFlag.GameObject = true;
                    SetWorldEffectID(_goTemplateAddon.WorldEffectID);
                }

                if (_goTemplateAddon.AIAnimKitID != 0)
                    _animKitId = (ushort)_goTemplateAddon.AIAnimKitID;
            }

            SetEntry(goInfo.entry);

            // set Name for logs usage, doesn't affect anything ingame
            SetName(goInfo.name);

            SetDisplayId(goInfo.displayId);

            CreateModel();

            // GAMEOBJECT_BYTES_1, index at 0, 1, 2 and 3
            SetGoType(goInfo.type);
            _prevGoState = goState;
            SetGoState(goState);
            SetGoArtKit(artKit);

            SetUpdateFieldValue(Values.ModifyValue(_gameObjectData).ModifyValue(_gameObjectData.SpawnTrackingStateAnimID), Global.DB2Mgr.GetEmptyAnimStateID());

            switch (goInfo.type)
            {
                case GameObjectTypes.FishingHole:
                    SetGoAnimProgress(animProgress);
                    _goValue.FishingHole.MaxOpens = RandomHelper.URand(GetGoInfo().FishingHole.minRestock, GetGoInfo().FishingHole.maxRestock);

                    break;
                case GameObjectTypes.DestructibleBuilding:
                    _goValue.Building.Health = 20000; //goinfo.DestructibleBuilding.intactNumHits + goinfo.DestructibleBuilding.damagedNumHits;
                    _goValue.Building.MaxHealth = _goValue.Building.Health;
                    SetGoAnimProgress(255);
                    // yes, even after the updatefield rewrite this garbage hack is still in client
                    SetUpdateFieldValue(Values.ModifyValue(_gameObjectData).ModifyValue(_gameObjectData.ParentRotation), new Quaternion(goInfo.DestructibleBuilding.DestructibleModelRec, 0f, 0f, 0f));

                    break;
                case GameObjectTypes.Transport:
                    _goTypeImpl = new GameObjectType.Transport(this);

                    if (goInfo.Transport.startOpen != 0)
                        SetGoState(GameObjectState.TransportStopped);
                    else
                        SetGoState(GameObjectState.TransportActive);

                    SetGoAnimProgress(animProgress);
                    SetActive(true);

                    break;
                case GameObjectTypes.FishingNode:
                    SetLevel(1);
                    SetGoAnimProgress(255);

                    break;
                case GameObjectTypes.Trap:
                    if (goInfo.Trap.stealthed != 0)
                    {
                        Stealth.AddFlag(StealthType.Trap);
                        Stealth.AddValue(StealthType.Trap, 70);
                    }

                    if (goInfo.Trap.stealthAffected != 0)
                    {
                        Invisibility.AddFlag(InvisibilityType.Trap);
                        Invisibility.AddValue(InvisibilityType.Trap, 300);
                    }

                    break;
                case GameObjectTypes.PhaseableMo:
                    RemoveFlag((GameObjectFlags)0xF00);
                    SetFlag((GameObjectFlags)((_goInfo.PhaseableMO.AreaNameSet & 0xF) << 8));

                    break;
                case GameObjectTypes.CapturePoint:
                    SetUpdateFieldValue(Values.ModifyValue(_gameObjectData).ModifyValue(_gameObjectData.SpellVisualID), _goInfo.CapturePoint.SpellVisual1);
                    _goValue.CapturePoint.AssaultTimer = 0;
                    _goValue.CapturePoint.LastTeamCapture = TeamId.Neutral;
                    _goValue.CapturePoint.State = BattlegroundCapturePointState.Neutral;
                    UpdateCapturePoint();

                    break;
                default:
                    SetGoAnimProgress(animProgress);

                    break;
            }

            if (gameObjectAddon != null)
            {
                if (gameObjectAddon.InvisibilityValue != 0)
                {
                    Invisibility.AddFlag(gameObjectAddon.InvisibilityType);
                    Invisibility.AddValue(gameObjectAddon.InvisibilityType, gameObjectAddon.InvisibilityValue);
                }

                if (gameObjectAddon.WorldEffectID != 0)
                {
                    UpdateFlag.GameObject = true;
                    SetWorldEffectID(gameObjectAddon.WorldEffectID);
                }

                if (gameObjectAddon.AIAnimKitID != 0)
                    _animKitId = (ushort)gameObjectAddon.AIAnimKitID;
            }

            LastUsedScriptID = GetGoInfo().ScriptId;
            AIM_Initialize();

            if (spawnid != 0)
                _spawnId = spawnid;

            uint linkedEntry = GetGoInfo().GetLinkedGameObjectEntry();

            if (linkedEntry != 0)
            {
                GameObject linkedGo = CreateGameObject(linkedEntry, map, pos, rotation, 255, GameObjectState.Ready);

                if (linkedGo != null)
                {
                    SetLinkedTrap(linkedGo);

                    if (!map.AddToMap(linkedGo))
                        linkedGo.Dispose();
                }
            }

            // Check if GameObject is Infinite
            if (goInfo.IsInfiniteGameObject())
                SetVisibilityDistanceOverride(VisibilityDistanceType.Infinite);

            // Check if GameObject is Gigantic
            if (goInfo.IsGiganticGameObject())
                SetVisibilityDistanceOverride(VisibilityDistanceType.Gigantic);

            // Check if GameObject is Large
            if (goInfo.IsLargeGameObject())
                SetVisibilityDistanceOverride(VisibilityDistanceType.Large);

            return true;
        }

        private void DespawnForPlayer(Player seer, TimeSpan respawnTime)
        {
            PerPlayerState perPlayerState = GetOrCreatePerPlayerStates(seer.GetGUID());
            perPlayerState.ValidUntil = GameTime.GetSystemTime() + respawnTime;
            perPlayerState.Despawned = true;
            seer.UpdateVisibilityOf(this);
        }

        private GameObject LookupFishingHoleAround(float range)
        {
            var u_check = new NearestGameObjectFishingHole(this, range);
            var checker = new GameObjectSearcher(this, u_check);

            Cell.VisitGridObjects(this, checker, range);

            return checker.GetTarget();
        }

        private void SwitchDoorOrButton(bool activate, bool alternative = false)
        {
            if (activate)
                SetFlag(GameObjectFlags.InUse);
            else
                RemoveFlag(GameObjectFlags.InUse);

            if (GetGoState() == GameObjectState.Ready) //if closed . open
                SetGoState(alternative ? GameObjectState.Destroyed : GameObjectState.Active);
            else //if open . close
                SetGoState(GameObjectState.Ready);
        }

        private bool IsAtInteractDistance(Position pos, float radius)
        {
            var displayInfo = CliDB.GameObjectDisplayInfoStorage.LookupByKey(GetGoInfo().displayId);

            if (displayInfo != null)
            {
                float scale = GetObjectScale();

                float minX = displayInfo.GeoBoxMin.X * scale - radius;
                float minY = displayInfo.GeoBoxMin.Y * scale - radius;
                float minZ = displayInfo.GeoBoxMin.Z * scale - radius;
                float maxX = displayInfo.GeoBoxMax.X * scale + radius;
                float maxY = displayInfo.GeoBoxMax.Y * scale + radius;
                float maxZ = displayInfo.GeoBoxMax.Z * scale + radius;

                Quaternion worldRotation = GetWorldRotation();

                //Todo Test this. Needs checked.
                var worldSpaceBox = MathFunctions.toWorldSpace(worldRotation.ToMatrix(), new Vector3(GetPositionX(), GetPositionY(), GetPositionZ()), new Box(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ)));

                return worldSpaceBox.Contains(new Vector3(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ()));
            }

            return GetExactDist(pos) <= radius;
        }

        private void ClearLoot()
        {
            // Unlink loot objects from this GameObject before destroying to avoid accessing freed memory from Loot destructor
            loot = null;
            _personalLoot.Clear();
            _unique_users.Clear();
            _usetimes = 0;
        }

        private void SetGoStateFor(GameObjectState state, Player viewer)
        {
            PerPlayerState perPlayerState = GetOrCreatePerPlayerStates(viewer.GetGUID());
            perPlayerState.ValidUntil = GameTime.GetSystemTime() + TimeSpan.FromSeconds(_respawnDelayTime);
            perPlayerState.State = state;

            GameObjectSetStateLocal setStateLocal = new();
            setStateLocal.ObjectGUID = GetGUID();
            setStateLocal.State = (byte)state;
            viewer.SendPacket(setStateLocal);
        }

        private void EnableCollision(bool enable)
        {
            if (_model == null)
                return;

            _model.EnableCollision(enable);
        }

        private void UpdateModel()
        {
            if (!IsInWorld)
                return;

            if (_model != null)
                if (GetMap().ContainsGameObjectModel(_model))
                    GetMap().RemoveGameObjectModel(_model);

            RemoveFlag(GameObjectFlags.MapObject);
            _model = null;
            CreateModel();

            if (_model != null)
                GetMap().InsertGameObjectModel(_model);
        }

        private void UpdateCapturePoint()
        {
            if (GetGoType() != GameObjectTypes.CapturePoint)
                return;

            GameObjectAI ai = GetAI();

            if (ai != null)
                if (ai.OnCapturePointUpdated(_goValue.CapturePoint.State))
                    return;

            uint spellVisualId = 0;
            uint customAnim = 0;

            switch (_goValue.CapturePoint.State)
            {
                case BattlegroundCapturePointState.Neutral:
                    spellVisualId = GetGoInfo().CapturePoint.SpellVisual1;

                    break;
                case BattlegroundCapturePointState.ContestedHorde:
                    customAnim = 1;
                    spellVisualId = GetGoInfo().CapturePoint.SpellVisual2;

                    break;
                case BattlegroundCapturePointState.ContestedAlliance:
                    customAnim = 2;
                    spellVisualId = GetGoInfo().CapturePoint.SpellVisual3;

                    break;
                case BattlegroundCapturePointState.HordeCaptured:
                    customAnim = 3;
                    spellVisualId = GetGoInfo().CapturePoint.SpellVisual4;

                    break;
                case BattlegroundCapturePointState.AllianceCaptured:
                    customAnim = 4;
                    spellVisualId = GetGoInfo().CapturePoint.SpellVisual5;

                    break;
                default:
                    break;
            }

            if (customAnim != 0)
                SendCustomAnim(customAnim);

            SetSpellVisualId(spellVisualId);
            UpdateDynamicFlagsForNearbyPlayers();

            BattlegroundMap map = GetMap().ToBattlegroundMap();

            if (map != null)
            {
                Battleground bg = map.GetBG();

                if (bg != null)
                {
                    UpdateCapturePoint packet = new();
                    packet.CapturePointInfo.State = _goValue.CapturePoint.State;
                    packet.CapturePointInfo.Pos = GetPosition();
                    packet.CapturePointInfo.Guid = GetGUID();
                    packet.CapturePointInfo.CaptureTotalDuration = TimeSpan.FromMilliseconds(GetGoInfo().CapturePoint.CaptureTime);
                    packet.CapturePointInfo.CaptureTime = _goValue.CapturePoint.AssaultTimer;
                    bg.SendPacketToAll(packet);
                    bg.UpdateWorldState((int)GetGoInfo().CapturePoint.worldState1, (byte)_goValue.CapturePoint.State);
                }
            }

            GetMap().UpdateSpawnGroupConditions();
        }

        private PerPlayerState GetOrCreatePerPlayerStates(ObjectGuid guid)
        {
            if (_perPlayerState == null)
                _perPlayerState = new Dictionary<ObjectGuid, PerPlayerState>();

            if (!_perPlayerState.ContainsKey(guid))
                _perPlayerState[guid] = new PerPlayerState();

            return _perPlayerState[guid];
        }

        private uint GetGoArtKit()
        {
            return _gameObjectData.ArtKit;
        }

        private byte GetGoAnimProgress()
        {
            return _gameObjectData.PercentHealth;
        }

        private bool HasLootMode(LootModes lootMode)
        {
            return Convert.ToBoolean(_LootMode & lootMode);
        }

        private void SetLootMode(LootModes lootMode)
        {
            _LootMode = lootMode;
        }

        private void AddLootMode(LootModes lootMode)
        {
            _LootMode |= lootMode;
        }

        private void RemoveLootMode(LootModes lootMode)
        {
            _LootMode &= ~lootMode;
        }

        private void ResetLootMode()
        {
            _LootMode = LootModes.Default;
        }

        private void ClearSkillupList()
        {
            _SkillupList.Clear();
        }

        private uint GetUniqueUseCount()
        {
            return (uint)_unique_users.Count;
        }

        private List<ObjectGuid> GetTapList()
        {
            return _tapList;
        }

        private void SetTapList(List<ObjectGuid> tapList)
        {
            _tapList = tapList;
        }

        private bool HasLootRecipient()
        {
            return !_tapList.Empty();
        }

        private GameObjectDestructibleState GetDestructibleState()
        {
            if ((_gameObjectData.Flags & (uint)GameObjectFlags.Destroyed) != 0)
                return GameObjectDestructibleState.Destroyed;

            if ((_gameObjectData.Flags & (uint)GameObjectFlags.Damaged) != 0)
                return GameObjectDestructibleState.Damaged;

            return GameObjectDestructibleState.Intact;
        }

        private void UpdateDynamicFlagsForNearbyPlayers()
        {
            Values.ModifyValue(ObjectData).ModifyValue(ObjectData.DynamicFlags);
            AddToObjectUpdateIfNeeded();
        }

        private void HandleCustomTypeCommand(GameObjectTypeBase.CustomCommand command)
        {
            if (_goTypeImpl != null)
                command.Execute(_goTypeImpl);
        }

        // There's many places not ready for dynamic spawns. This allows them to live on for now.
        private void SetRespawnCompatibilityMode(bool mode = true)
        {
            _respawnCompatibilityMode = mode;
        }

        #region Fields

        public GameObjectFieldData _gameObjectData;
        private GameObjectTypeBase _goTypeImpl;
        protected GameObjectValue _goValue; // TODO: replace with _goTypeImpl
        protected GameObjectTemplate _goInfo;
        protected GameObjectTemplateAddon _goTemplateAddon;
        private GameObjectData _goData;
        private ulong _spawnId;
        private uint _spellId;
        private long _respawnTime;      // (secs) Time of next respawn (or despawn if GO have owner()),
        private uint _respawnDelayTime; // (secs) if 0 then current GO State no dependent from timer
        private uint _despawnDelay;
        private TimeSpan _despawnRespawnTime; // override respawn Time after delayed despawn
        private LootState _lootState;
        private ObjectGuid _lootStateUnitGUID; // GUID of the unit passed with SetLootState(LootState, Unit*)
        private bool _spawnedByDefault;
        private long _restockTime;

        private long _cooldownTime; // used as internal reaction delay Time store (not State change reaction).
        // For traps this: spell casting cooldown, for doors/buttons: reset Time.

        private Player _ritualOwner; // used for GAMEOBJECT_TYPE_SUMMONING_RITUAL where GO is not summoned (no owner)
        private readonly List<ObjectGuid> _unique_users = new();
        private uint _usetimes;

        private List<ObjectGuid> _tapList = new();
        private LootModes _LootMode; // bitmask, default LOOT_MODE_DEFAULT, determines what loot will be lootable
        private long _packedRotation;
        private Quaternion _localRotation;
        public Position StationaryPosition { get; set; }

        private GameObjectAI _AI;
        private bool _respawnCompatibilityMode;
        private ushort _animKitId;
        private uint _worldEffectID;

        private Dictionary<ObjectGuid, PerPlayerState> _perPlayerState;

        private GameObjectState _prevGoState; // What State to set whenever resetting

        private readonly Dictionary<uint, ObjectGuid> ChairListSlots = new();
        private readonly List<ObjectGuid> _SkillupList = new();

        public Loot loot;
        private Dictionary<ObjectGuid, Loot> _personalLoot = new();

        public GameObjectModel _model;

        private ObjectGuid _linkedTrap;

        #endregion
    }

}