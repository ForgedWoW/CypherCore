// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.BattleGround;
using Forged.MapServer.Networking.Packets.Misc;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.BattleFields;

public class BattleField : ZoneScript
{
    public ObjectGuid StalkerGuid;
    protected WorldLocation KickPosition;
    protected uint m_BattleId;
    protected uint m_BattleTime;
    protected Dictionary<int, uint> m_Data32 = new();

    protected uint m_DefenderTeam;

    // Graveyard variables
    protected List<BfGraveyard> m_GraveyardList = new();

    protected Dictionary<ObjectGuid, long>[] m_InvitedPlayers = new Dictionary<ObjectGuid, long>[2];
    protected bool m_isActive;
    protected bool m_IsEnabled;
    protected Map m_Map;

    protected uint m_MapId;

    // MapId where is Battlefield
    protected uint m_MaxPlayer;

    protected uint m_MinLevel;

    // Maximum number of player that participated to Battlefield
    protected uint m_MinPlayer;

    // Minimum number of player for Battlefield start
    // Required level to participate at Battlefield
    // Length of a battle
    protected uint m_NoWarBattleTime;

    // Players info maps
    protected List<ObjectGuid>[] m_players = new List<ObjectGuid>[2];

    // Players in zone
    protected List<ObjectGuid>[] m_PlayersInQueue = new List<ObjectGuid>[2];

    // Players in the queue
    protected List<ObjectGuid>[] m_PlayersInWar = new List<ObjectGuid>[2];

    // Players in WG combat
    protected Dictionary<ObjectGuid, long>[] m_PlayersWillBeKick = new Dictionary<ObjectGuid, long>[2];

    // Time between two battles
    protected uint m_RestartAfterCrash;

    protected bool m_StartGrouping;

    protected uint m_StartGroupingTimer;

    // Delay to restart Wintergrasp if the server crashed during a running battle.
    protected uint m_TimeForAcceptInvite;

    protected uint m_Timer; // Global timer for event

    // Variables that must exist for each battlefield
    protected uint m_TypeId; // See enum BattlefieldTypes

    protected uint m_uiKickDontAcceptTimer;

    // BattleID (for packet)
    protected uint m_ZoneId; // ZoneID of Wintergrasp = 4197

    // Position where players are teleported if they switch to afk during the battle or if they don't accept invitation

    // Vector witch contain the different GY of the battle

    // Timer for invite players in area 15 minute before start battle
    // bool for know if all players in area has been invited
    // Map of the objectives belonging to this OutdoorPvP
    private readonly Dictionary<uint, BfCapturePoint> m_capturePoints = new();

    private readonly Dictionary<int, ulong> m_Data64 = new();
    private readonly List<ObjectGuid>[] m_Groups = new List<ObjectGuid>[2]; // Contain different raid group
    private uint m_LastResurectTimer;

    private uint m_uiKickAfkPlayersTimer; // Timer for check Afk in war
    // Timer for resurect player every 30 sec

    public BattleField(Map map)
    {
        m_IsEnabled = true;
        m_DefenderTeam = TeamIds.Neutral;

        m_TimeForAcceptInvite = 20;
        m_uiKickDontAcceptTimer = 1000;
        m_uiKickAfkPlayersTimer = 1000;

        m_LastResurectTimer = 30 * Time.IN_MILLISECONDS;

        m_Map = map;
        m_MapId = map.Id;

        for (byte i = 0; i < 2; ++i)
        {
            m_players[i] = new List<ObjectGuid>();
            m_PlayersInQueue[i] = new List<ObjectGuid>();
            m_PlayersInWar[i] = new List<ObjectGuid>();
            m_InvitedPlayers[i] = new Dictionary<ObjectGuid, long>();
            m_PlayersWillBeKick[i] = new Dictionary<ObjectGuid, long>();
            m_Groups[i] = new List<ObjectGuid>();
        }
    }

    public void AddCapturePoint(BfCapturePoint cp)
    {
        if (m_capturePoints.ContainsKey(cp.GetCapturePointEntry()))
            Log.Logger.Error("Battlefield.AddCapturePoint: CapturePoint {0} already exists!", cp.GetCapturePointEntry());

        m_capturePoints[cp.GetCapturePointEntry()] = cp;
    }

