// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class GuildBankUpdateTab : ClientPacket
{
	public ObjectGuid Banker;
	public byte BankTab;
	public string Name;
	public string Icon;
	public GuildBankUpdateTab(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		BankTab = _worldPacket.ReadUInt8();

		_worldPacket.ResetBitPos();
		var nameLen = _worldPacket.ReadBits<uint>(7);
		var iconLen = _worldPacket.ReadBits<uint>(9);

		Name = _worldPacket.ReadString(nameLen);
		Icon = _worldPacket.ReadString(iconLen);
	}
}