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
        WorldPacket.WritePackedGuid(Npc);
        WorldPacket.WriteInt32((int)InteractionType);
        WorldPacket.WriteBit(Success);
        WorldPacket.FlushBits();
    }
}