// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Chat;
using Forged.MapServer.Text;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.OutdoorPVP;

public class go_type
{
    public uint entry;
    public uint map;
    public Position pos;
    public Quaternion rot;

    public go_type(uint _entry, uint _map, float _x, float _y, float _z, float _o, float _rot0, float _rot1, float _rot2, float _rot3)
    {
        entry = _entry;
        map = _map;
        pos = new Position(_x, _y, _z, _o);
        rot = new Quaternion(_rot0, _rot1, _rot2, _rot3);
    }
}

public class OPvPCapturePoint
{
    // active players in the area of the objective, 0 - alliance, 1 - horde
    public HashSet<ObjectGuid>[] m_activePlayers = new HashSet<ObjectGuid>[2];

    public GameObject m_capturePoint;
    public ulong m_capturePointSpawnId;
    // total shift needed to capture the objective
    public float m_maxValue;

    // neutral value on capture bar
    public uint m_neutralValuePct;

    // the status of the objective
    public float m_value;
    // maximum speed of capture
    private float m_maxSpeed;

    private float m_minValue;
    private uint m_team;

    public OPvPCapturePoint(OutdoorPvP pvp)
    {
        m_team = TeamIds.Neutral;
        OldState = ObjectiveStates.Neutral;
        State = ObjectiveStates.Neutral;
        PvP = pvp;

        m_activePlayers[0] = new HashSet<ObjectGuid>();
        m_activePlayers[1] = new HashSet<ObjectGuid>();
    }

    // objective states
    public ObjectiveStates OldState { get; set; }

    // pointer to the OutdoorPvP this objective belongs to
    public OutdoorPvP PvP { get; set; }

    public ObjectiveStates State { get; set; }
    public virtual void ChangeState() { }

    public virtual void ChangeTeam(uint oldTeam) { }

    public virtual bool HandleCustomSpell(Player player, uint spellId, GameObject go)
    {
        if (!player.IsOutdoorPvPActive())
            return false;

        return false;
    }

    public virtual bool HandleDropFlag(Player player, uint id)
    {
        return false;
    }

    public virtual int HandleOpenGo(Player player, GameObject go)
    {
        return -1;
    }

    public virtual bool HandlePlayerEnter(Player player)
    {
        if (m_capturePoint)
        {
            player.SendUpdateWorldState(m_capturePoint.Template.ControlZone.worldState1, 1);
            player.SendUpdateWorldState(m_capturePoint.Template.ControlZone.worldstate2, (uint)Math.Ceiling((m_value + m_maxValue) / (2 * m_maxValue) * 100.0f));
            player.SendUpdateWorldState(m_capturePoint.Template.ControlZone.worldstate3, m_neutralValuePct);
        }

        return m_activePlayers[player.TeamId].Add(player.GUID);
    }

    public virtual void HandlePlayerLeave(Player player)
    {
        if (m_capturePoint)
            player.SendUpdateWorldState(m_capturePoint.Template.ControlZone.worldState1, 0);

        m_activePlayers[player.TeamId].Remove(player.GUID);
    }

    public bool IsInsideObjective(Player player)
    {
        var plSet = m_activePlayers[player.TeamId];

        return plSet.Contains(player.GUID);
    }

    public virtual void SendChangePhase()
    {
        if (!m_capturePoint)
            return;

        // send this too, sometimes the slider disappears, dunno why :(
        SendUpdateWorldState(m_capturePoint.Template.ControlZone.worldState1, 1);
        // send these updates to only the ones in this objective
        SendUpdateWorldState(m_capturePoint.Template.ControlZone.worldstate2, (uint)Math.Ceiling((m_value + m_maxValue) / (2 * m_maxValue) * 100.0f));
        // send this too, sometimes it resets :S
        SendUpdateWorldState(m_capturePoint.Template.ControlZone.worldstate3, m_neutralValuePct);
    }

    public void SendObjectiveComplete(uint id, ObjectGuid guid)
    {
        uint team;

        switch (State)
        {
            case ObjectiveStates.Alliance:
                team = 0;

                break;
            case ObjectiveStates.Horde:
                team = 1;

                break;
            default:
                return;
        }

        // send to all players present in the area
        foreach (var playerGuid in m_activePlayers[team])
        {
            var player = Global.ObjAccessor.FindPlayer(playerGuid);

            if (player)
                player.KilledMonsterCredit(id, guid);
        }
    }

