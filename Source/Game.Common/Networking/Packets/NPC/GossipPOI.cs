// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Numerics;
using Framework.Constants;

namespace Game.Common.Networking.Packets.NPC;

public class GossipPOI : ServerPacket
{
	public uint Id;
	public uint Flags;
	public Vector3 Pos;
	public uint Icon;
	public uint Importance;
	public uint WMOGroupID;
	public string Name;
	public GossipPOI() : base(ServerOpcodes.GossipPoi) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(Id);
		_worldPacket.WriteVector3(Pos);
		_worldPacket.WriteUInt32(Icon);
		_worldPacket.WriteUInt32(Importance);
		_worldPacket.WriteUInt32(WMOGroupID);
		_worldPacket.WriteBits(Flags, 14);
		_worldPacket.WriteBits(Name.GetByteCount(), 6);
		_worldPacket.FlushBits();
		_worldPacket.WriteString(Name);
	}
}
