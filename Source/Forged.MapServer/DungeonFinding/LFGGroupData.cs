// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LFGGroupData
{
    private uint _dungeon;

    public LFGGroupData()
    {
        State = LfgState.None;
        OldState = LfgState.None;
        KicksLeft = SharedConst.LFGMaxKicks;
    }

    public bool IsLfgGroup => OldState != LfgState.None;
    public bool IsVoteKickActive { get; set; }
    public byte KicksLeft { get; set; }

    public ObjectGuid Leader { get; set; }

    public LfgState OldState { get; set; }

    public byte PlayerCount => (byte)Players.Count;

    public List<ObjectGuid> Players { get; } = new();

    public LfgState State { get; set; }

    public void DecreaseKicksLeft()
    {
        if (KicksLeft != 0)
            --KicksLeft;
    }

    public uint GetDungeon(bool asId = true)
    {
        if (asId)
            return _dungeon & 0x00FFFFFF;

        return _dungeon;
    }

    public void RemoveAllPlayers()
    {
        Players.Clear();
    }

    public byte RemovePlayer(ObjectGuid guid)
    {
        Players.Remove(guid);

        return (byte)Players.Count;
    }

    public void RestoreState()
    {
        State = OldState;
    }

    public void SetDungeon(uint dungeon)
    {
        _dungeon = dungeon;
    }

    public void SetState(LfgState state)
    {
        switch (state)
        {
            case LfgState.None:
                _dungeon = 0;
                KicksLeft = SharedConst.LFGMaxKicks;
                OldState = state;

                break;

            case LfgState.FinishedDungeon:
            case LfgState.Dungeon:
                OldState = state;

                break;
        }

        State = state;
    }
}