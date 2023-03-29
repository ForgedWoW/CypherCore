﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Spells.Auras;

namespace Forged.MapServer.Spells;

public class DispelableAura
{
    private readonly Aura _aura;
    private readonly int _chance;
    private byte _charges;

    public DispelableAura(Aura aura, int dispelChance, byte dispelCharges)
    {
        _aura = aura;
        _chance = dispelChance;
        _charges = dispelCharges;
    }

    public bool RollDispel()
    {
        return RandomHelper.randChance(_chance);
    }

    public Aura GetAura()
    {
        return _aura;
    }

    public byte GetDispelCharges()
    {
        return _charges;
    }

    public void IncrementCharges()
    {
        ++_charges;
    }

    public bool DecrementCharge(byte charges)
    {
        if (_charges == 0)
            return false;

        _charges -= charges;

        return _charges > 0;
    }
}