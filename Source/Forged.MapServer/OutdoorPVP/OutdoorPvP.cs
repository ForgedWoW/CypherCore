// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
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
    public Dictionary<ulong, OPvPCapturePoint> CapturePoints { get; set; } = new();
    public OutdoorPvPTypes TypeId { get; set; }
    public Map Map { get; }
    private readonly List<ObjectGuid>[] _players = new List<ObjectGuid>[2];

    public OutdoorPvP(Map map)
    {
        TypeId = 0;
        Map = map;
        _players[0] = new List<ObjectGuid>();
        _players[1] = new List<ObjectGuid>();
    }

    public void AddCapturePoint(OPvPCapturePoint cp)
    {
        if (CapturePoints.ContainsKey(cp.CapturePointSpawnId))
            Log.Logger.Error("OutdoorPvP.AddCapturePoint: CapturePoint {0} already exists!", cp.CapturePointSpawnId);

        CapturePoints[cp.CapturePointSpawnId] = cp;
    }

    // awards rewards for player kill
    public virtual void AwardKillBonus(Player player) { }

    public int GetWorldState(int worldStateId)
    {
        return Map.WorldStateManager.GetValue(worldStateId, Map);
    }

    public virtual bool HandleAreaTrigger(Player player, uint trigger, bool entered)
    {
        return false;
    }

    public virtual bool HandleCustomSpell(Player player, uint spellId, GameObject go)
    {
        return CapturePoints.Any(pair => pair.Value.HandleCustomSpell(player, spellId, go));
    }

    public virtual bool HandleDropFlag(Player player, uint id)
    {
        return CapturePoints.Any(pair => pair.Value.HandleDropFlag(player, id));
    }

    public virtual void HandleKill(Player killer, Unit killed)
    {
        if (killer.Group != null)
        {
            for (var refe = killer.Group.FirstMember; refe != null; refe = refe.Next())
            {
                var groupGuy = refe.Source;

                if (groupGuy == null)
                    continue;

                // skip if too far away
                if (!groupGuy.IsAtGroupRewardDistance(killed))
                    continue;

                // creature kills must be notified, even if not inside objective / not outdoor pvp active
                // player kills only count if active and inside objective
                if ((groupGuy.IsOutdoorPvPActive() && IsInsideObjective(groupGuy)) || killed.IsTypeId(Framework.Constants.TypeId.Unit))
                    HandleKillImpl(groupGuy, killed);
            }
        }
        else
        {
            // creature kills must be notified, even if not inside objective / not outdoor pvp active
            if ((killer.IsOutdoorPvPActive() && IsInsideObjective(killer)) || killed.IsTypeId(Framework.Constants.TypeId.Unit))
                HandleKillImpl(killer, killed);
        }
    }

    public virtual void HandleKillImpl(Player killer, Unit killed) { }

    public virtual bool HandleOpenGo(Player player, GameObject go)
    {
        return CapturePoints.Any(pair => pair.Value.HandleOpenGo(player, go) >= 0);
    }

    public virtual void HandlePlayerEnterZone(Player player, uint zone)
    {
        _players[player.TeamId].Add(player.GUID);
    }

    public virtual void HandlePlayerLeaveZone(Player player, uint zone)
    {
        // inform the objectives of the leaving
        foreach (var pair in CapturePoints)
            pair.Value.HandlePlayerLeave(player);

        // remove the world state information from the player (we can't keep everyone up to date, so leave out those who are not in the concerning zones)
        if (!player.Session.PlayerLogout)
            SendRemoveWorldStates(player);

        _players[player.TeamId].Remove(player.GUID);
        Log.Logger.Debug("Player {0} left an outdoorpvp zone", player.GetName());
    }

    public virtual void HandlePlayerResurrects(Player player, uint zone) { }

    public bool HasPlayer(Player player)
    {
        return _players[player.TeamId].Contains(player.GUID);
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
        Map.OutdoorPvPManager.AddZone(zoneId, this);
    }

    public void SendDefenseMessage(uint zoneId, uint id)
    {
        DefenseMessageBuilder builder = new(zoneId, id, Map.OutdoorPvPManager);
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
        Map.WorldStateManager.SetValue(worldStateId, value, false, Map);
    }

    public void TeamApplyBuff(uint teamIndex, uint spellId, uint spellId2)
    {
        TeamCastSpell(teamIndex, (int)spellId);
        TeamCastSpell((uint)(teamIndex == TeamIds.Alliance ? TeamIds.Horde : TeamIds.Alliance), spellId2 != 0 ? -(int)spellId2 : -(int)spellId);
    }

    public void TeamCastSpell(uint teamIndex, int spellId)
    {
        foreach (var player in _players[teamIndex].Select(guid => Map.ObjectAccessor.FindPlayer(guid)).Where(player => player != null))
        {
            if (spellId > 0)
                player.SpellFactory.CastSpell(player, (uint)spellId, true);
            else
                player.RemoveAura((uint)-spellId); // by stack?
        }
    }

    public virtual bool Update(uint diff)
    {
        var objectiveChanged = false;

        foreach (var _ in CapturePoints.Where(pair => pair.Value.Update(diff)))
            objectiveChanged = true;

        return objectiveChanged;
    }

    private void BroadcastPacket(ServerPacket packet)
    {
        // This is faster than sWorld.SendZoneMessage
        for (var team = 0; team < 2; ++team)
            foreach (var guid in _players[team])
                Map.ObjectAccessor.FindPlayer(guid)?.SendPacket(packet);
    }

    private void BroadcastWorker(IDoWork<Player> worker, uint zoneId)
    {
        for (uint i = 0; i < SharedConst.PvpTeamsCount; ++i)
            foreach (var guid in _players[i])
            {
                var player = Map.ObjectAccessor.FindPlayer(guid);

                if (player == null)
                    continue;

                if (player.Location.Zone == zoneId)
                    worker.Invoke(player);
            }
    }

    private OPvPCapturePoint GetCapturePoint(ulong lowguid)
    {
        return CapturePoints.LookupByKey(lowguid);
    }

    private bool IsInsideObjective(Player player)
    {
        return CapturePoints.Any(pair => pair.Value.IsInsideObjective(player));
    }
}