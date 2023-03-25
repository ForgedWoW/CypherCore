// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Query;

public class CorpseTransportQuery : ServerPacket
{
	public ObjectGuid Player;
	public Vector3 Position;
	public float Facing;
	public CorpseTransportQuery() : base(ServerOpcodes.CorpseTransportQuery) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Player);
		_worldPacket.WriteVector3(Position);
		_worldPacket.WriteFloat(Facing);
	}
}