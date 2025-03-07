﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

class ResumeComms : ServerPacket
{
	public ResumeComms(ConnectionType connection) : base(ServerOpcodes.ResumeComms, connection) { }

	public override void Write() { }
}