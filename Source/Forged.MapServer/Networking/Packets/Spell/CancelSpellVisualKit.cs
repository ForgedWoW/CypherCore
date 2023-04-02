// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class CancelSpellVisualKit : ServerPacket
{
    public bool MountedVisual;
    public ObjectGuid Source;
    public uint SpellVisualKitID;
    public CancelSpellVisualKit() : base(ServerOpcodes.CancelSpellVisualKit) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Source);
        _worldPacket.WriteUInt32(SpellVisualKitID);
        _worldPacket.WriteBit(MountedVisual);
        _worldPacket.FlushBits();
    }
}