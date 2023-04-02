// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Bpay;

public class BattlePayAckFailed : ServerPacket
{
    public BattlePayAckFailed() : base(ServerOpcodes.BattlePayAckFailed) { }

    public uint ClientToken { get; set; } = 0;
    public ulong PurchaseID { get; set; } = 0;
    public uint PurchaseResult { get; set; } = 0;
    public override void Write()
    {
        WorldPacket.Write(PurchaseID);
        WorldPacket.Write(PurchaseResult);
        WorldPacket.Write(ClientToken);
    }
}