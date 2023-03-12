// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
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
    public HashSet<int> AuraEffectMask = new();


    public SpellInfo SpellInfo => SpellInfoInternal;
    public WorldObject Owner => OwnerInternal;

    internal ObjectGuid CastId;
	internal SpellInfo SpellInfoInternal;
	internal Difficulty CastDifficulty;
	internal WorldObject OwnerInternal;

	internal HashSet<int> TargetEffectMask = new();

	public AuraCreateInfo(ObjectGuid castId, SpellInfo spellInfo, Difficulty castDifficulty, HashSet<int> auraEffMask, WorldObject owner)
	{
		CastId = castId;
		SpellInfoInternal = spellInfo;
		CastDifficulty = castDifficulty;
		AuraEffectMask = auraEffMask;
		OwnerInternal = owner;

		Cypher.Assert(spellInfo != null);
		Cypher.Assert(auraEffMask.Count != 0);
		Cypher.Assert(owner != null);

		Cypher.Assert(auraEffMask.Count <= SpellConst.MaxEffects.Count);
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

	public void SetOwnerEffectMask(HashSet<int> effMask)
	{
		TargetEffectMask = effMask;
	}
}