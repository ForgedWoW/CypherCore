// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Trade;

public class ClearTradeItem : ClientPacket
{
	public byte TradeSlot;
	public ClearTradeItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		TradeSlot = _worldPacket.ReadUInt8();
	}
}
