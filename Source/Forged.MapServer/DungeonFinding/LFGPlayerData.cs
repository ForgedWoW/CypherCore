// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.LFG;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LFGPlayerData
{
    private ObjectGuid m_Group;

    // Achievement-related
    private byte m_NumberOfPartyMembersAtJoin;

    private LfgState m_OldState;

    // Queue
    private LfgRoles m_Roles;

    private List<uint> m_SelectedDungeons = new();

    private LfgState m_State;

    // Player
    private TeamFaction m_Team;

    // General
    private RideTicket m_Ticket;

    public LFGPlayerData()
    {
        m_State = LfgState.None;
        m_OldState = LfgState.None;
    }

    public ObjectGuid GetGroup()
    {
        return m_Group;
    }

    public byte GetNumberOfPartyMembersAtJoin()
    {
        return m_NumberOfPartyMembersAtJoin;
    }

    public LfgState GetOldState()
    {
        return m_OldState;
    }

    public LfgRoles GetRoles()
    {
        return m_Roles;
    }

    public List<uint> GetSelectedDungeons()
    {
        return m_SelectedDungeons;
    }

    public LfgState GetState()
    {
        return m_State;
    }

    public TeamFaction GetTeam()
    {
        return m_Team;
    }

    public RideTicket GetTicket()
    {
        return m_Ticket;
    }

    public void RestoreState()
    {
        if (m_OldState == LfgState.None)
        {
            m_SelectedDungeons.Clear();
            m_Roles = 0;
        }

        m_State = m_OldState;
    }

    public void SetGroup(ObjectGuid group)
    {
        m_Group = group;
    }

    public void SetNumberOfPartyMembersAtJoin(byte count)
    {
        m_NumberOfPartyMembersAtJoin = count;
    }

    public void SetRoles(LfgRoles roles)
    {
        m_Roles = roles;
    }

    public void SetSelectedDungeons(List<uint> dungeons)
    {
        m_SelectedDungeons = dungeons;
    }

    public void SetState(LfgState state)
    {
        switch (state)
        {
            case LfgState.None:
            case LfgState.FinishedDungeon:
                m_Roles = 0;
                m_SelectedDungeons.Clear();
                goto case LfgState.Dungeon;
            case LfgState.Dungeon:
                m_OldState = state;

                break;
        }

        m_State = state;
    }

    public void SetTeam(TeamFaction team)
    {
        m_Team = team;
    }

    public void SetTicket(RideTicket ticket)
    {
        m_Ticket = ticket;
    }
}