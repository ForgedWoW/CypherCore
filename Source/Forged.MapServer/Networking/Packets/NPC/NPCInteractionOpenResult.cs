// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.NPC;

public class NPCInteractionOpenResult : ServerPacket
{
    public PlayerInteractionType InteractionType;
    public ObjectGuid Npc;
    public bool Success = true;
    public NPCInteractionOpenResult() : base(ServerOpcodes.NpcInteractionOpenResult) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Npc);
        _worldPacket.WriteInt32((int)InteractionType);
        _worldPacket.WriteBit(Success);
        _worldPacket.FlushBits();
    }
}