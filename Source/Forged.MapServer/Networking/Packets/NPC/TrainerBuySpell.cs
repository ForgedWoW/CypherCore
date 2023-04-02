// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.NPC;

internal class TrainerBuySpell : ClientPacket
{
    public uint SpellID;
    public ObjectGuid TrainerGUID;
    public uint TrainerID;
    public TrainerBuySpell(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        TrainerGUID = WorldPacket.ReadPackedGuid();
        TrainerID = WorldPacket.ReadUInt32();
        SpellID = WorldPacket.ReadUInt32();
    }
}