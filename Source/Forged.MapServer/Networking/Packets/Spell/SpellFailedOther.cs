// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class SpellFailedOther : ServerPacket
{
	public ObjectGuid CasterUnit;
	public uint SpellID;
	public SpellCastVisual Visual;
	public ushort Reason;
	public ObjectGuid CastID;
	public SpellFailedOther() : base(ServerOpcodes.SpellFailedOther, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(CasterUnit);
		_worldPacket.WritePackedGuid(CastID);
		_worldPacket.WriteUInt32(SpellID);

		Visual.Write(_worldPacket);

		_worldPacket.WriteUInt16(Reason);
	}
}