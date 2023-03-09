// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class PlaySpeakerBoxSound : ServerPacket
{
	public ObjectGuid SourceObjectGUID;
	public uint SoundKitID;

	public PlaySpeakerBoxSound(ObjectGuid sourceObjectGuid, uint soundKitID) : base(ServerOpcodes.PlaySpeakerbotSound)
	{
		SourceObjectGUID = sourceObjectGuid;
		SoundKitID = soundKitID;
	}

	public override void Write()
	{
		_worldPacket.WritePackedGuid(SourceObjectGUID);
		_worldPacket.WriteUInt32(SoundKitID);
	}
}