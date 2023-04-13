// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.BattleFields;

public class BfCapturePoint
{
    // Battlefield this objective belongs to
    protected BattleField Bf;

    protected uint Team;

    // active Players in the area of the objective, 0 - alliance, 1 - horde
    private readonly HashSet<ObjectGuid>[] _activePlayers = new HashSet<ObjectGuid>[SharedConst.PvpTeamsCount];

    private readonly ObjectAccessor _objectAccessor;

    // Capture point entry
    private uint _capturePointEntry;

    // Gameobject related to that capture point
    private ObjectGuid _capturePointGUID;

    // Maximum speed of capture
    private float _maxSpeed;

    // Total shift needed to capture the objective
    private float _maxValue;

    private float _minValue;

    // Neutral value on capture bar
    private uint _neutralValuePct;

    // Objective states
    private BattleFieldObjectiveStates _oldState;

    private BattleFieldObjectiveStates _state;

    // The status of the objective
    private float _value;

    public BfCapturePoint(BattleField battlefield, ObjectAccessor objectAccessor)
    {
        Bf = battlefield;
        _objectAccessor = objectAccessor;
        _capturePointGUID = ObjectGuid.Empty;
        Team = TeamIds.Neutral;
        _value = 0;
        _minValue = 0.0f;
        _maxValue = 0.0f;
        _state = BattleFieldObjectiveStates.Neutral;
        _oldState = BattleFieldObjectiveStates.Neutral;
        _capturePointEntry = 0;
        _neutralValuePct = 0;
        _maxSpeed = 0;

        _activePlayers[0] = new HashSet<ObjectGuid>();
        _activePlayers[1] = new HashSet<ObjectGuid>();
    }

    public virtual void ChangeTeam(uint oldTeam) { }

    public uint GetCapturePointEntry()
    {
        return _capturePointEntry;
    }

    public virtual bool HandlePlayerEnter(Player player)
    {
        if (_capturePointGUID.IsEmpty)
            return _activePlayers[player.TeamId].Add(player.GUID);

        var capturePoint = Bf.GetGameObject(_capturePointGUID);

        if (!capturePoint)
            return _activePlayers[player.TeamId].Add(player.GUID);

        player.SendUpdateWorldState(capturePoint.Template.ControlZone.worldState1, 1);
        player.SendUpdateWorldState(capturePoint.Template.ControlZone.worldstate2, (uint)Math.Ceiling((_value + _maxValue) / (2 * _maxValue) * 100.0f));
        player.SendUpdateWorldState(capturePoint.Template.ControlZone.worldstate3, _neutralValuePct);

        return _activePlayers[player.TeamId].Add(player.GUID);
    }

    public virtual void HandlePlayerLeave(Player player)
    {
        if (!_capturePointGUID.IsEmpty)
        {
            var capturePoint = Bf.GetGameObject(_capturePointGUID);

            if (capturePoint)
                player.SendUpdateWorldState(capturePoint.Template.ControlZone.worldState1, 0);
        }

        _activePlayers[player.TeamId].Remove(player.GUID);
    }

    public virtual void SendChangePhase()
    {
        if (_capturePointGUID.IsEmpty)
            return;

        var capturePoint = Bf.GetGameObject(_capturePointGUID);

        if (capturePoint)
        {
            // send this too, sometimes the slider disappears, dunno why :(
            SendUpdateWorldState(capturePoint.Template.ControlZone.worldState1, 1);
            // send these updates to only the ones in this objective
            SendUpdateWorldState(capturePoint.Template.ControlZone.worldstate2, (uint)Math.Ceiling((_value + _maxValue) / (2 * _maxValue) * 100.0f));
            // send this too, sometimes it resets :S
            SendUpdateWorldState(capturePoint.Template.ControlZone.worldstate3, _neutralValuePct);
        }
    }

    public bool SetCapturePointData(GameObject capturePoint)
    {
        Log.Logger.Error("Creating capture point {0}", capturePoint.Entry);

        _capturePointGUID = capturePoint.GUID;
        _capturePointEntry = capturePoint.Entry;

        // check info existence
        var goinfo = capturePoint.Template;

        if (goinfo.type != GameObjectTypes.ControlZone)
        {
            Log.Logger.Error("OutdoorPvP: GO {0} is not capture point!", capturePoint.Entry);

            return false;
        }

        // get the needed values from goinfo
        _maxValue = goinfo.ControlZone.maxTime;
        _maxSpeed = _maxValue / (goinfo.ControlZone.minTime != 0 ? goinfo.ControlZone.minTime : 60);
        _neutralValuePct = goinfo.ControlZone.neutralPercent;
        _minValue = _maxValue * goinfo.ControlZone.neutralPercent / 100;

        if (Team == TeamIds.Alliance)
        {
            _value = _maxValue;
            _state = BattleFieldObjectiveStates.Alliance;
        }
        else
        {
            _value = -_maxValue;
            _state = BattleFieldObjectiveStates.Horde;
        }

        return true;
    }

