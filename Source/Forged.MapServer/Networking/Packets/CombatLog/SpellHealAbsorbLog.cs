// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class SpellHealAbsorbLog : ServerPacket
{
	public ObjectGuid Healer;
	public ObjectGuid Target;
	public ObjectGuid AbsorbCaster;
	public int AbsorbSpellID;
	public int AbsorbedSpellID;
	public int Absorbed;
	public int OriginalHeal;
	public ContentTuningParams ContentTuning;
	public SpellHealAbsorbLog() : base(ServerOpcodes.SpellHealAbsorbLog, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Target);
		_worldPacket.WritePackedGuid(AbsorbCaster);
		_worldPacket.WritePackedGuid(Healer);
		_worldPacket.WriteInt32(AbsorbSpellID);
		_worldPacket.WriteInt32(AbsorbedSpellID);
		_worldPacket.WriteInt32(Absorbed);
		_worldPacket.WriteInt32(OriginalHeal);
		_worldPacket.WriteBit(ContentTuning != null);
		_worldPacket.FlushBits();

		if (ContentTuning != null)
			ContentTuning.Write(_worldPacket);
	}
}