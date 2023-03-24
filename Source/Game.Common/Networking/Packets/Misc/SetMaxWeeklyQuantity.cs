// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Misc;

public class SetMaxWeeklyQuantity : ServerPacket
{
	public uint MaxWeeklyQuantity;
	public uint Type;
	public SetMaxWeeklyQuantity() : base(ServerOpcodes.SetMaxWeeklyQuantity, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(Type);
		_worldPacket.WriteUInt32(MaxWeeklyQuantity);
	}
}
