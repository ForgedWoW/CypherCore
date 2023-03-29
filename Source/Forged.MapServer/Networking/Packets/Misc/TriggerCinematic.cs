// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

public class TriggerCinematic : ServerPacket
{
    public uint CinematicID;
    public ObjectGuid ConversationGuid;
    public TriggerCinematic() : base(ServerOpcodes.TriggerCinematic) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(CinematicID);
        _worldPacket.WritePackedGuid(ConversationGuid);
    }
}