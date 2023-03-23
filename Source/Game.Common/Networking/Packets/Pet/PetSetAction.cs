// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Pet;

public class PetSetAction : ClientPacket
{
	public ObjectGuid PetGUID;
	public uint Index;
	public uint Action;
	public PetSetAction(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGUID = _worldPacket.ReadPackedGuid();

		Index = _worldPacket.ReadUInt32();
		Action = _worldPacket.ReadUInt32();
	}
}