    public virtual void AddPlayerToResurrectQueue(ObjectGuid npcGuid, ObjectGuid playerGuid)
    {
        for (byte i = 0; i < m_GraveyardList.Count; i++)
        {
            if (m_GraveyardList[i] == null)
                continue;

            if (m_GraveyardList[i].HasNpc(npcGuid))
            {
                m_GraveyardList[i].AddPlayer(playerGuid);

                break;
            }
        }
    }

    // Called in WorldSession:HandleBfExitRequest
    public void AskToLeaveQueue(Player player)
    {
        // Remove player from queue
        m_PlayersInQueue[player.TeamId].Remove(player.GUID);
    }

    public void BroadcastPacketToQueue(ServerPacket data)
    {
        for (byte team = 0; team < 2; ++team)
            foreach (var guid in m_PlayersInQueue[team])
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                    player.SendPacket(data);
            }
    }

    public void BroadcastPacketToWar(ServerPacket data)
    {
        for (byte team = 0; team < 2; ++team)
            foreach (var guid in m_PlayersInWar[team])
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                    player.SendPacket(data);
            }
    }

    public void BroadcastPacketToZone(ServerPacket data)
    {
        for (byte team = 0; team < 2; ++team)
            foreach (var guid in m_players[team])
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                    player.SendPacket(data);
            }
    }

    // Return if we can use mount in battlefield
    public bool CanFlyIn()
    {
        return !m_isActive;
    }

    public virtual void DoCompleteOrIncrementAchievement(uint achievement, Player player, byte incrementNumber = 1)
    { }

    public void EndBattle(bool endByTimer)
    {
        if (!m_isActive)
            return;

        m_isActive = false;

        m_StartGrouping = false;

        if (!endByTimer)
            SetDefenderTeam(GetAttackerTeam());

        // Reset battlefield timer
        m_Timer = m_NoWarBattleTime;

        OnBattleEnd(endByTimer);
    }

    public uint GetAttackerTeam()
    {
        return 1 - m_DefenderTeam;
    }

    public uint GetBattleId()
    {
        return m_BattleId;
    }

    public WorldSafeLocsEntry GetClosestGraveYard(Player player)
    {
        BfGraveyard closestGY = null;
        float maxdist = -1;

        for (byte i = 0; i < m_GraveyardList.Count; i++)
            if (m_GraveyardList[i] != null)
            {
                if (m_GraveyardList[i].GetControlTeamId() != player.TeamId)
                    continue;

                var dist = m_GraveyardList[i].GetDistance(player);

                if (dist < maxdist || maxdist < 0)
                {
                    closestGY = m_GraveyardList[i];
                    maxdist = dist;
                }
            }

        if (closestGY != null)
            return Global.ObjectMgr.GetWorldSafeLoc(closestGY.GetGraveyardId());

        return null;
    }

    public Creature GetCreature(ObjectGuid guid)
    {
        if (!m_Map)
            return null;

        return m_Map.GetCreature(guid);
    }

    // All-purpose data storage 32 bit
    public virtual uint GetData(int dataId)
    {
        return m_Data32[dataId];
    }

    // All-purpose data storage 64 bit
    public virtual ulong GetData64(int dataId)
    {
        return m_Data64[dataId];
    }

    // Battlefield - generic methods
    public uint GetDefenderTeam()
    {
        return m_DefenderTeam;
    }

    public GameObject GetGameObject(ObjectGuid guid)
    {
        if (!m_Map)
            return null;

        return m_Map.GetGameObject(guid);
    }

    public BfGraveyard GetGraveyardById(int id)
    {
        if (id < m_GraveyardList.Count)
        {
            var graveyard = m_GraveyardList[id];

            if (graveyard != null)
                return graveyard;
            else
                Log.Logger.Error("Battlefield:GetGraveyardById Id: {0} not existed", id);
        }
        else
        {
            Log.Logger.Error("Battlefield:GetGraveyardById Id: {0} cant be found", id);
        }

        return null;
    }

    public Map GetMap()
    {
        return m_Map;
    }

    public uint GetMapId()
    {
        return m_MapId;
    }

    public int GetOtherTeam(int teamIndex)
    {
        return (teamIndex == TeamIds.Horde ? TeamIds.Alliance : TeamIds.Horde);
    }

    public ulong GetQueueId()
    {
        return MathFunctions.MakePair64(m_BattleId | 0x20000, 0x1F100000);
    }

    public uint GetTimer()
    {
        return m_Timer;
    }

    public uint GetTypeId()
    {
        return m_TypeId;
    }

    public uint GetZoneId()
    {
        return m_ZoneId;
    }

    // Called when a Unit is kill in battlefield zone
    public virtual void HandleKill(Player killer, Unit killed)
    { }

    public void HandlePlayerEnterZone(Player player, uint zone)
    {
        // If battle is started,
        // If not full of players > invite player to join the war
        // If full of players > announce to player that BF is full and kick him after a few second if he desn't leave
        if (IsWarTime())
        {
            if (m_PlayersInWar[player.TeamId].Count + m_InvitedPlayers[player.TeamId].Count < m_MaxPlayer) // Vacant spaces
            {
                InvitePlayerToWar(player);
            }
            else // No more vacant places
            {
                // todo Send a packet to announce it to player
                m_PlayersWillBeKick[player.TeamId][player.GUID] = GameTime.CurrentTime + 10;
                InvitePlayerToQueue(player);
            }
        }
        else
        {
            // If time left is < 15 minutes invite player to join queue
            if (m_Timer <= m_StartGroupingTimer)
                InvitePlayerToQueue(player);
        }

        // Add player in the list of player in zone
        m_players[player.TeamId].Add(player.GUID);
        OnPlayerEnterZone(player);
    }

    // Called when a player leave the zone
    public void HandlePlayerLeaveZone(Player player, uint zone)
    {
        if (IsWarTime())
            // If the player is participating to the battle
            if (m_PlayersInWar[player.TeamId].Contains(player.GUID))
            {
                m_PlayersInWar[player.TeamId].Remove(player.GUID);
                var group = player.Group;

                if (group) // Remove the player from the raid group
                    group.RemoveMember(player.GUID);

                OnPlayerLeaveWar(player);
            }

        foreach (var capturePoint in m_capturePoints.Values)
            capturePoint.HandlePlayerLeave(player);

        m_InvitedPlayers[player.TeamId].Remove(player.GUID);
        m_PlayersWillBeKick[player.TeamId].Remove(player.GUID);
        m_players[player.TeamId].Remove(player.GUID);
        SendRemoveWorldStates(player);
        RemovePlayerFromResurrectQueue(player.GUID);
        OnPlayerLeaveZone(player);
    }

    public bool HasPlayer(Player player)
    {
        return m_players[player.TeamId].Contains(player.GUID);
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

        if (creature)
            StalkerGuid = creature.GUID;
        else
            Log.Logger.Error("Battlefield.InitStalker: could not spawn Stalker (Creature entry {0}), zone messeges will be un-available", entry);
    }

    // Return if battlefield is enable
    public bool IsEnabled()
    {
        return m_IsEnabled;
    }

    // Return true if battle is start, false if battle is not started
    public bool IsWarTime()
    {
        return m_isActive;
    }

    public void KickPlayerFromBattlefield(ObjectGuid guid)
    {
        var player = Global.ObjAccessor.FindPlayer(guid);

        if (player)
            if (player.Zone == GetZoneId())
                player.TeleportTo(KickPosition);
    }

    // Called at the end of battle
    public virtual void OnBattleEnd(bool endByTimer)
    { }

    // Called on start
    public virtual void OnBattleStart()
    { }

    // Called when a player enter in battlefield zone
    public virtual void OnPlayerEnterZone(Player player)
    { }

    // Called when a player accept to join the battle
    public virtual void OnPlayerJoinWar(Player player)
    { }

    // Called when a player leave the battle
    public virtual void OnPlayerLeaveWar(Player player)
    { }

    // Called when a player leave battlefield zone
    public virtual void OnPlayerLeaveZone(Player player)
    { }

    // Called x minutes before battle start when player in zone are invite to join queue
    public virtual void OnStartGrouping()
    { }

    // Called in WorldSession:HandleBfQueueInviteResponse
    public void PlayerAcceptInviteToQueue(Player player)
    {
        // Add player in queue
        m_PlayersInQueue[player.TeamId].Add(player.GUID);
    }

    // Called in WorldSession:HandleBfEntryInviteResponse
    public void PlayerAcceptInviteToWar(Player player)
    {
        if (!IsWarTime())
            return;

        if (AddOrSetPlayerToCorrectBfGroup(player))
        {
            m_PlayersInWar[player.TeamId].Add(player.GUID);
            m_InvitedPlayers[player.TeamId].Remove(player.GUID);

            if (player.IsAfk)
                player.ToggleAfk();

            OnPlayerJoinWar(player); //for scripting
        }
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
        Global.BattleFieldMgr.AddZone(zoneId, this);
    }

    //***************End of Group System*******************
    public void RemovePlayerFromResurrectQueue(ObjectGuid playerGuid)
    {
        for (byte i = 0; i < m_GraveyardList.Count; i++)
        {
            if (m_GraveyardList[i] == null)
                continue;

            if (m_GraveyardList[i].HasPlayer(playerGuid))
            {
                m_GraveyardList[i].RemovePlayer(playerGuid);

                break;
            }
        }
    }

    public void SendAreaSpiritHealerQuery(Player player, ObjectGuid guid)
    {
        AreaSpiritHealerTime areaSpiritHealerTime = new()
        {
            HealerGuid = guid,
            TimeLeft = m_LastResurectTimer // resurrect every 30 seconds
        };

        player.SendPacket(areaSpiritHealerTime);
    }

    // use for switch off all worldstate for client
    public virtual void SendRemoveWorldStates(Player player)
    { }

    public void SendWarning(uint id, WorldObject target = null)
    {
        var stalker = GetCreature(StalkerGuid);

        if (stalker)
            Global.CreatureTextMgr.SendChat(stalker, (byte)id, target);
    }

    public virtual void SetData(int dataId, uint value)
    {
        m_Data32[dataId] = value;
    }

    public virtual void SetData64(int dataId, ulong value)
    {
        m_Data64[dataId] = value;
    }

    public void SetTimer(uint timer)
    {
        m_Timer = timer;
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
        if (Global.ObjectMgr.GetCreatureTemplate(entry) == null)
        {
            Log.Logger.Error("Battlefield:SpawnCreature: entry {0} does not exist.", entry);

            return null;
        }

        var creature = CreatureFactory.CreateCreature(entry, m_Map, pos);

        if (!creature)
        {
            Log.Logger.Error("Battlefield:SpawnCreature: Can't create creature entry: {0}", entry);

            return null;
        }

        creature.HomePosition = pos;

        // Set creature in world
        m_Map.AddToMap(creature);
        creature.SetActive(true);
        creature.Visibility.SetFarVisible(true);

        return creature;
    }

    // Method for spawning gameobject on map
    public GameObject SpawnGameObject(uint entry, Position pos, Quaternion rotation)
    {
        if (Global.ObjectMgr.GetGameObjectTemplate(entry) == null)
        {
            Log.Logger.Error("Battlefield.SpawnGameObject: GameObject template {0} not found in database! Battlefield not created!", entry);

            return null;
        }

        // Create gameobject
        var go = GameObject.CreateGameObject(entry, m_Map, pos, rotation, 255, GameObjectState.Ready);

        if (!go)
        {
            Log.Logger.Error("Battlefield:SpawnGameObject: Cannot create gameobject template {1}! Battlefield not created!", entry);

            return null;
        }

        // Add to world
        m_Map.AddToMap(go);
        go.SetActive(true);
        go.Visibility.SetFarVisible(true);

        return go;
    }

    public void StartBattle()
    {
        if (m_isActive)
            return;

        for (var team = 0; team < 2; team++)
        {
            m_PlayersInWar[team].Clear();
            m_Groups[team].Clear();
        }

        m_Timer = m_BattleTime;
        m_isActive = true;

        InvitePlayersInZoneToWar();
        InvitePlayersInQueueToWar();

        OnBattleStart();
    }

    public void TeamCastSpell(uint teamIndex, int spellId)
    {
        foreach (var guid in m_PlayersInWar[teamIndex])
        {
            var player = Global.ObjAccessor.FindPlayer(guid);

            if (player)
            {
                if (spellId > 0)
                    player.CastSpell(player, (uint)spellId, true);
                else
                    player.RemoveAuraFromStack((uint)-spellId);
            }
        }
    }

    // Enable or Disable battlefield
    public void ToggleBattlefield(bool enable)
    {
        m_IsEnabled = enable;
    }

    public virtual bool Update(uint diff)
    {
        if (m_Timer <= diff)
        {
            // Battlefield ends on time
            if (IsWarTime())
                EndBattle(true);
            else // Time to start a new battle!
                StartBattle();
        }
        else
        {
            m_Timer -= diff;
        }

        // Invite players a few minutes before the battle's beginning
        if (!IsWarTime() && !m_StartGrouping && m_Timer <= m_StartGroupingTimer)
        {
            m_StartGrouping = true;
            InvitePlayersInZoneToQueue();
            OnStartGrouping();
        }

        var objective_changed = false;

        if (IsWarTime())
        {
            if (m_uiKickAfkPlayersTimer <= diff)
            {
                m_uiKickAfkPlayersTimer = 1000;
                KickAfkPlayers();
            }
            else
            {
                m_uiKickAfkPlayersTimer -= diff;
            }

            // Kick players who chose not to accept invitation to the battle
            if (m_uiKickDontAcceptTimer <= diff)
            {
                var now = GameTime.CurrentTime;

                for (var team = 0; team < SharedConst.PvpTeamsCount; team++)
                    foreach (var pair in m_InvitedPlayers[team])
                        if (pair.Value <= now)
                            KickPlayerFromBattlefield(pair.Key);

                InvitePlayersInZoneToWar();

                for (var team = 0; team < SharedConst.PvpTeamsCount; team++)
                    foreach (var pair in m_PlayersWillBeKick[team])
                        if (pair.Value <= now)
                            KickPlayerFromBattlefield(pair.Key);

                m_uiKickDontAcceptTimer = 1000;
            }
            else
            {
                m_uiKickDontAcceptTimer -= diff;
            }

            foreach (var pair in m_capturePoints)
                if (pair.Value.Update(diff))
                    objective_changed = true;
        }

        if (m_LastResurectTimer <= diff)
        {
            for (byte i = 0; i < m_GraveyardList.Count; i++)
                if (GetGraveyardById(i) != null)
                    m_GraveyardList[i].Resurrect();

            m_LastResurectTimer = BattlegroundConst.ResurrectionInterval;
        }
        else
        {
            m_LastResurectTimer -= diff;
        }

        return objective_changed;
    }

    public virtual void UpdateData(int index, int pad)
    {
        if (pad < 0)
            m_Data32[index] -= (uint)-pad;
        else
            m_Data32[index] += (uint)pad;
    }

    private bool AddOrSetPlayerToCorrectBfGroup(Player player)
    {
        if (!player.Location.IsInWorld)
            return false;

        var oldgroup = player.Group;

        if (oldgroup)
            oldgroup.RemoveMember(player.GUID);

        var group = GetFreeBfRaid(player.TeamId);

        if (!group)
        {
            group = new PlayerGroup();
            group.SetBattlefieldGroup(this);
            group.Create(player);
            Global.GroupMgr.AddGroup(group);
            m_Groups[player.TeamId].Add(group.GUID);
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

    private void DoPlaySoundToAll(uint soundID)
    {
        BroadcastPacketToWar(new PlaySound(ObjectGuid.Empty, soundID, 0));
    }

    private BfCapturePoint GetCapturePoint(uint entry)
    {
        return m_capturePoints.LookupByKey(entry);
    }

    // ****************************************************
    // ******************* Group System *******************
    // ****************************************************
    private PlayerGroup GetFreeBfRaid(int teamIndex)
    {
        foreach (var guid in m_Groups[teamIndex])
        {
            var group = Global.GroupMgr.GetGroupByGUID(guid);

            if (group)
                if (!group.IsFull)
                    return group;
        }

        return null;
    }

    private List<BfGraveyard> GetGraveyardVector()
    {
        return m_GraveyardList;
    }

    private PlayerGroup GetGroupPlayer(ObjectGuid plguid, int teamIndex)
    {
        foreach (var guid in m_Groups[teamIndex])
        {
            var group = Global.GroupMgr.GetGroupByGUID(guid);

            if (group)
                if (group.IsMember(plguid))
                    return group;
        }

        return null;
    }

    private BattlefieldState GetState()
    {
        return m_isActive ? BattlefieldState.InProgress : (m_Timer <= m_StartGroupingTimer ? BattlefieldState.Warnup : BattlefieldState.Inactive);
    }

    private void InvitePlayersInQueueToWar()
    {
        for (byte team = 0; team < 2; ++team)
        {
            foreach (var guid in m_PlayersInQueue[team])
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                {
                    if (m_PlayersInWar[player.TeamId].Count + m_InvitedPlayers[player.TeamId].Count < m_MaxPlayer)
                    {
                        InvitePlayerToWar(player);
                    }
                    else
                    {
                        //Full
                    }
                }
            }

            m_PlayersInQueue[team].Clear();
        }
    }

    private void InvitePlayersInZoneToQueue()
    {
        for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
            foreach (var guid in m_players[team])
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                    InvitePlayerToQueue(player);
            }
    }

    private void InvitePlayersInZoneToWar()
    {
        for (byte team = 0; team < 2; ++team)
            foreach (var guid in m_players[team])
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                {
                    if (m_PlayersInWar[player.TeamId].Contains(player.GUID) || m_InvitedPlayers[player.TeamId].ContainsKey(player.GUID))
                        continue;

                    if (m_PlayersInWar[player.TeamId].Count + m_InvitedPlayers[player.TeamId].Count < m_MaxPlayer)
                        InvitePlayerToWar(player);
                    else // Battlefield is full of players
                        m_PlayersWillBeKick[player.TeamId][player.GUID] = GameTime.CurrentTime + 10;
                }
            }
    }

    private void InvitePlayerToQueue(Player player)
    {
        if (m_PlayersInQueue[player.TeamId].Contains(player.GUID))
            return;

        if (m_PlayersInQueue[player.TeamId].Count <= m_MinPlayer || m_PlayersInQueue[GetOtherTeam(player.TeamId)].Count >= m_MinPlayer)
            PlayerAcceptInviteToQueue(player);
    }

    private void InvitePlayerToWar(Player player)
    {
        if (!player)
            return;

        // todo needed ?
        if (player.IsInFlight)
            return;

        if (player.InArena || player.Battleground)
        {
            m_PlayersInQueue[player.TeamId].Remove(player.GUID);

            return;
        }

        // If the player does not match minimal level requirements for the battlefield, kick him
        if (player.Level < m_MinLevel)
        {
            if (!m_PlayersWillBeKick[player.TeamId].ContainsKey(player.GUID))
                m_PlayersWillBeKick[player.TeamId][player.GUID] = GameTime.CurrentTime + 10;

            return;
        }

        // Check if player is not already in war
        if (m_PlayersInWar[player.TeamId].Contains(player.GUID) || m_InvitedPlayers[player.TeamId].ContainsKey(player.GUID))
            return;

        m_PlayersWillBeKick[player.TeamId].Remove(player.GUID);
        m_InvitedPlayers[player.TeamId][player.GUID] = GameTime.CurrentTime + m_TimeForAcceptInvite;
        PlayerAcceptInviteToWar(player);
    }

    private void KickAfkPlayers()
    {
        for (byte team = 0; team < 2; ++team)
            foreach (var guid in m_PlayersInWar[team])
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                    if (player.IsAFK)
                        KickPlayerFromBattlefield(guid);
            }
    }

    private void SetDefenderTeam(uint team)
    {
        m_DefenderTeam = team;
    }
}