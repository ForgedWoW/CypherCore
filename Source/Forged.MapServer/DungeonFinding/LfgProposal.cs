// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LfgProposal
{
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

    public long CancelTime { get; set; }
    public uint DungeonId { get; set; }
    public uint Encounters { get; set; }
    public ObjectGuid Group { get; set; }
    public uint ID { get; set; }
    public bool IsNew { get; set; }
    public ObjectGuid Leader { get; set; }
    public Dictionary<ObjectGuid, LfgProposalPlayer> Players { get; set; } = new();
    public List<ObjectGuid> Queues { get; set; } = new();
    public List<ulong> Showorder { get; set; } = new();
}