// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class CorpseLocation : ServerPacket
{
	public ObjectGuid Player;
	public ObjectGuid Transport;
	public Vector3 Position;
	public int ActualMapID;
	public int MapID;
	public bool Valid;
	public CorpseLocation() : base(ServerOpcodes.CorpseLocation) { }

	public override void Write()
	{
		_worldPacket.WriteBit(Valid);
		_worldPacket.FlushBits();

		_worldPacket.WritePackedGuid(Player);
		_worldPacket.WriteInt32(ActualMapID);
		_worldPacket.WriteVector3(Position);
		_worldPacket.WriteInt32(MapID);
		_worldPacket.WritePackedGuid(Transport);
	}
}