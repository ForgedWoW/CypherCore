// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Instance;

internal class InstanceInfoPkt : ServerPacket
{
    public List<InstanceLockPkt> LockList = new();
    public InstanceInfoPkt() : base(ServerOpcodes.InstanceInfo) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(LockList.Count);

        foreach (var lockInfos in LockList)
            lockInfos.Write(WorldPacket);
    }
}