// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.OutdoorPVP;

public class OPvPCapturePoint
{
    // active players in the area of the objective, 0 - alliance, 1 - horde
    public HashSet<ObjectGuid>[] ActivePlayers { get; set; } = new HashSet<ObjectGuid>[2];

    public GameObject CapturePoint { get; set; }

    public ulong CapturePointSpawnId { get; set; }

    // total shift needed to capture the objective
    public float MaxValue { get; set; }

    // neutral value on capture bar
    public uint NeutralValuePct { get; set; }

    // the status of the objective
    public float Value { get; set; }

    // maximum speed of capture
    private float _maxSpeed;

    private float _minValue;
    private uint _team;

    // objective states
    public ObjectiveStates OldState { get; set; }

    // pointer to the OutdoorPvP this objective belongs to
    public OutdoorPvP PvP { get; set; }

    public ObjectiveStates State { get; set; }

    public OPvPCapturePoint(OutdoorPvP pvp)
    {
        _team = TeamIds.Neutral;
        OldState = ObjectiveStates.Neutral;
        State = ObjectiveStates.Neutral;
        PvP = pvp;

        ActivePlayers[0] = new HashSet<ObjectGuid>();
        ActivePlayers[1] = new HashSet<ObjectGuid>();
    }

    public virtual void ChangeState()
    { }

    public virtual void ChangeTeam(uint oldTeam)
    { }

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
        if (CapturePoint)
        {
            player.SendUpdateWorldState(CapturePoint.Template.ControlZone.worldState1, 1);
            player.SendUpdateWorldState(CapturePoint.Template.ControlZone.worldstate2, (uint)Math.Ceiling((Value + MaxValue) / (2 * MaxValue) * 100.0f));
            player.SendUpdateWorldState(CapturePoint.Template.ControlZone.worldstate3, NeutralValuePct);
        }

        return ActivePlayers[player.TeamId].Add(player.GUID);
    }

    public virtual void HandlePlayerLeave(Player player)
    {
        if (CapturePoint)
            player.SendUpdateWorldState(CapturePoint.Template.ControlZone.worldState1, 0);

        ActivePlayers[player.TeamId].Remove(player.GUID);
    }

    public bool IsInsideObjective(Player player)
    {
        var plSet = ActivePlayers[player.TeamId];

        return plSet.Contains(player.GUID);
    }

    public virtual void SendChangePhase()
    {
        if (!CapturePoint)
            return;

        // send this too, sometimes the slider disappears, dunno why :(
        SendUpdateWorldState(CapturePoint.Template.ControlZone.worldState1, 1);
        // send these updates to only the ones in this objective
        SendUpdateWorldState(CapturePoint.Template.ControlZone.worldstate2, (uint)Math.Ceiling((Value + MaxValue) / (2 * MaxValue) * 100.0f));
        // send this too, sometimes it resets :S
        SendUpdateWorldState(CapturePoint.Template.ControlZone.worldstate3, NeutralValuePct);
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
        foreach (var playerGuid in ActivePlayers[team])
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
            foreach (var guid in ActivePlayers[team])
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
        MaxValue = goinfo.ControlZone.maxTime;
        _maxSpeed = MaxValue / (goinfo.ControlZone.minTime != 0 ? goinfo.ControlZone.minTime : 60);
        NeutralValuePct = goinfo.ControlZone.neutralPercent;
        _minValue = MathFunctions.CalculatePct(MaxValue, NeutralValuePct);

        return true;
    }

    public virtual bool Update(uint diff)
    {
        if (!CapturePoint)
            return false;

        float radius = CapturePoint.Template.ControlZone.radius;

        for (var team = 0; team < 2; ++team)
            foreach (var playerGuid in ActivePlayers[team].ToList())
            {
                var player = Global.ObjAccessor.FindPlayer(playerGuid);

                if (player)
                    if (!CapturePoint.Location.IsWithinDistInMap(player, radius) || !player.IsOutdoorPvPActive())
                        HandlePlayerLeave(player);
            }

        List<Unit> players = new();
        var checker = new AnyPlayerInObjectRangeCheck(CapturePoint, radius);
        var searcher = new PlayerListSearcher(CapturePoint, players, checker);
        Cell.VisitGrid(CapturePoint, searcher, radius);

        foreach (Player player in players)
            if (player.IsOutdoorPvPActive())
                if (ActivePlayers[player.TeamId].Add(player.GUID))
                    HandlePlayerEnter(player);

        // get the difference of numbers
        var fact_diff = (float)(ActivePlayers[0].Count - ActivePlayers[1].Count) * diff / 1000;

        if (fact_diff == 0.0f)
            return false;

        TeamFaction Challenger;
        var maxDiff = _maxSpeed * diff;

        if (fact_diff < 0)
        {
            // horde is in majority, but it's already horde-controlled . no change
            if (State == ObjectiveStates.Horde && Value <= -MaxValue)
                return false;

            if (fact_diff < -maxDiff)
                fact_diff = -maxDiff;

            Challenger = TeamFaction.Horde;
        }
        else
        {
            // ally is in majority, but it's already ally-controlled . no change
            if (State == ObjectiveStates.Alliance && Value >= MaxValue)
                return false;

            if (fact_diff > maxDiff)
                fact_diff = maxDiff;

            Challenger = TeamFaction.Alliance;
        }

        var oldValue = Value;
        var oldTeam = _team;

        OldState = State;

        Value += fact_diff;

        if (Value < -_minValue) // red
        {
            if (Value < -MaxValue)
                Value = -MaxValue;

            State = ObjectiveStates.Horde;
            _team = TeamIds.Horde;
        }
        else if (Value > _minValue) // blue
        {
            if (Value > MaxValue)
                Value = MaxValue;

            State = ObjectiveStates.Alliance;
            _team = TeamIds.Alliance;
        }
        else if (oldValue * Value <= 0) // grey, go through mid point
        {
            // if challenger is ally, then n.a challenge
            if (Challenger == TeamFaction.Alliance)
                State = ObjectiveStates.NeutralAllianceChallenge;
            // if challenger is horde, then n.h challenge
            else if (Challenger == TeamFaction.Horde)
                State = ObjectiveStates.NeutralHordeChallenge;

            _team = TeamIds.Neutral;
        }
        else // grey, did not go through mid point
        {
            // old phase and current are on the same side, so one team challenges the other
            if (Challenger == TeamFaction.Alliance && (OldState == ObjectiveStates.Horde || OldState == ObjectiveStates.NeutralHordeChallenge))
                State = ObjectiveStates.HordeAllianceChallenge;
            else if (Challenger == TeamFaction.Horde && (OldState == ObjectiveStates.Alliance || OldState == ObjectiveStates.NeutralAllianceChallenge))
                State = ObjectiveStates.AllianceHordeChallenge;

            _team = TeamIds.Neutral;
        }

        if (Value != oldValue)
            SendChangePhase();

        if (OldState != State)
        {
            if (oldTeam != _team)
                ChangeTeam(oldTeam);

            ChangeState();

            return true;
        }

        return false;
    }
}