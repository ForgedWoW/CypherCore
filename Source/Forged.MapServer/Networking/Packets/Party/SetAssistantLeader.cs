// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Party;

internal class SetAssistantLeader : ClientPacket
{
    public bool Apply;
    public byte PartyIndex;
    public ObjectGuid Target;
    public SetAssistantLeader(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        PartyIndex = WorldPacket.ReadUInt8();
        Target = WorldPacket.ReadPackedGuid();
        Apply = WorldPacket.HasBit();
    }
}