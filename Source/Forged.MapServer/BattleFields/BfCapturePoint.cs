// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.BattleFields;

public class BfCapturePoint
{
    // Battlefield this objective belongs to
    protected BattleField m_Bf;

    protected uint m_team;

    // active Players in the area of the objective, 0 - alliance, 1 - horde
    private readonly HashSet<ObjectGuid>[] m_activePlayers = new HashSet<ObjectGuid>[SharedConst.PvpTeamsCount];

    // Capture point entry
    private uint m_capturePointEntry;

    // Gameobject related to that capture point
    private ObjectGuid m_capturePointGUID;

    // Maximum speed of capture
    private float m_maxSpeed;

    // Total shift needed to capture the objective
    private float m_maxValue;

    private float m_minValue;

    // Neutral value on capture bar
    private uint m_neutralValuePct;

    // Objective states
    private BattleFieldObjectiveStates m_OldState;

    private BattleFieldObjectiveStates m_State;

    // The status of the objective
    private float m_value;

    public BfCapturePoint(BattleField battlefield)
    {
        m_Bf = battlefield;
        m_capturePointGUID = ObjectGuid.Empty;
        m_team = TeamIds.Neutral;
        m_value = 0;
        m_minValue = 0.0f;
        m_maxValue = 0.0f;
        m_State = BattleFieldObjectiveStates.Neutral;
        m_OldState = BattleFieldObjectiveStates.Neutral;
        m_capturePointEntry = 0;
        m_neutralValuePct = 0;
        m_maxSpeed = 0;

        m_activePlayers[0] = new HashSet<ObjectGuid>();
        m_activePlayers[1] = new HashSet<ObjectGuid>();
    }

    public virtual void ChangeTeam(uint oldTeam) { }

    public uint GetCapturePointEntry()
    {
        return m_capturePointEntry;
    }

    public virtual bool HandlePlayerEnter(Player player)
    {
        if (!m_capturePointGUID.IsEmpty)
        {
            var capturePoint = m_Bf.GetGameObject(m_capturePointGUID);

            if (capturePoint)
            {
                player.SendUpdateWorldState(capturePoint.Template.ControlZone.worldState1, 1);
                player.SendUpdateWorldState(capturePoint.Template.ControlZone.worldstate2, (uint)(Math.Ceiling((m_value + m_maxValue) / (2 * m_maxValue) * 100.0f)));
                player.SendUpdateWorldState(capturePoint.Template.ControlZone.worldstate3, m_neutralValuePct);
            }
        }

        return m_activePlayers[player.TeamId].Add(player.GUID);
    }

    public virtual void HandlePlayerLeave(Player player)
    {
        if (!m_capturePointGUID.IsEmpty)
        {
            var capturePoint = m_Bf.GetGameObject(m_capturePointGUID);

            if (capturePoint)
                player.SendUpdateWorldState(capturePoint.Template.ControlZone.worldState1, 0);
        }

        m_activePlayers[player.TeamId].Remove(player.GUID);
    }

    public virtual void SendChangePhase()
    {
        if (m_capturePointGUID.IsEmpty)
            return;

        var capturePoint = m_Bf.GetGameObject(m_capturePointGUID);

        if (capturePoint)
        {
            // send this too, sometimes the slider disappears, dunno why :(
            SendUpdateWorldState(capturePoint.Template.ControlZone.worldState1, 1);
            // send these updates to only the ones in this objective
            SendUpdateWorldState(capturePoint.Template.ControlZone.worldstate2, (uint)Math.Ceiling((m_value + m_maxValue) / (2 * m_maxValue) * 100.0f));
            // send this too, sometimes it resets :S
            SendUpdateWorldState(capturePoint.Template.ControlZone.worldstate3, m_neutralValuePct);
        }
    }

    public bool SetCapturePointData(GameObject capturePoint)
    {
        Log.Logger.Error("Creating capture point {0}", capturePoint.Entry);

        m_capturePointGUID = capturePoint.GUID;
        m_capturePointEntry = capturePoint.Entry;

        // check info existence
        var goinfo = capturePoint.Template;

        if (goinfo.type != GameObjectTypes.ControlZone)
        {
            Log.Logger.Error("OutdoorPvP: GO {0} is not capture point!", capturePoint.Entry);

            return false;
        }

        // get the needed values from goinfo
        m_maxValue = goinfo.ControlZone.maxTime;
        m_maxSpeed = m_maxValue / (goinfo.ControlZone.minTime != 0 ? goinfo.ControlZone.minTime : 60);
        m_neutralValuePct = goinfo.ControlZone.neutralPercent;
        m_minValue = m_maxValue * goinfo.ControlZone.neutralPercent / 100;

        if (m_team == TeamIds.Alliance)
        {
            m_value = m_maxValue;
            m_State = BattleFieldObjectiveStates.Alliance;
        }
        else
        {
            m_value = -m_maxValue;
            m_State = BattleFieldObjectiveStates.Horde;
        }

        return true;
    }

