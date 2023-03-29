// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class CancelOrphanSpellVisual : ServerPacket
{
    public uint SpellVisualID;
    public CancelOrphanSpellVisual() : base(ServerOpcodes.CancelOrphanSpellVisual) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(SpellVisualID);
    }
}