// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Achievements;

internal class AllAccountCriteria : ServerPacket
{
    public List<CriteriaProgressPkt> Progress = new();
    public AllAccountCriteria() : base(ServerOpcodes.AllAccountCriteria, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(Progress.Count);

        foreach (var progress in Progress)
            progress.Write(WorldPacket);
    }
}