    public virtual bool Update(uint diff)
    {
        if (_capturePointGUID.IsEmpty)
            return false;

        var capturePoint = Bf.GetGameObject(_capturePointGUID);

        if (capturePoint)
        {
            float radius = capturePoint.Template.ControlZone.radius;

            for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
                foreach (var guid in _activePlayers[team])
                {
                    var player = _objectAccessor.FindPlayer(guid);

                    if (!player)
                        continue;

                    if (!capturePoint.Location.IsWithinDistInMap(player, radius) || !player.IsOutdoorPvPActive())
                        HandlePlayerLeave(player);
                }

            List<Unit> players = new();
            var checker = new AnyPlayerInObjectRangeCheck(capturePoint, radius);
            var searcher = new PlayerListSearcher(capturePoint, players, checker);
            Cell.VisitGrid(capturePoint, searcher, radius);

            foreach (var player in from Player player in players
                                   where
                                       player.IsOutdoorPvPActive()
                                   where
                                       _activePlayers[player.TeamId].Add(player.GUID)
                                   select player)
                HandlePlayerEnter(player);
        }

        // get the difference of numbers
        var factDiff = ((float)_activePlayers[TeamIds.Alliance].Count - _activePlayers[TeamIds.Horde].Count) * diff / 1000;

        if (MathFunctions.fuzzyEq(factDiff, 0.0f))
            return false;

        TeamFaction challenger;
        var maxDiff = _maxSpeed * diff;

        if (factDiff < 0)
        {
            // horde is in majority, but it's already horde-controlled . no change
            if (_state == BattleFieldObjectiveStates.Horde && _value <= -_maxValue)
                return false;

            if (factDiff < -maxDiff)
                factDiff = -maxDiff;

            challenger = TeamFaction.Horde;
        }
        else
        {
            // ally is in majority, but it's already ally-controlled . no change
            if (_state == BattleFieldObjectiveStates.Alliance && _value >= _maxValue)
                return false;

            if (factDiff > maxDiff)
                factDiff = maxDiff;

            challenger = TeamFaction.Alliance;
        }

        var oldValue = _value;
        var oldTeam = Team;

        _oldState = _state;

        _value += factDiff;

        if (_value < -_minValue) // red
        {
            if (_value < -_maxValue)
                _value = -_maxValue;

            _state = BattleFieldObjectiveStates.Horde;
            Team = TeamIds.Horde;
        }
        else if (_value > _minValue) // blue
        {
            if (_value > _maxValue)
                _value = _maxValue;

            _state = BattleFieldObjectiveStates.Alliance;
            Team = TeamIds.Alliance;
        }
        else if (oldValue * _value <= 0) // grey, go through mid point
        {
            // if challenger is ally, then n.a challenge
            _state = challenger == TeamFaction.Alliance ? BattleFieldObjectiveStates.NeutralAllianceChallenge : BattleFieldObjectiveStates.NeutralHordeChallenge;

            Team = TeamIds.Neutral;
        }
        else // grey, did not go through mid point
        {
            _state = challenger switch
            {
                // old phase and current are on the same side, so one team challenges the other
                TeamFaction.Alliance when _oldState is BattleFieldObjectiveStates.Horde or BattleFieldObjectiveStates.NeutralHordeChallenge    => BattleFieldObjectiveStates.HordeAllianceChallenge,
                TeamFaction.Horde when _oldState is BattleFieldObjectiveStates.Alliance or BattleFieldObjectiveStates.NeutralAllianceChallenge => BattleFieldObjectiveStates.AllianceHordeChallenge,
                _                                                                                                                              => _state
            };

            Team = TeamIds.Neutral;
        }

        if (MathFunctions.fuzzyNe(_value, oldValue))
            SendChangePhase();

        if (_oldState != _state)
        {
            if (oldTeam != Team)
                ChangeTeam(oldTeam);

            return true;
        }

        return false;
    }

    private bool DelCapturePoint()
    {
        if (!_capturePointGUID.IsEmpty)
        {
            var capturePoint = Bf.GetGameObject(_capturePointGUID);

            if (capturePoint)
            {
                capturePoint.SetRespawnTime(0); // not save respawn time
                capturePoint.Delete();
                capturePoint.Dispose();
            }

            _capturePointGUID.Clear();
        }

        return true;
    }

    private GameObject GetCapturePointGo()
    {
        return Bf.GetGameObject(_capturePointGUID);
    }

    private uint GetTeamId()
    {
        return Team;
    }

    private bool IsInsideObjective(Player player)
    {
        return _activePlayers[player.TeamId].Contains(player.GUID);
    }

    private void SendObjectiveComplete(uint id, ObjectGuid oGuid)
    {
        uint team;

        switch (_state)
        {
            case BattleFieldObjectiveStates.Alliance:
                team = TeamIds.Alliance;

                break;

            case BattleFieldObjectiveStates.Horde:
                team = TeamIds.Horde;

                break;

            default:
                return;
        }

        // send to all players present in the area
        foreach (var player in _activePlayers[team].Select(guid => _objectAccessor.FindPlayer(guid)).Where(player => player))
            player.KilledMonsterCredit(id, oGuid);
    }

    private void SendUpdateWorldState(uint field, uint value)
    {
        for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
            foreach (var player in _activePlayers[team].Select(guid => _objectAccessor.FindPlayer(guid)).Where(player => player))
                player.SendUpdateWorldState(field, value);
    }
}