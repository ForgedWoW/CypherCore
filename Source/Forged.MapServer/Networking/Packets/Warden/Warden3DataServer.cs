// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.IO;

namespace Forged.MapServer.Networking.Packets.Warden;

internal class Warden3DataServer : ServerPacket
{
	public ByteBuffer Data;
	public Warden3DataServer() : base(ServerOpcodes.Warden3Data) { }

	public override void Write()
	{
		_worldPacket.WriteBytes(Data);
	}
}