    public void SendUpdateWorldState(uint field, uint value)
    {
        for (var team = 0; team < 2; ++team)
            // send to all players present in the area
            foreach (var guid in m_activePlayers[team])
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                    player.SendUpdateWorldState(field, value);
            }
    }

    public bool SetCapturePointData(uint entry)
    {
        Log.Logger.Debug("Creating capture point {0}", entry);

        // check info existence
        var goinfo = Global.ObjectMgr.GetGameObjectTemplate(entry);

        if (goinfo == null || goinfo.type != GameObjectTypes.ControlZone)
        {
            Log.Logger.Error("OutdoorPvP: GO {0} is not capture point!", entry);

            return false;
        }

        // get the needed values from goinfo
        m_maxValue = goinfo.ControlZone.maxTime;
        m_maxSpeed = m_maxValue / (goinfo.ControlZone.minTime != 0 ? goinfo.ControlZone.minTime : 60);
        m_neutralValuePct = goinfo.ControlZone.neutralPercent;
        m_minValue = MathFunctions.CalculatePct(m_maxValue, m_neutralValuePct);

        return true;
    }

    public virtual bool Update(uint diff)
    {
        if (!m_capturePoint)
            return false;

        float radius = m_capturePoint.Template.ControlZone.radius;

        for (var team = 0; team < 2; ++team)
            foreach (var playerGuid in m_activePlayers[team].ToList())
            {
                var player = Global.ObjAccessor.FindPlayer(playerGuid);

                if (player)
                    if (!m_capturePoint.Location.IsWithinDistInMap(player, radius) || !player.IsOutdoorPvPActive())
                        HandlePlayerLeave(player);
            }

        List<Unit> players = new();
        var checker = new AnyPlayerInObjectRangeCheck(m_capturePoint, radius);
        var searcher = new PlayerListSearcher(m_capturePoint, players, checker);
        Cell.VisitGrid(m_capturePoint, searcher, radius);

        foreach (Player player in players)
            if (player.IsOutdoorPvPActive())
                if (m_activePlayers[player.TeamId].Add(player.GUID))
                    HandlePlayerEnter(player);

        // get the difference of numbers
        var fact_diff = (float)(m_activePlayers[0].Count - m_activePlayers[1].Count) * diff / 1000;

        if (fact_diff == 0.0f)
            return false;

        TeamFaction Challenger;
        var maxDiff = m_maxSpeed * diff;

        if (fact_diff < 0)
        {
            // horde is in majority, but it's already horde-controlled . no change
            if (State == ObjectiveStates.Horde && m_value <= -m_maxValue)
                return false;

            if (fact_diff < -maxDiff)
                fact_diff = -maxDiff;

            Challenger = TeamFaction.Horde;
        }
        else
        {
            // ally is in majority, but it's already ally-controlled . no change
            if (State == ObjectiveStates.Alliance && m_value >= m_maxValue)
                return false;

            if (fact_diff > maxDiff)
                fact_diff = maxDiff;

            Challenger = TeamFaction.Alliance;
        }

        var oldValue = m_value;
        var oldTeam = m_team;

        OldState = State;

        m_value += fact_diff;

        if (m_value < -m_minValue) // red
        {
            if (m_value < -m_maxValue)
                m_value = -m_maxValue;

            State = ObjectiveStates.Horde;
            m_team = TeamIds.Horde;
        }
        else if (m_value > m_minValue) // blue
        {
            if (m_value > m_maxValue)
                m_value = m_maxValue;

            State = ObjectiveStates.Alliance;
            m_team = TeamIds.Alliance;
        }
        else if (oldValue * m_value <= 0) // grey, go through mid point
        {
            // if challenger is ally, then n.a challenge
            if (Challenger == TeamFaction.Alliance)
                State = ObjectiveStates.NeutralAllianceChallenge;
            // if challenger is horde, then n.h challenge
            else if (Challenger == TeamFaction.Horde)
                State = ObjectiveStates.NeutralHordeChallenge;

            m_team = TeamIds.Neutral;
        }
        else // grey, did not go through mid point
        {
            // old phase and current are on the same side, so one team challenges the other
            if (Challenger == TeamFaction.Alliance && (OldState == ObjectiveStates.Horde || OldState == ObjectiveStates.NeutralHordeChallenge))
                State = ObjectiveStates.HordeAllianceChallenge;
            else if (Challenger == TeamFaction.Horde && (OldState == ObjectiveStates.Alliance || OldState == ObjectiveStates.NeutralAllianceChallenge))
                State = ObjectiveStates.AllianceHordeChallenge;

            m_team = TeamIds.Neutral;
        }

        if (m_value != oldValue)
            SendChangePhase();

        if (OldState != State)
        {
            if (oldTeam != m_team)
                ChangeTeam(oldTeam);

            ChangeState();

            return true;
        }

        return false;
    }
}

