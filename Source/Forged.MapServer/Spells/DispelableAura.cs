// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Spells.Auras;

namespace Forged.MapServer.Spells;

public class DispelableAura
{
    private readonly int _chance;

    public DispelableAura(Aura aura, int dispelChance, byte dispelCharges)
    {
        Aura = aura;
        _chance = dispelChance;
        DispelCharges = dispelCharges;
    }

    public Aura Aura { get; }

    public byte DispelCharges { get; private set; }

    public bool DecrementCharge(byte charges)
    {
        if (DispelCharges == 0)
            return false;

        DispelCharges -= charges;

        return DispelCharges > 0;
    }

    public void IncrementCharges()
    {
        ++DispelCharges;
    }

    public bool RollDispel()
    {
        return RandomHelper.randChance(_chance);
    }
}