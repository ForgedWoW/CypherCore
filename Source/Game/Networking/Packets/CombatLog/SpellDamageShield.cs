// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class SpellDamageShield : CombatLogServerPacket
{
	public ObjectGuid Attacker;
	public ObjectGuid Defender;
	public uint SpellID;
	public uint TotalDamage;
	public int OriginalDamage;
	public uint OverKill;
	public uint SchoolMask;
	public uint LogAbsorbed;
	public SpellDamageShield() : base(ServerOpcodes.SpellDamageShield, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Attacker);
		_worldPacket.WritePackedGuid(Defender);
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteUInt32(TotalDamage);
		_worldPacket.WriteInt32(OriginalDamage);
		_worldPacket.WriteUInt32(OverKill);
		_worldPacket.WriteUInt32(SchoolMask);
		_worldPacket.WriteUInt32(LogAbsorbed);

		WriteLogDataBit();
		FlushBits();
		WriteLogData();
	}
}