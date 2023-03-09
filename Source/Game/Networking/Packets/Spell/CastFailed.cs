// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

class CastFailed : CastFailedBase
{
	public SpellCastVisual Visual;

	public CastFailed() : base(ServerOpcodes.CastFailed, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(CastID);
		_worldPacket.WriteInt32(SpellID);

		Visual.Write(_worldPacket);

		_worldPacket.WriteInt32((int)Reason);
		_worldPacket.WriteInt32(FailedArg1);
		_worldPacket.WriteInt32(FailedArg2);
	}
}