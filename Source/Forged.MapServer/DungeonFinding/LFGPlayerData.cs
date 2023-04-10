// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.LFG;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LFGPlayerData
{
    private ObjectGuid _mGroup;

    // Achievement-related
    private byte _mNumberOfPartyMembersAtJoin;

    private LfgState _mOldState;

    // Queue
    private LfgRoles _mRoles;

    private List<uint> _mSelectedDungeons = new();

    private LfgState _mState;

    // Player
    private TeamFaction _mTeam;

    // General
    private RideTicket _mTicket;

    public LFGPlayerData()
    {
        _mState = LfgState.None;
        _mOldState = LfgState.None;
    }

    public ObjectGuid GetGroup()
    {
        return _mGroup;
    }

    public byte GetNumberOfPartyMembersAtJoin()
    {
        return _mNumberOfPartyMembersAtJoin;
    }

    public LfgState GetOldState()
    {
        return _mOldState;
    }

    public LfgRoles GetRoles()
    {
        return _mRoles;
    }

    public List<uint> GetSelectedDungeons()
    {
        return _mSelectedDungeons;
    }

    public LfgState GetState()
    {
        return _mState;
    }

    public TeamFaction GetTeam()
    {
        return _mTeam;
    }

    public RideTicket GetTicket()
    {
        return _mTicket;
    }

    public void RestoreState()
    {
        if (_mOldState == LfgState.None)
        {
            _mSelectedDungeons.Clear();
            _mRoles = 0;
        }

        _mState = _mOldState;
    }

    public void SetGroup(ObjectGuid group)
    {
        _mGroup = group;
    }

    public void SetNumberOfPartyMembersAtJoin(byte count)
    {
        _mNumberOfPartyMembersAtJoin = count;
    }

    public void SetRoles(LfgRoles roles)
    {
        _mRoles = roles;
    }

    public void SetSelectedDungeons(List<uint> dungeons)
    {
        _mSelectedDungeons = dungeons;
    }

    public void SetState(LfgState state)
    {
        switch (state)
        {
            case LfgState.None:
            case LfgState.FinishedDungeon:
                _mRoles = 0;
                _mSelectedDungeons.Clear();
                goto case LfgState.Dungeon;
            case LfgState.Dungeon:
                _mOldState = state;

                break;
        }

        _mState = state;
    }

    public void SetTeam(TeamFaction team)
    {
        _mTeam = team;
    }

    public void SetTicket(RideTicket ticket)
    {
        _mTicket = ticket;
    }
}