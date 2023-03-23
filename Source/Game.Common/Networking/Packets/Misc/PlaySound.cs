// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Misc;

public class PlaySound : ServerPacket
{
	public ObjectGuid SourceObjectGuid;
	public uint SoundKitID;
	public uint BroadcastTextID;

	public PlaySound(ObjectGuid sourceObjectGuid, uint soundKitID, uint broadcastTextId) : base(ServerOpcodes.PlaySound)
	{
		SourceObjectGuid = sourceObjectGuid;
		SoundKitID = soundKitID;
		BroadcastTextID = broadcastTextId;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt32(SoundKitID);
		_worldPacket.WritePackedGuid(SourceObjectGuid);
		_worldPacket.WriteUInt32(BroadcastTextID);
	}
}
