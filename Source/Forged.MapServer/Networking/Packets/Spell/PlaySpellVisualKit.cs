// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class PlaySpellVisualKit : ServerPacket
{
    public uint Duration;
    public uint KitRecID;
    public uint KitType;
    public bool MountedVisual;
    public ObjectGuid Unit;
    public PlaySpellVisualKit() : base(ServerOpcodes.PlaySpellVisualKit) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Unit);
        WorldPacket.WriteUInt32(KitRecID);
        WorldPacket.WriteUInt32(KitType);
        WorldPacket.WriteUInt32(Duration);
        WorldPacket.WriteBit(MountedVisual);
        WorldPacket.FlushBits();
    }
}