// base class for specific outdoor pvp handlers
public class OutdoorPvP : ZoneScript
{
    // the map of the objectives belonging to this outdoorpvp
    public Dictionary<ulong, OPvPCapturePoint> m_capturePoints = new();
    public OutdoorPvPTypes m_TypeId;
    private readonly Map m_map;
    private readonly List<ObjectGuid>[] m_players = new List<ObjectGuid>[2];
    public OutdoorPvP(Map map)
    {
        m_TypeId = 0;
        m_map = map;
        m_players[0] = new List<ObjectGuid>();
        m_players[1] = new List<ObjectGuid>();
    }

    public void AddCapturePoint(OPvPCapturePoint cp)
    {
        if (m_capturePoints.ContainsKey(cp.m_capturePointSpawnId))
            Log.Logger.Error("OutdoorPvP.AddCapturePoint: CapturePoint {0} already exists!", cp.m_capturePointSpawnId);

        m_capturePoints[cp.m_capturePointSpawnId] = cp;
    }

    // awards rewards for player kill
    public virtual void AwardKillBonus(Player player) { }

    public Map GetMap()
    {
        return m_map;
    }

    public OutdoorPvPTypes GetTypeId()
    {
        return m_TypeId;
    }

    public int GetWorldState(int worldStateId)
    {
        return Global.WorldStateMgr.GetValue(worldStateId, m_map);
    }

    public virtual bool HandleAreaTrigger(Player player, uint trigger, bool entered)
    {
        return false;
    }

    public virtual bool HandleCustomSpell(Player player, uint spellId, GameObject go)
    {
        foreach (var pair in m_capturePoints)
            if (pair.Value.HandleCustomSpell(player, spellId, go))
                return true;

        return false;
    }

    public virtual bool HandleDropFlag(Player player, uint id)
    {
        foreach (var pair in m_capturePoints)
            if (pair.Value.HandleDropFlag(player, id))
                return true;

        return false;
    }

    public virtual void HandleKill(Player killer, Unit killed)
    {
        var group = killer.Group;

        if (group)
        {
            for (var refe = group.FirstMember; refe != null; refe = refe.Next())
            {
                var groupGuy = refe.Source;

                if (!groupGuy)
                    continue;

                // skip if too far away
                if (!groupGuy.IsAtGroupRewardDistance(killed))
                    continue;

                // creature kills must be notified, even if not inside objective / not outdoor pvp active
                // player kills only count if active and inside objective
                if ((groupGuy.IsOutdoorPvPActive() && IsInsideObjective(groupGuy)) || killed.IsTypeId(TypeId.Unit))
                    HandleKillImpl(groupGuy, killed);
            }
        }
        else
        {
            // creature kills must be notified, even if not inside objective / not outdoor pvp active
            if ((killer.IsOutdoorPvPActive() && IsInsideObjective(killer)) || killed.IsTypeId(TypeId.Unit))
                HandleKillImpl(killer, killed);
        }
    }

    public virtual void HandleKillImpl(Player killer, Unit killed) { }

    public virtual bool HandleOpenGo(Player player, GameObject go)
    {
        foreach (var pair in m_capturePoints)
            if (pair.Value.HandleOpenGo(player, go) >= 0)
                return true;

        return false;
    }

    public virtual void HandlePlayerEnterZone(Player player, uint zone)
    {
        m_players[player.TeamId].Add(player.GUID);
    }

    public virtual void HandlePlayerLeaveZone(Player player, uint zone)
    {
        // inform the objectives of the leaving
        foreach (var pair in m_capturePoints)
            pair.Value.HandlePlayerLeave(player);

        // remove the world state information from the player (we can't keep everyone up to date, so leave out those who are not in the concerning zones)
        if (!player.Session.PlayerLogout)
            SendRemoveWorldStates(player);

        m_players[player.TeamId].Remove(player.GUID);
        Log.Logger.Debug("Player {0} left an outdoorpvp zone", player.GetName());
    }

