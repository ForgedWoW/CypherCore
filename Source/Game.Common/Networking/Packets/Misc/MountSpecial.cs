// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Misc;

public class MountSpecial : ClientPacket
{
	public int[] SpellVisualKitIDs;
	public int SequenceVariation;
	public MountSpecial(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		SpellVisualKitIDs = new int[_worldPacket.ReadUInt32()];
		SequenceVariation = _worldPacket.ReadInt32();

		for (var i = 0; i < SpellVisualKitIDs.Length; ++i)
			SpellVisualKitIDs[i] = _worldPacket.ReadInt32();
	}
}
