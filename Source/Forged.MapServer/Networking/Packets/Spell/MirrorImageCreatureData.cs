// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class MirrorImageCreatureData : ServerPacket
{
    public int DisplayID;
    public int SpellVisualKitID;
    public ObjectGuid UnitGUID;
    public MirrorImageCreatureData() : base(ServerOpcodes.MirrorImageCreatureData) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(UnitGUID);
        WorldPacket.WriteInt32(DisplayID);
        WorldPacket.WriteInt32(SpellVisualKitID);
    }
}