// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.RealmServer.Entities;

public class PetAura
{
	readonly Dictionary<uint, uint> _auras = new();
	readonly bool _removeOnChangePet;
	readonly double _damage;

	public PetAura()
	{
		_removeOnChangePet = false;
		_damage = 0;
	}

	public PetAura(uint petEntry, uint aura, bool removeOnChangePet, double damage)
	{
		_removeOnChangePet = removeOnChangePet;
		_damage = damage;

		_auras[petEntry] = aura;
	}

	public uint GetAura(uint petEntry)
	{
		var auraId = _auras.LookupByKey(petEntry);

		if (auraId != 0)
			return auraId;

		auraId = _auras.LookupByKey(0);

		if (auraId != 0)
			return auraId;

		return 0;
	}

	public void AddAura(uint petEntry, uint aura)
	{
		_auras[petEntry] = aura;
	}

	public bool IsRemovedOnChangePet()
	{
		return _removeOnChangePet;
	}

	public double GetDamage()
	{
		return _damage;
	}
}