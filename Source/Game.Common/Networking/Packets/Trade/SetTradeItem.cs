// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Trade;

public class SetTradeItem : ClientPacket
{
	public byte TradeSlot;
	public byte PackSlot;
	public byte ItemSlotInPack;
	public SetTradeItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		TradeSlot = _worldPacket.ReadUInt8();
		PackSlot = _worldPacket.ReadUInt8();
		ItemSlotInPack = _worldPacket.ReadUInt8();
	}
}