    public virtual void HandlePlayerResurrects(Player player, uint zone) { }

    public bool HasPlayer(Player player)
    {
        return m_players[player.TeamId].Contains(player.GUID);
    }

    public override void OnGameObjectCreate(GameObject go)
    {
        if (go.GoType != GameObjectTypes.ControlZone)
            return;

        var cp = GetCapturePoint(go.SpawnId);

        if (cp != null)
            cp.m_capturePoint = go;
    }

    public override void OnGameObjectRemove(GameObject go)
    {
        if (go.GoType != GameObjectTypes.ControlZone)
            return;

        var cp = GetCapturePoint(go.SpawnId);

        if (cp != null)
            cp.m_capturePoint = null;
    }

    public void RegisterZone(uint zoneId)
    {
        Global.OutdoorPvPMgr.AddZone(zoneId, this);
    }

    public void SendDefenseMessage(uint zoneId, uint id)
    {
        DefenseMessageBuilder builder = new(zoneId, id);
        var localizer = new LocalizedDo(builder);
        BroadcastWorker(localizer, zoneId);
    }

    public virtual void SendRemoveWorldStates(Player player) { }

    // setup stuff
    public virtual bool SetupOutdoorPvP()
    {
        return true;
    }

    public void SetWorldState(int worldStateId, int value)
    {
        Global.WorldStateMgr.SetValue(worldStateId, value, false, m_map);
    }

    public void TeamApplyBuff(uint teamIndex, uint spellId, uint spellId2)
    {
        TeamCastSpell(teamIndex, (int)spellId);
        TeamCastSpell((uint)(teamIndex == TeamIds.Alliance ? TeamIds.Horde : TeamIds.Alliance), spellId2 != 0 ? -(int)spellId2 : -(int)spellId);
    }

    public void TeamCastSpell(uint teamIndex, int spellId)
    {
        foreach (var guid in m_players[teamIndex])
        {
            var player = Global.ObjAccessor.FindPlayer(guid);

            if (player)
            {
                if (spellId > 0)
                    player.CastSpell(player, (uint)spellId, true);
                else
                    player.RemoveAura((uint)-spellId); // by stack?
            }
        }
    }

    public virtual bool Update(uint diff)
    {
        var objective_changed = false;

        foreach (var pair in m_capturePoints)
            if (pair.Value.Update(diff))
                objective_changed = true;

        return objective_changed;
    }
    private void BroadcastPacket(ServerPacket packet)
    {
        // This is faster than sWorld.SendZoneMessage
        for (var team = 0; team < 2; ++team)
            foreach (var guid in m_players[team])
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                    player.SendPacket(packet);
            }
    }

    private void BroadcastWorker(IDoWork<Player> _worker, uint zoneId)
    {
        for (uint i = 0; i < SharedConst.PvpTeamsCount; ++i)
            foreach (var guid in m_players[i])
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                    if (player.Zone == zoneId)
                        _worker.Invoke(player);
            }
    }

    private OPvPCapturePoint GetCapturePoint(ulong lowguid)
    {
        return m_capturePoints.LookupByKey(lowguid);
    }

    private bool IsInsideObjective(Player player)
    {
        foreach (var pair in m_capturePoints)
            if (pair.Value.IsInsideObjective(player))
                return true;

        return false;
    }
}
internal class DefenseMessageBuilder : MessageBuilder
{
    private readonly uint _id;
    private readonly uint _zoneId; // ZoneId
                                   // BroadcastTextId

    public DefenseMessageBuilder(uint zoneId, uint id)
    {
        _zoneId = zoneId;
        _id = id;
    }

    public override PacketSenderOwning<DefenseMessage> Invoke(Locale locale = Locale.enUS)
    {
        var text = Global.OutdoorPvPMgr.GetDefenseMessage(_zoneId, _id, locale);

        PacketSenderOwning<DefenseMessage> defenseMessage = new()
        {
            Data =
            {
                ZoneID = _zoneId,
                MessageText = text
            }
        };

        return defenseMessage;
    }
}
internal class creature_type
{
    public uint entry;
    public uint map;
    private readonly Position pos;

    public creature_type(uint _entry, uint _map, float _x, float _y, float _z, float _o)
    {
        entry = _entry;
        map = _map;
        pos = new Position(_x, _y, _z, _o);
    }
}