// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class MergeGuildBankItemWithGuildBankItem : ClientPacket
{
	public ObjectGuid Banker;
	public byte BankTab;
	public byte BankSlot;
	public byte BankTab1;
	public byte BankSlot1;
	public uint StackCount;
	public MergeGuildBankItemWithGuildBankItem(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Banker = _worldPacket.ReadPackedGuid();
		BankTab = _worldPacket.ReadUInt8();
		BankSlot = _worldPacket.ReadUInt8();
		BankTab1 = _worldPacket.ReadUInt8();
		BankSlot1 = _worldPacket.ReadUInt8();
		StackCount = _worldPacket.ReadUInt32();
	}
}