// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LFGGroupData
{
    private readonly List<ObjectGuid> _mPlayers = new();

    // Dungeon
    private uint _mDungeon;

    // Vote Kick
    private byte _mKicksLeft;

    private ObjectGuid _mLeader;

    private LfgState _mOldState;

    // General
    private LfgState _mState;
    private bool _mVoteKickActive;

    public LFGGroupData()
    {
        _mState = LfgState.None;
        _mOldState = LfgState.None;
        _mKicksLeft = SharedConst.LFGMaxKicks;
    }

    public void AddPlayer(ObjectGuid guid)
    {
        _mPlayers.Add(guid);
    }

    public void DecreaseKicksLeft()
    {
        if (_mKicksLeft != 0)
            --_mKicksLeft;
    }

    public uint GetDungeon(bool asId = true)
    {
        if (asId)
            return _mDungeon & 0x00FFFFFF;
        else
            return _mDungeon;
    }

    public byte GetKicksLeft()
    {
        return _mKicksLeft;
    }

    public ObjectGuid GetLeader()
    {
        return _mLeader;
    }

    public LfgState GetOldState()
    {
        return _mOldState;
    }

    public byte GetPlayerCount()
    {
        return (byte)_mPlayers.Count;
    }

    public List<ObjectGuid> GetPlayers()
    {
        return _mPlayers;
    }

    public LfgState GetState()
    {
        return _mState;
    }

    public bool IsLfgGroup()
    {
        return _mOldState != LfgState.None;
    }

    public bool IsVoteKickActive()
    {
        return _mVoteKickActive;
    }

    public void RemoveAllPlayers()
    {
        _mPlayers.Clear();
    }

    public byte RemovePlayer(ObjectGuid guid)
    {
        _mPlayers.Remove(guid);

        return (byte)_mPlayers.Count;
    }

    public void RestoreState()
    {
        _mState = _mOldState;
    }

    public void SetDungeon(uint dungeon)
    {
        _mDungeon = dungeon;
    }

    public void SetLeader(ObjectGuid guid)
    {
        _mLeader = guid;
    }

    public void SetState(LfgState state)
    {
        switch (state)
        {
            case LfgState.None:
                _mDungeon = 0;
                _mKicksLeft = SharedConst.LFGMaxKicks;
                _mOldState = state;

                break;
            case LfgState.FinishedDungeon:
            case LfgState.Dungeon:
                _mOldState = state;

                break;
        }

        _mState = state;
    }

    public void SetVoteKick(bool active)
    {
        _mVoteKickActive = active;
    }
}