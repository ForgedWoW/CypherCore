// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class AreaSpiritHealerTime : ServerPacket
{
	public ObjectGuid HealerGuid;
	public uint TimeLeft;
	public AreaSpiritHealerTime() : base(ServerOpcodes.AreaSpiritHealerTime) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(HealerGuid);
		_worldPacket.WriteUInt32(TimeLeft);
	}
}