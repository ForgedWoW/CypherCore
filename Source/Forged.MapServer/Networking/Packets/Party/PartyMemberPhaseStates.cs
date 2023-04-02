// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Party;

public class PartyMemberPhaseStates
{
    public List<PartyMemberPhase> List = new();
    public ObjectGuid PersonalGUID;
    public int PhaseShiftFlags;
    public void Write(WorldPacket data)
    {
        data.WriteInt32(PhaseShiftFlags);
        data.WriteInt32(List.Count);
        data.WritePackedGuid(PersonalGUID);

        foreach (var phase in List)
            phase.Write(data);
    }
}