// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LFGGroupData
{
    private readonly List<ObjectGuid> m_Players = new();

    // General
    private LfgState m_State;
    private LfgState m_OldState;

    private ObjectGuid m_Leader;

    // Dungeon
    private uint m_Dungeon;

    // Vote Kick
    private byte m_KicksLeft;
    private bool m_VoteKickActive;

    public LFGGroupData()
    {
        m_State = LfgState.None;
        m_OldState = LfgState.None;
        m_KicksLeft = SharedConst.LFGMaxKicks;
    }

    public bool IsLfgGroup()
    {
        return m_OldState != LfgState.None;
    }

    public void SetState(LfgState state)
    {
        switch (state)
        {
            case LfgState.None:
                m_Dungeon = 0;
                m_KicksLeft = SharedConst.LFGMaxKicks;
                m_OldState = state;

                break;
            case LfgState.FinishedDungeon:
            case LfgState.Dungeon:
                m_OldState = state;

                break;
        }

        m_State = state;
    }

    public void RestoreState()
    {
        m_State = m_OldState;
    }

    public void AddPlayer(ObjectGuid guid)
    {
        m_Players.Add(guid);
    }

    public byte RemovePlayer(ObjectGuid guid)
    {
        m_Players.Remove(guid);

        return (byte)m_Players.Count;
    }

    public void RemoveAllPlayers()
    {
        m_Players.Clear();
    }

    public void SetLeader(ObjectGuid guid)
    {
        m_Leader = guid;
    }

    public void SetDungeon(uint dungeon)
    {
        m_Dungeon = dungeon;
    }

    public void DecreaseKicksLeft()
    {
        if (m_KicksLeft != 0)
            --m_KicksLeft;
    }

    public LfgState GetState()
    {
        return m_State;
    }

    public LfgState GetOldState()
    {
        return m_OldState;
    }

    public List<ObjectGuid> GetPlayers()
    {
        return m_Players;
    }

    public byte GetPlayerCount()
    {
        return (byte)m_Players.Count;
    }

    public ObjectGuid GetLeader()
    {
        return m_Leader;
    }

    public uint GetDungeon(bool asId = true)
    {
        if (asId)
            return (m_Dungeon & 0x00FFFFFF);
        else
            return m_Dungeon;
    }

    public byte GetKicksLeft()
    {
        return m_KicksLeft;
    }

    public void SetVoteKick(bool active)
    {
        m_VoteKickActive = active;
    }

    public bool IsVoteKickActive()
    {
        return m_VoteKickActive;
    }
}