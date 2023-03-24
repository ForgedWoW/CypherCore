// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Trait;

public class ClassTalentsSetStarterBuildActive : ClientPacket
{
	public int ConfigID;
	public bool Active;

	public ClassTalentsSetStarterBuildActive(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ConfigID = _worldPacket.ReadInt32();
		Active = _worldPacket.HasBit();
	}
}
