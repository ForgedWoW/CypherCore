// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public class GuildBankSetTabText : ClientPacket
{
	public int Tab;
	public string TabText;
	public GuildBankSetTabText(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Tab = _worldPacket.ReadInt32();
		TabText = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(14));
	}
}