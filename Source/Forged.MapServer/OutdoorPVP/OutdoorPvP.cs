// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Networking;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.OutdoorPVP;

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
        if (m_capturePoints.ContainsKey(cp.CapturePointSpawnId))
            Log.Logger.Error("OutdoorPvP.AddCapturePoint: CapturePoint {0} already exists!", cp.CapturePointSpawnId);

        m_capturePoints[cp.CapturePointSpawnId] = cp;
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
            cp.CapturePoint = go;
    }

    public override void OnGameObjectRemove(GameObject go)
    {
        if (go.GoType != GameObjectTypes.ControlZone)
            return;

        var cp = GetCapturePoint(go.SpawnId);

        if (cp != null)
            cp.CapturePoint = null;
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