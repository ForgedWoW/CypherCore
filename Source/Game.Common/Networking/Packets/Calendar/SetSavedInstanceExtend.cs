// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Calendar;

public class SetSavedInstanceExtend : ClientPacket
{
	public int MapID;
	public bool Extend;
	public uint DifficultyID;
	public SetSavedInstanceExtend(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		MapID = _worldPacket.ReadInt32();
		DifficultyID = _worldPacket.ReadUInt32();
		Extend = _worldPacket.HasBit();
	}
}
