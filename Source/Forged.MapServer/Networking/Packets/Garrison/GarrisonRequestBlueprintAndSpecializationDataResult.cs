// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Garrison;

internal class GarrisonRequestBlueprintAndSpecializationDataResult : ServerPacket
{
    public List<uint> BlueprintsKnown = null;
    public GarrisonType GarrTypeID;
    public List<uint> SpecializationsKnown = null;
    public GarrisonRequestBlueprintAndSpecializationDataResult() : base(ServerOpcodes.GarrisonRequestBlueprintAndSpecializationDataResult, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32((uint)GarrTypeID);
        WorldPacket.WriteInt32(BlueprintsKnown?.Count ?? 0);
        WorldPacket.WriteInt32(SpecializationsKnown?.Count ?? 0);

        if (BlueprintsKnown != null)
            foreach (var blueprint in BlueprintsKnown)
                WorldPacket.WriteUInt32(blueprint);

        if (SpecializationsKnown != null)
            foreach (var specialization in SpecializationsKnown)
                WorldPacket.WriteUInt32(specialization);
    }
}