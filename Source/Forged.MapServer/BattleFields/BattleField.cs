// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Groups;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.BattleGround;
using Forged.MapServer.Text;
using Framework.Constants;
using Game.Common;
using Serilog;

namespace Forged.MapServer.BattleFields;

public class BattleField : ZoneScript
{
    public uint BattleTime;
    public Dictionary<int, uint> Data32 = new();
    public uint DefenderTeam;

    // Graveyard variables
    public List<BfGraveyard> GraveyardList = new();

    public Dictionary<ObjectGuid, long>[] InvitedPlayers = new Dictionary<ObjectGuid, long>[2];
    public bool IsActive;
    public WorldLocation KickPosition;
    public Map Map;
    public uint MapId;

    // MapId where is Battlefield
    public uint MaxPlayer;

    public uint MinLevel;

    // Maximum number of player that participated to Battlefield
    public uint MinPlayer;

    // Minimum number of player for Battlefield start
    // Required level to participate at Battlefield
    // SectionLength of a battle
    public uint NoWarBattleTime;

    // Players info maps
    public List<ObjectGuid>[] Players = new List<ObjectGuid>[2];

    // Players in zone
    public List<ObjectGuid>[] PlayersInQueue = new List<ObjectGuid>[2];

    // Players in the queue
    public List<ObjectGuid>[] PlayersInWar = new List<ObjectGuid>[2];

    // Players in WG combat
    public Dictionary<ObjectGuid, long>[] PlayersWillBeKick = new Dictionary<ObjectGuid, long>[2];

    // Time between two battles
    public uint RestartAfterCrash;

    public ObjectGuid StalkerGuid;
    public bool StartGrouping;
    public uint StartGroupingTimer;

    // Delay to restart Wintergrasp if the server crashed during a running battle.
    public uint TimeForAcceptInvite;

    public uint Timer;

    // Variables that must exist for each battlefield
    public uint TypeId;

    public uint UiKickDontAcceptTimer;

    // See enum BattlefieldTypes
    // BattleID (for packet)
    public uint ZoneId;
    private readonly GameObjectFactory _gameObjectFactory;
    private readonly ClassFactory _classFactory;
    private readonly WorldSafeLocationsCache _worldSafeLocationsCache;

    // Timer for invite players in area 15 minute before start battle
    // bool for know if all players in area has been invited
    // Map of the objectives belonging to this OutdoorPvP
    private readonly Dictionary<uint, BfCapturePoint> _capturePoints = new();

    // Vector witch contain the different GY of the battle
    private readonly Dictionary<int, ulong> _data64 = new();

    // Position where players are teleported if they switch to afk during the battle or if they don't accept invitation
    private readonly List<ObjectGuid>[] _groups = new List<ObjectGuid>[2];

    // Contain different raid group
    private uint _lastResurectTimer;

    // ZoneID of Wintergrasp = 4197
    private uint _uiKickAfkPlayersTimer;

    public BattleField(Map map, ObjectAccessor objectAccessor, GameObjectManager objectManager, BattleFieldManager battleFieldManager, CreatureTextManager creatureTextManager,
                       CreatureFactory creatureFactory, GroupManager groupManager, GameObjectFactory gameObjectFactory, ClassFactory classFactory, WorldSafeLocationsCache worldSafeLocationsCache)
    {
        CreatureFactory = creatureFactory;
        GroupManager = groupManager;
        IsEnabled = true;
        DefenderTeam = TeamIds.Neutral;

        TimeForAcceptInvite = 20;
        UiKickDontAcceptTimer = 1000;
        _uiKickAfkPlayersTimer = 1000;

        _lastResurectTimer = 30 * Time.IN_MILLISECONDS;

        Map = map;
        _gameObjectFactory = gameObjectFactory;
        _classFactory = classFactory;
        _worldSafeLocationsCache = worldSafeLocationsCache;
        ObjectAccessor = objectAccessor;
        ObjectManager = objectManager;
        BattlefieldManager = battleFieldManager;
        CreatureTextManager = creatureTextManager;
        MapId = map.Id;

        for (byte i = 0; i < 2; ++i)
        {
            Players[i] = new List<ObjectGuid>();
            PlayersInQueue[i] = new List<ObjectGuid>();
            PlayersInWar[i] = new List<ObjectGuid>();
            InvitedPlayers[i] = new Dictionary<ObjectGuid, long>();
            PlayersWillBeKick[i] = new Dictionary<ObjectGuid, long>();
            _groups[i] = new List<ObjectGuid>();
        }
    }

