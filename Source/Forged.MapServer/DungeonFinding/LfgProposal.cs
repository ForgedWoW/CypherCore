// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LfgProposal
{
    public long CancelTime;
    public uint DungeonId;
    public uint Encounters;
    public ObjectGuid Group;
    public uint ID;
    public bool IsNew;
    public ObjectGuid Leader;
    public Dictionary<ObjectGuid, LfgProposalPlayer> Players = new();
    public List<ObjectGuid> Queues = new();
    public List<ulong> Showorder = new();

    public LfgProposalState State;
    // Players data

    public LfgProposal(uint dungeon = 0)
    {
        ID = 0;
        DungeonId = dungeon;
        State = LfgProposalState.Initiating;
        Group = ObjectGuid.Empty;
        Leader = ObjectGuid.Empty;
        CancelTime = 0;
        Encounters = 0;
        IsNew = true;
    }
}