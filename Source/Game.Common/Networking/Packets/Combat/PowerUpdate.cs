// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Combat;

public class PowerUpdate : ServerPacket
{
	public ObjectGuid Guid;
	public List<PowerUpdatePower> Powers;

	public PowerUpdate() : base(ServerOpcodes.PowerUpdate)
	{
		Powers = new List<PowerUpdatePower>();
	}

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
		_worldPacket.WriteInt32(Powers.Count);

		foreach (var power in Powers)
		{
			_worldPacket.WriteInt32(power.Power);
			_worldPacket.WriteUInt8(power.PowerType);
		}
	}
}