    public uint AttackerTeam => 1 - DefenderTeam;
    public BattleFieldManager BattlefieldManager { get; }
    public uint BattleId { get; set; }
    public bool CanFlyIn => !IsActive;
    public CreatureFactory CreatureFactory { get; }
    public CreatureTextManager CreatureTextManager { get; }
    public GroupManager GroupManager { get; }
    public bool IsEnabled { get; set; }
    public ObjectAccessor ObjectAccessor { get; }

    public GameObjectManager ObjectManager { get; }
    // Global timer for event

    // Timer for check Afk in war
    // Timer for resurect player every 30 sec
    public void AddCapturePoint(BfCapturePoint cp)
    {
        if (_capturePoints.ContainsKey(cp.GetCapturePointEntry()))
            Log.Logger.Error("Battlefield.AddCapturePoint: CapturePoint {0} already exists!", cp.GetCapturePointEntry());

        _capturePoints[cp.GetCapturePointEntry()] = cp;
    }

    public virtual void AddPlayerToResurrectQueue(ObjectGuid npcGuid, ObjectGuid playerGuid)
    {
        for (byte i = 0; i < GraveyardList.Count; i++)
        {
            if (GraveyardList[i] == null)
                continue;

            if (!GraveyardList[i].HasNpc(npcGuid))
                continue;

            GraveyardList[i].AddPlayer(playerGuid);

            break;
        }
    }

    // Called in WorldSession:HandleBfExitRequest
    public void AskToLeaveQueue(Player player)
    {
        // Remove player from queue
        PlayersInQueue[player.TeamId].Remove(player.GUID);
    }

    public void BroadcastPacketToQueue(ServerPacket data)
    {
        for (byte team = 0; team < 2; ++team)
            foreach (var guid in PlayersInQueue[team])
            {
                ObjectAccessor.FindPlayer(guid)?.SendPacket(data);
            }
    }

    public void BroadcastPacketToWar(ServerPacket data)
    {
        for (byte team = 0; team < 2; ++team)
            foreach (var guid in PlayersInWar[team])
            {
                ObjectAccessor.FindPlayer(guid)?.SendPacket(data);
            }
    }

    public void BroadcastPacketToZone(ServerPacket data)
    {
        for (byte team = 0; team < 2; ++team)
            foreach (var guid in Players[team])
            {
                ObjectAccessor.FindPlayer(guid)?.SendPacket(data);
            }
    }

    // Return if we can use mount in battlefield
    public virtual void DoCompleteOrIncrementAchievement(uint achievement, Player player, byte incrementNumber = 1) { }

    public void EndBattle(bool endByTimer)
    {
        if (!IsActive)
            return;

        IsActive = false;

        StartGrouping = false;

        if (!endByTimer)
            SetDefenderTeam(AttackerTeam);

        // Reset battlefield timer
        Timer = NoWarBattleTime;

        OnBattleEnd(endByTimer);
    }

    public WorldSafeLocsEntry GetClosestGraveYard(Player player)
    {
        BfGraveyard closestGY = null;
        float maxdist = -1;

        for (byte i = 0; i < GraveyardList.Count; i++)
            if (GraveyardList[i] != null)
            {
                if (GraveyardList[i].ControlTeamId != player.TeamId)
                    continue;

                var dist = GraveyardList[i].GetDistance(player);

                if (dist < maxdist || maxdist < 0)
                {
                    closestGY = GraveyardList[i];
                    maxdist = dist;
                }
            }

        if (closestGY != null)
            return _worldSafeLocationsCache.GetWorldSafeLoc(closestGY.GraveyardId);

        return null;
    }

    public Creature GetCreature(ObjectGuid guid)
    {
        return Map?.GetCreature(guid);
    }

    // All-purpose data storage 32 bit
    public virtual uint GetData(int dataId)
    {
        return Data32[dataId];
    }

