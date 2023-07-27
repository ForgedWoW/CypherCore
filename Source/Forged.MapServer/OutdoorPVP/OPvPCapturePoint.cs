// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.OutdoorPVP;

public class OPvPCapturePoint
{
    // maximum speed of capture
    private float _maxSpeed;

    private float _minValue;
    private uint _team;

    public OPvPCapturePoint(OutdoorPvP pvp)
    {
        _team = TeamIds.Neutral;
        OldState = ObjectiveStates.Neutral;
        State = ObjectiveStates.Neutral;
        PvP = pvp;

        ActivePlayers[0] = new HashSet<ObjectGuid>();
        ActivePlayers[1] = new HashSet<ObjectGuid>();
    }

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
        if (CapturePoint == null)
            return ActivePlayers[player.TeamId].Add(player.GUID);

        player.SendUpdateWorldState(CapturePoint.Template.ControlZone.worldState1, 1);
        player.SendUpdateWorldState(CapturePoint.Template.ControlZone.worldstate2, (uint)Math.Ceiling((Value + MaxValue) / (2 * MaxValue) * 100.0f));
        player.SendUpdateWorldState(CapturePoint.Template.ControlZone.worldstate3, NeutralValuePct);

        return ActivePlayers[player.TeamId].Add(player.GUID);
    }

    public virtual void HandlePlayerLeave(Player player)
    {
        if (CapturePoint != null)
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
        if (CapturePoint == null)
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
            PvP.Map.ObjectAccessor.FindPlayer(playerGuid)?.KilledMonsterCredit(id, guid);
    }

    public void SendUpdateWorldState(uint field, uint value)
    {
        for (var team = 0; team < 2; ++team)
            // send to all players present in the area
            foreach (var guid in ActivePlayers[team])
                PvP.Map.ObjectAccessor.FindPlayer(guid)?.SendUpdateWorldState(field, value);
    }

    public bool SetCapturePointData(uint entry)
    {
        Log.Logger.Debug("Creating capture point {0}", entry);

        // check info existence
        var goinfo = PvP.Map.GameObjectManager.GameObjectTemplateCache.GetGameObjectTemplate(entry);

        if (goinfo is not { type: GameObjectTypes.ControlZone })
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
        if (CapturePoint == null)
            return false;

        float radius = CapturePoint.Template.ControlZone.radius;

        for (var team = 0; team < 2; ++team)
            foreach (var playerGuid in ActivePlayers[team].ToList())
            {
                var player = PvP.Map.ObjectAccessor.FindPlayer(playerGuid);

                if (player == null)
                    continue;

                if (!CapturePoint.Location.IsWithinDistInMap(player, radius) || !player.IsOutdoorPvPActive())
                    HandlePlayerLeave(player);
            }

        List<Unit> players = new();
        var checker = new AnyPlayerInObjectRangeCheck(CapturePoint, radius);
        var searcher = new PlayerListSearcher(CapturePoint, players, checker);
        PvP.Map.CellCalculator.VisitGrid(CapturePoint, searcher, radius);

        foreach (var player in from Player player in players where player.IsOutdoorPvPActive() where ActivePlayers[player.TeamId].Add(player.GUID) select player)
            HandlePlayerEnter(player);

        // get the difference of numbers
        var factDiff = (float)(ActivePlayers[0].Count - ActivePlayers[1].Count) * diff / 1000;

        if (factDiff == 0.0f)
            return false;

        TeamFaction challenger;
        var maxDiff = _maxSpeed * diff;

        if (factDiff < 0)
        {
            // horde is in majority, but it's already horde-controlled . no change
            if (State == ObjectiveStates.Horde && Value <= -MaxValue)
                return false;

            if (factDiff < -maxDiff)
                factDiff = -maxDiff;

            challenger = TeamFaction.Horde;
        }
        else
        {
            // ally is in majority, but it's already ally-controlled . no change
            if (State == ObjectiveStates.Alliance && Value >= MaxValue)
                return false;

            if (factDiff > maxDiff)
                factDiff = maxDiff;

            challenger = TeamFaction.Alliance;
        }

        var oldValue = Value;
        var oldTeam = _team;

        OldState = State;

        Value += factDiff;

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
            State = challenger switch
            {
                // if challenger is ally, then n.a challenge
                TeamFaction.Alliance => ObjectiveStates.NeutralAllianceChallenge,
                // if challenger is horde, then n.h challenge
                TeamFaction.Horde => ObjectiveStates.NeutralHordeChallenge,
                _                 => State
            };

            _team = TeamIds.Neutral;
        }
        else // grey, did not go through mid point
        {
            State = challenger switch
            {
                // old phase and current are on the same side, so one team challenges the other
                TeamFaction.Alliance when OldState is ObjectiveStates.Horde or ObjectiveStates.NeutralHordeChallenge    => ObjectiveStates.HordeAllianceChallenge,
                TeamFaction.Horde when OldState is ObjectiveStates.Alliance or ObjectiveStates.NeutralAllianceChallenge => ObjectiveStates.AllianceHordeChallenge,
                _                                                                                                       => State
            };

            _team = TeamIds.Neutral;
        }

        if (Value != oldValue)
            SendChangePhase();

        if (OldState == State)
            return false;

        if (oldTeam != _team)
            ChangeTeam(oldTeam);

        ChangeState();

        return true;
    }
}