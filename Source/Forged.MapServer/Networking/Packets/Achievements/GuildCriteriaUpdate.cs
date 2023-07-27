// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Achievements;

public class GuildCriteriaUpdate : ServerPacket
{
    public List<GuildCriteriaProgress> Progress = new();
    public GuildCriteriaUpdate() : base(ServerOpcodes.GuildCriteriaUpdate) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(Progress.Count);

        foreach (var progress in Progress)
        {
            WorldPacket.WriteUInt32(progress.CriteriaID);
            WorldPacket.WriteInt64(progress.DateCreated);
            WorldPacket.WriteInt64(progress.DateStarted);
            WorldPacket.WritePackedTime(progress.DateUpdated);
            WorldPacket.WriteUInt32(0); // this is a hack. this is a packed time written as int64 (progress.DateUpdated)
            WorldPacket.WriteUInt64(progress.Quantity);
            WorldPacket.WritePackedGuid(progress.PlayerGUID);
            WorldPacket.WriteInt32(progress.Unused_10_1_5);
            WorldPacket.WriteInt32(progress.Flags);
        }
    }
}