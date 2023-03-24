// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.NPC;

// CMSG_BANKER_ACTIVATE
// CMSG_BINDER_ACTIVATE
// CMSG_BINDER_CONFIRM
// CMSG_GOSSIP_HELLO
// CMSG_LIST_INVENTORY
// CMSG_TRAINER_LIST
// CMSG_BATTLEMASTER_HELLO
public class Hello : ClientPacket
{
	public ObjectGuid Unit;
	public Hello(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Unit = _worldPacket.ReadPackedGuid();
	}
}

//Structs