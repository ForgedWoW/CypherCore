// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Spell;

public class CooldownEvent : ServerPacket
{
	public bool IsPet;
	public uint SpellID;

	public CooldownEvent(bool isPet, uint spellId) : base(ServerOpcodes.CooldownEvent, ConnectionType.Instance)
	{
		IsPet = isPet;
		SpellID = spellId;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteBit(IsPet);
		_worldPacket.FlushBits();
	}
}