    // All-purpose data storage 64 bit
    public virtual ulong GetData64(int dataId)
    {
        return _data64[dataId];
    }

    public GameObject GetGameObject(ObjectGuid guid)
    {
        return Map?.GetGameObject(guid);
    }

    public BfGraveyard GetGraveyardById(int id)
    {
        if (id < GraveyardList.Count)
        {
            var graveyard = GraveyardList[id];

            if (graveyard != null)
                return graveyard;

            Log.Logger.Error("Battlefield:GetGraveyardById Id: {0} not existed", id);
        }
        else
        {
            Log.Logger.Error("Battlefield:GetGraveyardById Id: {0} cant be found", id);
        }

        return null;
    }

    public int GetOtherTeam(int teamIndex)
    {
        return teamIndex == TeamIds.Horde ? TeamIds.Alliance : TeamIds.Horde;
    }

    // Called when a Unit is kill in battlefield zone
    public virtual void HandleKill(Player killer, Unit killed) { }

    public void HandlePlayerEnterZone(Player player, uint zone)
    {
        // If battle is started,
        // If not full of players > invite player to join the war
        // If full of players > announce to player that BF is full and kick him after a few second if he desn't leave
        if (IsActive)
        {
            if (PlayersInWar[player.TeamId].Count + InvitedPlayers[player.TeamId].Count < MaxPlayer) // Vacant spaces
            {
                InvitePlayerToWar(player);
            }
            else // No more vacant places
            {
                // todo Send a packet to announce it to player
                PlayersWillBeKick[player.TeamId][player.GUID] = GameTime.CurrentTime + 10;
                InvitePlayerToQueue(player);
            }
        }
        else
        {
            // If time left is < 15 minutes invite player to join queue
            if (Timer <= StartGroupingTimer)
                InvitePlayerToQueue(player);
        }

        // Add player in the list of player in zone
        Players[player.TeamId].Add(player.GUID);
        OnPlayerEnterZone(player);
    }

    // Called when a player leave the zone
    public void HandlePlayerLeaveZone(Player player, uint zone)
    {
        if (IsActive)
            // If the player is participating to the battle
            if (PlayersInWar[player.TeamId].Contains(player.GUID))
            {
                PlayersInWar[player.TeamId].Remove(player.GUID);
                // Remove the player from the raid group
                player.Group?.RemoveMember(player.GUID);

                OnPlayerLeaveWar(player);
            }

        foreach (var capturePoint in _capturePoints.Values)
            capturePoint.HandlePlayerLeave(player);

        InvitedPlayers[player.TeamId].Remove(player.GUID);
        PlayersWillBeKick[player.TeamId].Remove(player.GUID);
        Players[player.TeamId].Remove(player.GUID);
        SendRemoveWorldStates(player);
        RemovePlayerFromResurrectQueue(player.GUID);
        OnPlayerLeaveZone(player);
    }

    public bool HasPlayer(Player player)
    {
        return Players[player.TeamId].Contains(player.GUID);
    }

