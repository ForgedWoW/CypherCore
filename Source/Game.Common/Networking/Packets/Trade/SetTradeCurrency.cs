// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Trade;

public class SetTradeCurrency : ClientPacket
{
	public uint Type;
	public uint Quantity;
	public SetTradeCurrency(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Type = _worldPacket.ReadUInt32();
		Quantity = _worldPacket.ReadUInt32();
	}
}
