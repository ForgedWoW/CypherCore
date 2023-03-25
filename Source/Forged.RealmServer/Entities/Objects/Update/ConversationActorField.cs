// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Networking;

namespace Forged.RealmServer.Entities;

public class ConversationActorField
{
	public uint CreatureID;
	public uint CreatureDisplayInfoID;
	public ObjectGuid ActorGUID;
	public int Id;
	public ConversationActorType Type;
	public uint NoActorObject;

	public void WriteCreate(WorldPacket data, Conversation owner, Player receiver)
	{
		data.WriteUInt32(CreatureID);
		data.WriteUInt32(CreatureDisplayInfoID);
		data.WritePackedGuid(ActorGUID);
		data.WriteInt32(Id);
		data.WriteBits(Type, 1);
		data.WriteBits(NoActorObject, 1);
		data.FlushBits();
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Conversation owner, Player receiver)
	{
		data.WriteUInt32(CreatureID);
		data.WriteUInt32(CreatureDisplayInfoID);
		data.WritePackedGuid(ActorGUID);
		data.WriteInt32(Id);
		data.WriteBits(Type, 1);
		data.WriteBits(NoActorObject, 1);
		data.FlushBits();
	}
}