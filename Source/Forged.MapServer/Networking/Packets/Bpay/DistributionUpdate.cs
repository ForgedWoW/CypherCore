// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Bpay;

public class DistributionUpdate : ServerPacket
{
    public DistributionUpdate() : base(ServerOpcodes.BattlePayDistributionUpdate) { }

    public BpayDistributionObject DistributionObject { get; set; } = new();
    public override void Write()
    {
        DistributionObject.Write(WorldPacket);
    }
}