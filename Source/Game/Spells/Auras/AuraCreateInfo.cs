// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Spells;

public class AuraCreateInfo
{
	public ObjectGuid CasterGuid;
	public Unit Caster;
	public Dictionary<int, double> BaseAmount;
	public ObjectGuid CastItemGuid;
	public uint CastItemId = 0;
	public int CastItemLevel = -1;
	public bool IsRefresh;
	public bool ResetPeriodicTimer = true;

	internal ObjectGuid CastId;
	internal SpellInfo SpellInfo;
	internal Difficulty CastDifficulty;
	internal uint AuraEffectMask;
	internal WorldObject Owner;

	internal uint TargetEffectMask;

	public AuraCreateInfo(ObjectGuid castId, SpellInfo spellInfo, Difficulty castDifficulty, uint auraEffMask, WorldObject owner)
	{
		CastId = castId;
		SpellInfo = spellInfo;
		CastDifficulty = castDifficulty;
		AuraEffectMask = auraEffMask;
		Owner = owner;

		Cypher.Assert(spellInfo != null);
		Cypher.Assert(auraEffMask != 0);
		Cypher.Assert(owner != null);

		Cypher.Assert(auraEffMask <= SpellConst.MaxEffectMask);
	}

	public void SetCasterGuid(ObjectGuid guid)
	{
		CasterGuid = guid;
	}

	public void SetCaster(Unit caster)
	{
		Caster = caster;
	}

	public void SetBaseAmount(Dictionary<int, double> bp)
	{
		BaseAmount = bp;
	}

	public void SetCastItem(ObjectGuid guid, uint itemId, int itemLevel)
	{
		CastItemGuid = guid;
		CastItemId = itemId;
		CastItemLevel = itemLevel;
	}

	public void SetPeriodicReset(bool reset)
	{
		ResetPeriodicTimer = reset;
	}

	public void SetOwnerEffectMask(uint effMask)
	{
		TargetEffectMask = effMask;
	}

	public void SetAuraEffectMask(uint effMask)
	{
		AuraEffectMask = effMask;
	}

	public SpellInfo GetSpellInfo()
	{
		return SpellInfo;
	}

	public uint GetAuraEffectMask()
	{
		return AuraEffectMask;
	}

	public WorldObject GetOwner()
	{
		return Owner;
	}
}