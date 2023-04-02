// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Bpay;

public class BattlePayDeliveryStarted : ServerPacket
{
    public BattlePayDeliveryStarted() : base(ServerOpcodes.BattlePayDeliveryStarted) { }

    public ulong DistributionID { get; set; } = 0;
    public override void Write()
    {
        _worldPacket.Write(DistributionID);
    }
}