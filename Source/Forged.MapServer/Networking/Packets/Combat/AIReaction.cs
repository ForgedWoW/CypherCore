// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Combat;

public class AIReaction : ServerPacket
{
    public AiReaction Reaction;
    public ObjectGuid UnitGUID;
    public AIReaction() : base(ServerOpcodes.AiReaction, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(UnitGUID);
        _worldPacket.WriteUInt32((uint)Reaction);
    }
}