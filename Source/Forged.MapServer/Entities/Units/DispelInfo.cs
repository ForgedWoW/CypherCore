// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Entities.Units;

public class DispelInfo
{
    public DispelInfo(WorldObject dispeller, uint dispellerSpellId, byte chargesRemoved)
    {
        Dispeller = dispeller;
        DispellerSpellId = dispellerSpellId;
        RemovedCharges = chargesRemoved;
    }

    public WorldObject Dispeller { get; set; }
    public uint DispellerSpellId { get; set; }
    public byte RemovedCharges { get; set; }
}