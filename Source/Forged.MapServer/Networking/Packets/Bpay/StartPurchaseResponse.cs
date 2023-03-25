// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Bpay;

public class StartPurchaseResponse : ServerPacket
{
	public ulong PurchaseID { get; set; } = 0;
	public uint ClientToken { get; set; } = 0;
	public uint PurchaseResult { get; set; } = 0;

	public StartPurchaseResponse() : base(ServerOpcodes.BattlePayStartPurchaseResponse) { }

	public override void Write()
	{
		_worldPacket.Write(PurchaseID);
		_worldPacket.Write(PurchaseResult);
		_worldPacket.Write(ClientToken);
	}
}