﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.DungeonFinding;

public class LFGPlayerData
{
	// General
	RideTicket m_Ticket;
	LfgState m_State;

	LfgState m_OldState;

	// Player
	TeamFaction m_Team;
	ObjectGuid m_Group;

	// Queue
	LfgRoles m_Roles;
	List<uint> m_SelectedDungeons = new();

	// Achievement-related
	byte m_NumberOfPartyMembersAtJoin;

	public LFGPlayerData()
	{
		m_State = LfgState.None;
		m_OldState = LfgState.None;
	}

	public void SetTicket(RideTicket ticket)
	{
		m_Ticket = ticket;
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

	public void RestoreState()
	{
		if (m_OldState == LfgState.None)
		{
			m_SelectedDungeons.Clear();
			m_Roles = 0;
		}

		m_State = m_OldState;
	}

	public void SetTeam(TeamFaction team)
	{
		m_Team = team;
	}

	public void SetGroup(ObjectGuid group)
	{
		m_Group = group;
	}

	public void SetRoles(LfgRoles roles)
	{
		m_Roles = roles;
	}

	public void SetSelectedDungeons(List<uint> dungeons)
	{
		m_SelectedDungeons = dungeons;
	}

	public RideTicket GetTicket()
	{
		return m_Ticket;
	}

	public LfgState GetState()
	{
		return m_State;
	}

	public LfgState GetOldState()
	{
		return m_OldState;
	}

	public TeamFaction GetTeam()
	{
		return m_Team;
	}

	public ObjectGuid GetGroup()
	{
		return m_Group;
	}

	public LfgRoles GetRoles()
	{
		return m_Roles;
	}

	public List<uint> GetSelectedDungeons()
	{
		return m_SelectedDungeons;
	}

	public void SetNumberOfPartyMembersAtJoin(byte count)
	{
		m_NumberOfPartyMembersAtJoin = count;
	}

	public byte GetNumberOfPartyMembersAtJoin()
	{
		return m_NumberOfPartyMembersAtJoin;
	}
}