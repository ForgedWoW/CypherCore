// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.MythicPlus;

public class MythicPlusRun
{
    public int MapChallengeModeID;
    public bool Completed;
    public uint Level;
    public int DurationMs;
    public long StartDate;
    public long CompletionDate;
    public int Season;
    public List<MythicPlusMember> Members = new();
    public float RunScore;
    public int[] KeystoneAffixIDs = new int[4];

    public void Write(WorldPacket data)
    {
        data.WriteInt32(MapChallengeModeID);
        data.WriteUInt32(Level);
        data.WriteInt32(DurationMs);
        data.WriteInt64(StartDate);
        data.WriteInt64(CompletionDate);
        data.WriteInt32(Season);

        foreach (var id in KeystoneAffixIDs)
            data.WriteInt32(id);

        data.WriteInt32(Members.Count);
        data.WriteFloat(RunScore);

        foreach (var member in Members)
            member.Write(data);

        data.WriteBit(Completed);
        data.FlushBits();
    }
}