    public virtual bool Update(uint diff)
    {
        if (m_capturePointGUID.IsEmpty)
            return false;

        var capturePoint = m_Bf.GetGameObject(m_capturePointGUID);

        if (capturePoint)
        {
            float radius = capturePoint.Template.ControlZone.radius;

            for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
                foreach (var guid in m_activePlayers[team])
                {
                    var player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                        if (!capturePoint.Location.IsWithinDistInMap(player, radius) || !player.IsOutdoorPvPActive())
                            HandlePlayerLeave(player);
                }

            List<Unit> players = new();
            var checker = new AnyPlayerInObjectRangeCheck(capturePoint, radius);
            var searcher = new PlayerListSearcher(capturePoint, players, checker);
            Cell.VisitGrid(capturePoint, searcher, radius);

            foreach (Player player in players)
                if (player.IsOutdoorPvPActive())
                    if (m_activePlayers[player.TeamId].Add(player.GUID))
                        HandlePlayerEnter(player);
        }

        // get the difference of numbers
        var fact_diff = ((float)m_activePlayers[TeamIds.Alliance].Count - m_activePlayers[TeamIds.Horde].Count) * diff / 1000;

        if (MathFunctions.fuzzyEq(fact_diff, 0.0f))
            return false;

        TeamFaction Challenger;
        var maxDiff = m_maxSpeed * diff;

        if (fact_diff < 0)
        {
            // horde is in majority, but it's already horde-controlled . no change
            if (m_State == BattleFieldObjectiveStates.Horde && m_value <= -m_maxValue)
                return false;

            if (fact_diff < -maxDiff)
                fact_diff = -maxDiff;

            Challenger = TeamFaction.Horde;
        }
        else
        {
            // ally is in majority, but it's already ally-controlled . no change
            if (m_State == BattleFieldObjectiveStates.Alliance && m_value >= m_maxValue)
                return false;

            if (fact_diff > maxDiff)
                fact_diff = maxDiff;

            Challenger = TeamFaction.Alliance;
        }

        var oldValue = m_value;
        var oldTeam = m_team;

        m_OldState = m_State;

        m_value += fact_diff;

        if (m_value < -m_minValue) // red
        {
            if (m_value < -m_maxValue)
                m_value = -m_maxValue;

            m_State = BattleFieldObjectiveStates.Horde;
            m_team = TeamIds.Horde;
        }
        else if (m_value > m_minValue) // blue
        {
            if (m_value > m_maxValue)
                m_value = m_maxValue;

            m_State = BattleFieldObjectiveStates.Alliance;
            m_team = TeamIds.Alliance;
        }
        else if (oldValue * m_value <= 0) // grey, go through mid point
        {
            // if challenger is ally, then n.a challenge
            if (Challenger == TeamFaction.Alliance)
                m_State = BattleFieldObjectiveStates.NeutralAllianceChallenge;
            // if challenger is horde, then n.h challenge
            else if (Challenger == TeamFaction.Horde)
                m_State = BattleFieldObjectiveStates.NeutralHordeChallenge;

            m_team = TeamIds.Neutral;
        }
        else // grey, did not go through mid point
        {
            // old phase and current are on the same side, so one team challenges the other
            if (Challenger == TeamFaction.Alliance && (m_OldState == BattleFieldObjectiveStates.Horde || m_OldState == BattleFieldObjectiveStates.NeutralHordeChallenge))
                m_State = BattleFieldObjectiveStates.HordeAllianceChallenge;
            else if (Challenger == TeamFaction.Horde && (m_OldState == BattleFieldObjectiveStates.Alliance || m_OldState == BattleFieldObjectiveStates.NeutralAllianceChallenge))
                m_State = BattleFieldObjectiveStates.AllianceHordeChallenge;

            m_team = TeamIds.Neutral;
        }

        if (MathFunctions.fuzzyNe(m_value, oldValue))
            SendChangePhase();

        if (m_OldState != m_State)
        {
            if (oldTeam != m_team)
                ChangeTeam(oldTeam);

            return true;
        }

        return false;
    }

    private bool DelCapturePoint()
    {
        if (!m_capturePointGUID.IsEmpty)
        {
            var capturePoint = m_Bf.GetGameObject(m_capturePointGUID);

            if (capturePoint)
            {
                capturePoint.SetRespawnTime(0); // not save respawn time
                capturePoint.Delete();
                capturePoint.Dispose();
            }

            m_capturePointGUID.Clear();
        }

        return true;
    }

    private GameObject GetCapturePointGo()
    {
        return m_Bf.GetGameObject(m_capturePointGUID);
    }

    private uint GetTeamId()
    {
        return m_team;
    }

    private bool IsInsideObjective(Player player)
    {
        return m_activePlayers[player.TeamId].Contains(player.GUID);
    }

    private void SendObjectiveComplete(uint id, ObjectGuid guid)
    {
        uint team;

        switch (m_State)
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
        foreach (var _guid in m_activePlayers[team])
        {
            var player = Global.ObjAccessor.FindPlayer(_guid);

            if (player)
                player.KilledMonsterCredit(id, guid);
        }
    }

    private void SendUpdateWorldState(uint field, uint value)
    {
        for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
            foreach (var guid in m_activePlayers[team]) // send to all players present in the area
            {
                var player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                    player.SendUpdateWorldState(field, value);
            }
    }
}