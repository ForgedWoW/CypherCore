// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public class SetActionBarToggles : ClientPacket
{
	public byte Mask;
	public SetActionBarToggles(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Mask = _worldPacket.ReadUInt8();
	}
}