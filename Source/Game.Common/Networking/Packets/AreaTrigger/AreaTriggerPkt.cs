// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.AreaTrigger;

public class AreaTriggerPkt : ClientPacket
{
	public uint AreaTriggerID;
	public bool Entered;
	public bool FromClient;
	public AreaTriggerPkt(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		AreaTriggerID = _worldPacket.ReadUInt32();
		Entered = _worldPacket.HasBit();
		FromClient = _worldPacket.HasBit();
	}
}

//Structs