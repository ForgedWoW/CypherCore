// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using Framework.Constants;

namespace Forged.MapServer.Entities.Objects.Update;

public class ConversationActorField
{
    public ObjectGuid ActorGUID;
    public uint CreatureDisplayInfoID;
    public uint CreatureID;
    public int Id;
    public uint NoActorObject;
    public ConversationActorType Type;
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