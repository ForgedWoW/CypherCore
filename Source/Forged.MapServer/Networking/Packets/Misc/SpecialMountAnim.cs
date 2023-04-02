// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class SpecialMountAnim : ServerPacket
{
    public int SequenceVariation;
    public List<int> SpellVisualKitIDs = new();
    public ObjectGuid UnitGUID;
    public SpecialMountAnim() : base(ServerOpcodes.SpecialMountAnim, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(UnitGUID);
        WorldPacket.WriteInt32(SpellVisualKitIDs.Count);
        WorldPacket.WriteInt32(SequenceVariation);

        foreach (var id in SpellVisualKitIDs)
            WorldPacket.WriteInt32(id);
    }
}