    public void HideNpc(Creature creature)
    {
        creature.CombatStop();
        creature.ReactState = ReactStates.Passive;
        creature.SetUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible);
        creature.DisappearAndDie();
        creature.SetVisible(false);
    }

    public void InitStalker(uint entry, Position pos)
    {
        var creature = SpawnCreature(entry, pos);

        if (creature != null)
            StalkerGuid = creature.GUID;
        else
            Log.Logger.Error("Battlefield.InitStalker: could not spawn Stalker (Creature entry {0}), zone messeges will be un-available", entry);
    }

    public void KickPlayerFromBattlefield(ObjectGuid guid)
    {
        var player = ObjectAccessor.FindPlayer(guid);

        if (player == null)
            return;

        if (player.Location.Zone == ZoneId)
            player.TeleportTo(KickPosition);
    }

    // Called at the end of battle
    public virtual void OnBattleEnd(bool endByTimer) { }

    // Called on start
    public virtual void OnBattleStart() { }

    // Called when a player enter in battlefield zone
    public virtual void OnPlayerEnterZone(Player player) { }

    // Called when a player accept to join the battle
    public virtual void OnPlayerJoinWar(Player player) { }

    // Called when a player leave the battle
    public virtual void OnPlayerLeaveWar(Player player) { }

    // Called when a player leave battlefield zone
    public virtual void OnPlayerLeaveZone(Player player) { }

    // Called x minutes before battle start when player in zone are invite to join queue
    public virtual void OnStartGrouping() { }

    // Called in WorldSession:HandleBfQueueInviteResponse
    public void PlayerAcceptInviteToQueue(Player player)
    {
        // Add player in queue
        PlayersInQueue[player.TeamId].Add(player.GUID);
    }

    // Called in WorldSession:HandleBfEntryInviteResponse
    public void PlayerAcceptInviteToWar(Player player)
    {
        if (!IsActive)
            return;

        if (!AddOrSetPlayerToCorrectBfGroup(player))
            return;

        PlayersInWar[player.TeamId].Add(player.GUID);
        InvitedPlayers[player.TeamId].Remove(player.GUID);

        if (player.IsAfk)
            player.ToggleAfk();

        OnPlayerJoinWar(player); //for scripting
    }

    // Called in WorldSession::HandleHearthAndResurrect
    public void PlayerAskToLeave(Player player)
    {
        // Player leaving Wintergrasp, teleport to Dalaran.
        // ToDo: confirm teleport destination.
        player.TeleportTo(571, 5804.1499f, 624.7710f, 647.7670f, 1.6400f);
    }

    public void RegisterZone(uint zoneId)
    {
        BattlefieldManager.AddZone(zoneId, this);
    }

    //***************End of Group System*******************
    public void RemovePlayerFromResurrectQueue(ObjectGuid playerGuid)
    {
        for (byte i = 0; i < GraveyardList.Count; i++)
        {
            if (GraveyardList[i] == null)
                continue;

            if (!GraveyardList[i].HasPlayer(playerGuid))
                continue;

            GraveyardList[i].RemovePlayer(playerGuid);

            break;
        }
    }

    public void SendAreaSpiritHealerQuery(Player player, ObjectGuid guid)
    {
        AreaSpiritHealerTime areaSpiritHealerTime = new()
        {
            HealerGuid = guid,
            TimeLeft = _lastResurectTimer // resurrect every 30 seconds
        };

        player.SendPacket(areaSpiritHealerTime);
    }

    // use for switch off all worldstate for client
    public virtual void SendRemoveWorldStates(Player player) { }

    public void SendWarning(uint id, WorldObject target = null)
    {
        var stalker = GetCreature(StalkerGuid);

        if (stalker != null)
            CreatureTextManager.SendChat(stalker, (byte)id, target);
    }

    // Call this to init the Battlefield
    public virtual bool SetupBattlefield()
    {
        return true;
    }

    public void ShowNpc(Creature creature, bool aggressive)
    {
        creature.SetVisible(true);
        creature.RemoveUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible);

        if (!creature.IsAlive)
            creature.Respawn(true);

        if (aggressive)
        {
            creature.ReactState = ReactStates.Aggressive;
        }
        else
        {
            creature.SetUnitFlag(UnitFlags.NonAttackable);
            creature.ReactState = ReactStates.Passive;
        }
    }

    public Creature SpawnCreature(uint entry, Position pos)
    {
        if (ObjectManager.CreatureTemplateCache.GetCreatureTemplate(entry) == null)
        {
            Log.Logger.Error("Battlefield:SpawnCreature: entry {0} does not exist.", entry);

            return null;
        }

        var creature = CreatureFactory.CreateCreature(entry, Map, pos);

        if (creature == null)
        {
            Log.Logger.Error("Battlefield:SpawnCreature: Can't create creature entry: {0}", entry);

            return null;
        }

        creature.HomePosition = pos;

        // Set creature in world
        Map.AddToMap(creature);
        creature.SetActive(true);
        creature.Visibility.SetFarVisible(true);

        return creature;
    }

    // Method for spawning gameobject on map
    public GameObject SpawnGameObject(uint entry, Position pos, Quaternion rotation)
    {
        if (ObjectManager.GameObjectTemplateCache.GetGameObjectTemplate(entry) == null)
        {
            Log.Logger.Error("Battlefield.SpawnGameObject: GameObject template {0} not found in database! Battlefield not created!", entry);

            return null;
        }

        // Create gameobject
        var go = _gameObjectFactory.CreateGameObject(entry, Map, pos, rotation, 255, GameObjectState.Ready);

        if (go == null)
        {
            Log.Logger.Error("Battlefield:SpawnGameObject: Cannot create gameobject template {0}! Battlefield not created!", entry);

            return null;
        }

        // Add to world
        Map.AddToMap(go);
        go.SetActive(true);
        go.Visibility.SetFarVisible(true);

        return go;
    }

    public void StartBattle()
    {
        if (IsActive)
            return;

        for (var team = 0; team < 2; team++)
        {
            PlayersInWar[team].Clear();
            _groups[team].Clear();
        }

        Timer = BattleTime;
        IsActive = true;

        InvitePlayersInZoneToWar();
        InvitePlayersInQueueToWar();

        OnBattleStart();
    }

    public void TeamCastSpell(uint teamIndex, int spellId)
    {
        foreach (var player in PlayersInWar[teamIndex].Select(guid => ObjectAccessor.FindPlayer(guid)).Where(player => player != null))
        {
            if (spellId > 0)
                player.SpellFactory.CastSpell(player, (uint)spellId, true);
            else
                player.RemoveAuraFromStack((uint)-spellId);
        }
    }

    // Enable or Disable battlefield
    public void ToggleBattlefield(bool enable)
    {
        IsEnabled = enable;
    }

    public virtual bool Update(uint diff)
    {
        if (Timer <= diff)
        {
            // Battlefield ends on time
            if (IsActive)
                EndBattle(true);
            else // Time to start a new battle!
                StartBattle();
        }
        else
        {
            Timer -= diff;
        }

        // Invite players a few minutes before the battle's beginning
        if (!IsActive && !StartGrouping && Timer <= StartGroupingTimer)
        {
            StartGrouping = true;
            InvitePlayersInZoneToQueue();
            OnStartGrouping();
        }

        var objectiveChanged = false;

        if (IsActive)
        {
            if (_uiKickAfkPlayersTimer <= diff)
            {
                _uiKickAfkPlayersTimer = 1000;
                KickAfkPlayers();
            }
            else
            {
                _uiKickAfkPlayersTimer -= diff;
            }

            // Kick players who chose not to accept invitation to the battle
            if (UiKickDontAcceptTimer <= diff)
            {
                var now = GameTime.CurrentTime;

                for (var team = 0; team < SharedConst.PvpTeamsCount; team++)
                    foreach (var pair in InvitedPlayers[team].Where(pair => pair.Value <= now))
                        KickPlayerFromBattlefield(pair.Key);

                InvitePlayersInZoneToWar();

                for (var team = 0; team < SharedConst.PvpTeamsCount; team++)
                    foreach (var pair in PlayersWillBeKick[team].Where(pair => pair.Value <= now))
                        KickPlayerFromBattlefield(pair.Key);

                UiKickDontAcceptTimer = 1000;
            }
            else
            {
                UiKickDontAcceptTimer -= diff;
            }

            foreach (var pair in _capturePoints.Where(pair => pair.Value.Update(diff)))
                objectiveChanged = true;
        }

        if (_lastResurectTimer <= diff)
        {
            for (byte i = 0; i < GraveyardList.Count; i++)
                if (GetGraveyardById(i) != null)
                    GraveyardList[i].Resurrect();

            _lastResurectTimer = BattlegroundConst.RESURRECTION_INTERVAL;
        }
        else
        {
            _lastResurectTimer -= diff;
        }

        return objectiveChanged;
    }

    public virtual void UpdateData(int index, int pad)
    {
        if (pad < 0)
            Data32[index] -= (uint)-pad;
        else
            Data32[index] += (uint)pad;
    }

    private bool AddOrSetPlayerToCorrectBfGroup(Player player)
    {
        if (!player.Location.IsInWorld)
            return false;

        player.Group?.RemoveMember(player.GUID);

        var group = GetFreeBfRaid(player.TeamId);

        if (group == null)
        {
            group = _classFactory.Resolve<PlayerGroup>();
            group.SetBattlefieldGroup(this);
            group.Create(player);
            GroupManager.AddGroup(group);
            _groups[player.TeamId].Add(group.GUID);
        }
        else if (group.IsMember(player.GUID))
        {
            var subgroup = group.GetMemberGroup(player.GUID);
            player.SetBattlegroundOrBattlefieldRaid(group, subgroup);
        }
        else
        {
            group.AddMember(player);
        }

        return true;
    }

    // ****************************************************
    // ******************* Group System *******************
    // ****************************************************
    private PlayerGroup GetFreeBfRaid(int teamIndex)
    {
        foreach (var guid in _groups[teamIndex])
        {
            var group = GroupManager.GetGroupByGuid(guid);

            if (group is { IsFull: false })
                return group;
        }

        return null;
    }

    private void InvitePlayersInQueueToWar()
    {
        for (byte team = 0; team < 2; ++team)
        {
            foreach (var player in PlayersInQueue[team].Select(guid => ObjectAccessor.FindPlayer(guid)).Where(player => player != null))
            {
                if (PlayersInWar[player.TeamId].Count + InvitedPlayers[player.TeamId].Count < MaxPlayer)
                {
                    InvitePlayerToWar(player);
                }
                //Full
            }

            PlayersInQueue[team].Clear();
        }
    }

    private void InvitePlayersInZoneToQueue()
    {
        for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
            foreach (var player in Players[team].Select(guid => ObjectAccessor.FindPlayer(guid)).Where(player => player != null))
            {
                InvitePlayerToQueue(player);
            }
    }

    private void InvitePlayersInZoneToWar()
    {
        for (byte team = 0; team < 2; ++team)
            foreach (var guid in Players[team])
            {
                var player = ObjectAccessor.FindPlayer(guid);

                if (player == null)
                    continue;

                if (PlayersInWar[player.TeamId].Contains(player.GUID) || InvitedPlayers[player.TeamId].ContainsKey(player.GUID))
                    continue;

                if (PlayersInWar[player.TeamId].Count + InvitedPlayers[player.TeamId].Count < MaxPlayer)
                    InvitePlayerToWar(player);
                else // Battlefield is full of players
                    PlayersWillBeKick[player.TeamId][player.GUID] = GameTime.CurrentTime + 10;
            }
    }

    private void InvitePlayerToQueue(Player player)
    {
        if (PlayersInQueue[player.TeamId].Contains(player.GUID))
            return;

        if (PlayersInQueue[player.TeamId].Count <= MinPlayer || PlayersInQueue[GetOtherTeam(player.TeamId)].Count >= MinPlayer)
            PlayerAcceptInviteToQueue(player);
    }

    private void InvitePlayerToWar(Player player)
    {
        if (player == null)
            return;

        // todo needed ?
        if (player.IsInFlight)
            return;

        if (player.InArena || player.Battleground != null)
        {
            PlayersInQueue[player.TeamId].Remove(player.GUID);

            return;
        }

        // If the player does not match minimal level requirements for the battlefield, kick him
        if (player.Level < MinLevel)
        {
            if (!PlayersWillBeKick[player.TeamId].ContainsKey(player.GUID))
                PlayersWillBeKick[player.TeamId][player.GUID] = GameTime.CurrentTime + 10;

            return;
        }

        // Check if player is not already in war
        if (PlayersInWar[player.TeamId].Contains(player.GUID) || InvitedPlayers[player.TeamId].ContainsKey(player.GUID))
            return;

        PlayersWillBeKick[player.TeamId].Remove(player.GUID);
        InvitedPlayers[player.TeamId][player.GUID] = GameTime.CurrentTime + TimeForAcceptInvite;
        PlayerAcceptInviteToWar(player);
    }

    private void KickAfkPlayers()
    {
        for (byte team = 0; team < 2; ++team)
            foreach (var guid in PlayersInWar[team])
            {
                var player = ObjectAccessor.FindPlayer(guid);

                if (player is { IsAfk: true })
                    KickPlayerFromBattlefield(guid);
            }
    }

    private void SetDefenderTeam(uint team)
    {
        DefenderTeam = team;
    }
}