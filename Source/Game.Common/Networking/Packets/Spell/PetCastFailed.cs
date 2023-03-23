// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking.Packets.Spell;

namespace Game.Common.Networking.Packets.Spell;

public class PetCastFailed : CastFailedBase
{
	public PetCastFailed() : base(ServerOpcodes.PetCastFailed, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(CastID);
		_worldPacket.WriteInt32(SpellID);
		_worldPacket.WriteInt32((int)Reason);
		_worldPacket.WriteInt32(FailedArg1);
		_worldPacket.WriteInt32(FailedArg2);
	}
}
