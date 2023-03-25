// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.Objects;

public struct FindCreatureOptions
{
	public FindCreatureOptions SetCreatureId(uint creatureId)
	{
		CreatureId = creatureId;

		return this;
	}

	public FindCreatureOptions SetStringId(string stringId)
	{
		StringId = stringId;

		return this;
	}

	public FindCreatureOptions SetIsAlive(bool isAlive)
	{
		IsAlive = isAlive;

		return this;
	}

	public FindCreatureOptions SetIsInCombat(bool isInCombat)
	{
		IsInCombat = isInCombat;

		return this;
	}

	public FindCreatureOptions SetIsSummon(bool isSummon)
	{
		IsSummon = isSummon;

		return this;
	}

	public FindCreatureOptions SetIgnorePhases(bool ignorePhases)
	{
		IgnorePhases = ignorePhases;

		return this;
	}

	public FindCreatureOptions SetIgnoreNotOwnedPrivateObjects(bool ignoreNotOwnedPrivateObjects)
	{
		IgnoreNotOwnedPrivateObjects = ignoreNotOwnedPrivateObjects;

		return this;
	}

	public FindCreatureOptions SetIgnorePrivateObjects(bool ignorePrivateObjects)
	{
		IgnorePrivateObjects = ignorePrivateObjects;

		return this;
	}

	public FindCreatureOptions SetHasAura(uint spellId)
	{
		AuraSpellId = spellId;

		return this;
	}

	public FindCreatureOptions SetOwner(ObjectGuid ownerGuid)
	{
		OwnerGuid = ownerGuid;

		return this;
	}

	public FindCreatureOptions SetCharmer(ObjectGuid charmerGuid)
	{
		CharmerGuid = charmerGuid;

		return this;
	}

	public FindCreatureOptions SetCreator(ObjectGuid creatorGuid)
	{
		CreatorGuid = creatorGuid;

		return this;
	}

	public FindCreatureOptions SetDemonCreator(ObjectGuid demonCreatorGuid)
	{
		DemonCreatorGuid = demonCreatorGuid;

		return this;
	}

	public FindCreatureOptions SetPrivateObjectOwner(ObjectGuid privateObjectOwnerGuid)
	{
		PrivateObjectOwnerGuid = privateObjectOwnerGuid;

		return this;
	}

	public uint? CreatureId;
	public string StringId;

	public bool? IsAlive;
	public bool? IsInCombat;
	public bool? IsSummon;

	public bool IgnorePhases;
	public bool IgnoreNotOwnedPrivateObjects;
	public bool IgnorePrivateObjects;

	public uint? AuraSpellId;
	public ObjectGuid? OwnerGuid;
	public ObjectGuid? CharmerGuid;
	public ObjectGuid? CreatorGuid;
	public ObjectGuid? DemonCreatorGuid;
	public ObjectGuid? PrivateObjectOwnerGuid;
}