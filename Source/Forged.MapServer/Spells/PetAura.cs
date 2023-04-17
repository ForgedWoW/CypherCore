// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Spells;

public class PetAura
{
    private readonly Dictionary<uint, uint> _auras = new();

    public PetAura()
    {
        IsRemovedOnChangePet = false;
        Damage = 0;
    }

    public PetAura(uint petEntry, uint aura, bool removeOnChangePet, double damage)
    {
        IsRemovedOnChangePet = removeOnChangePet;
        Damage = damage;

        _auras[petEntry] = aura;
    }

    public double Damage { get; }

    public bool IsRemovedOnChangePet { get; }

    public void AddAura(uint petEntry, uint aura)
    {
        _auras[petEntry] = aura;
    }

    public uint GetAura(uint petEntry)
    {
        if (_auras.TryGetValue(petEntry, out var auraId))
            return auraId;

        auraId = _auras.LookupByKey(0);

        return auraId != 0 ? auraId : (uint)0;
    }
}