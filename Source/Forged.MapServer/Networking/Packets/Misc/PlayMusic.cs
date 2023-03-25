// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

public class PlayMusic : ServerPacket
{
	readonly uint SoundKitID;

	public PlayMusic(uint soundKitID) : base(ServerOpcodes.PlayMusic)
	{
		SoundKitID = soundKitID;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(SoundKitID);
	}
}