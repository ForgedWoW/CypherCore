// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class CancelSpellVisual : ServerPacket
{
    public ObjectGuid Source;
    public uint SpellVisualID;
    public CancelSpellVisual() : base(ServerOpcodes.CancelSpellVisual) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Source);
        _worldPacket.WriteUInt32(SpellVisualID);
    }
}