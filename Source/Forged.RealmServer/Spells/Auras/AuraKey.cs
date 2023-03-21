// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Spells;

public class AuraKey : IEquatable<AuraKey>
{
	public ObjectGuid Caster;
	public ObjectGuid Item;
	public uint SpellId;
	public uint EffectMask;

	public AuraKey(ObjectGuid caster, ObjectGuid item, uint spellId, uint effectMask)
	{
		Caster = caster;
		Item = item;
		SpellId = spellId;
		EffectMask = effectMask;
	}

	public bool Equals(AuraKey other)
	{
		return other.Caster == Caster && other.Item == Item && other.SpellId == SpellId && other.EffectMask == EffectMask;
	}

	public static bool operator ==(AuraKey first, AuraKey other)
	{
		if (ReferenceEquals(first, other))
			return true;

		if (ReferenceEquals(first, null) || ReferenceEquals(other, null))
			return false;

		return first.Equals(other);
	}

	public static bool operator !=(AuraKey first, AuraKey other)
	{
		return !(first == other);
	}

	public override bool Equals(object obj)
	{
		return obj != null && obj is AuraKey && Equals((AuraKey)obj);
	}

	public override int GetHashCode()
	{
		return new
		{
			Caster,
			Item,
			SpellId,
			EffectMask
		}.GetHashCode();
	}
}