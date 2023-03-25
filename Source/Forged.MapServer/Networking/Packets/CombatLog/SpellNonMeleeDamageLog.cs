// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class SpellNonMeleeDamageLog : CombatLogServerPacket
{
	public ObjectGuid Me;
	public ObjectGuid CasterGUID;
	public ObjectGuid CastID;
	public int SpellID;
	public SpellCastVisual Visual;
	public int Damage;
	public int OriginalDamage;
	public int Overkill = -1;
	public byte SchoolMask;
	public int ShieldBlock;
	public int Resisted;
	public bool Periodic;
	public int Absorbed;

	public int Flags;

	// Optional<SpellNonMeleeDamageLogDebugInfo> DebugInfo;
	public ContentTuningParams ContentTuning;
	public SpellNonMeleeDamageLog() : base(ServerOpcodes.SpellNonMeleeDamageLog, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Me);
		_worldPacket.WritePackedGuid(CasterGUID);
		_worldPacket.WritePackedGuid(CastID);
		_worldPacket.WriteInt32(SpellID);
		Visual.Write(_worldPacket);
		_worldPacket.WriteInt32(Damage);
		_worldPacket.WriteInt32(OriginalDamage);
		_worldPacket.WriteInt32(Overkill);
		_worldPacket.WriteUInt8(SchoolMask);
		_worldPacket.WriteInt32(Absorbed);
		_worldPacket.WriteInt32(Resisted);
		_worldPacket.WriteInt32(ShieldBlock);

		_worldPacket.WriteBit(Periodic);
		_worldPacket.WriteBits(Flags, 7);
		_worldPacket.WriteBit(false); // Debug info
		WriteLogDataBit();
		_worldPacket.WriteBit(ContentTuning != null);
		FlushBits();
		WriteLogData();

		if (ContentTuning != null)
			ContentTuning.Write(_worldPacket);
	}
}