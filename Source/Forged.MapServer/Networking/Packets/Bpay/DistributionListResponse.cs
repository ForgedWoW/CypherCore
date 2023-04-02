// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Bpay;

public class DistributionListResponse : ServerPacket
{
    public DistributionListResponse() : base(ServerOpcodes.BattlePayGetDistributionListResponse) { }

    public List<BpayDistributionObject> DistributionObject { get; set; } = new();
    public uint Result { get; set; } = 0;
    public override void Write()
    {
        _worldPacket.Write(Result);
        _worldPacket.WriteBits((uint)DistributionObject.Count, 11);

        foreach (var objectData in DistributionObject)
            objectData.Write(_worldPacket);
    }
}