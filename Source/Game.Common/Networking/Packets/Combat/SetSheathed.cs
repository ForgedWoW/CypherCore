// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Combat;

public class SetSheathed : ClientPacket
{
	public int CurrentSheathState;
	public bool Animate = true;
	public SetSheathed(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		CurrentSheathState = _worldPacket.ReadInt32();
		Animate = _worldPacket.HasBit();
	}
}
