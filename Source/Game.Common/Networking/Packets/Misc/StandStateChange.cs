// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Misc;

public class StandStateChange : ClientPacket
{
	public UnitStandStateType StandState;
	public StandStateChange(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		StandState = (UnitStandStateType)_worldPacket.ReadUInt32();
	}
}
