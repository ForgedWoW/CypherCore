﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Battlenet;

public class BattlenetRequest : ClientPacket
{
	public MethodCall Method;
	public byte[] Data;
	public BattlenetRequest(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Method.Read(_worldPacket);
		var protoSize = _worldPacket.ReadUInt32();

		Data = _worldPacket.ReadBytes(protoSize);
	}
}
