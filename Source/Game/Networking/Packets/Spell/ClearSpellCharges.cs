// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class ClearSpellCharges : ServerPacket
{
	public bool IsPet;
	public uint Category;
	public ClearSpellCharges() : base(ServerOpcodes.ClearSpellCharges, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(Category);
		_worldPacket.WriteBit(IsPet);
		_worldPacket.FlushBits();
	}
}