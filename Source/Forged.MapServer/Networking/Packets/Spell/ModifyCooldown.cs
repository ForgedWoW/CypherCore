// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class ModifyCooldown : ServerPacket
{
	public bool IsPet;
	public bool WithoutCategoryCooldown;
	public int DeltaTime;
	public uint SpellID;
	public ModifyCooldown() : base(ServerOpcodes.ModifyCooldown, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteInt32(DeltaTime);
		_worldPacket.WriteBit(IsPet);
		_worldPacket.WriteBit(WithoutCategoryCooldown);
		_worldPacket.FlushBits();
	}
}