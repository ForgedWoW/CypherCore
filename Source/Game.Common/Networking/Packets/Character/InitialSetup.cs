﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class InitialSetup : ServerPacket
{
	public byte ServerExpansionTier;
	public byte ServerExpansionLevel;
	public InitialSetup() : base(ServerOpcodes.InitialSetup, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8(ServerExpansionLevel);
		_worldPacket.WriteUInt8(ServerExpansionTier);
	}
}