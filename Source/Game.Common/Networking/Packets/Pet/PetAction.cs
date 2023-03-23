// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Pet;

public class PetAction : ClientPacket
{
	public ObjectGuid PetGUID;
	public uint Action;
	public ObjectGuid TargetGUID;
	public Vector3 ActionPosition;
	public PetAction(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PetGUID = _worldPacket.ReadPackedGuid();

		Action = _worldPacket.ReadUInt32();
		TargetGUID = _worldPacket.ReadPackedGuid();

		ActionPosition = _worldPacket.ReadVector3();
	}
}
