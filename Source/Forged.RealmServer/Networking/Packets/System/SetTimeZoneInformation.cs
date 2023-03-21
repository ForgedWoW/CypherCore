// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

public class SetTimeZoneInformation : ServerPacket
{
	public string ServerTimeTZ;
	public string GameTimeTZ;
	public string ServerRegionalTZ;
	public SetTimeZoneInformation() : base(ServerOpcodes.SetTimeZoneInformation) { }

	public override void Write()
	{
		_worldPacket.WriteBits(ServerTimeTZ.GetByteCount(), 7);
		_worldPacket.WriteBits(GameTimeTZ.GetByteCount(), 7);
		_worldPacket.WriteBits(ServerRegionalTZ.GetByteCount(), 7);
		_worldPacket.FlushBits();

		_worldPacket.WriteString(ServerTimeTZ);
		_worldPacket.WriteString(GameTimeTZ);
		_worldPacket.WriteString(ServerRegionalTZ);
	}
}