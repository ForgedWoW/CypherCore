// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class PhaseShiftData
{
    public ObjectGuid PersonalGUID;
    public List<PhaseShiftDataPhase> Phases = new();
    public uint PhaseShiftFlags;
    public void Write(WorldPacket data)
    {
        data.WriteUInt32(PhaseShiftFlags);
        data.WriteInt32(Phases.Count);
        data.WritePackedGuid(PersonalGUID);

        foreach (var phaseShiftDataPhase in Phases)
            phaseShiftDataPhase.Write(data);
    }
}