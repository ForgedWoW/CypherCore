// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Maps.Checks;

public class CreatureWithOptionsInObjectRangeCheck<T> : ICheck<Creature> where T : NoopCheckCustomizer
{
	readonly WorldObject _obj;
	readonly T _customizer;
	readonly FindCreatureOptions _args;

	public CreatureWithOptionsInObjectRangeCheck(WorldObject obj, T customizer, FindCreatureOptions args)
	{
		_obj = obj;
		_args = args;
		_customizer = customizer;
	}

	public bool Invoke(Creature u)
	{
		if (u.DeathState == DeathState.Dead) // Despawned
			return false;

		if (u.GUID == _obj.GUID)
			return false;

		if (!_customizer.Test(u))
			return false;

		if (_args.CreatureId.HasValue && u.Entry != _args.CreatureId)
			return false;

		if (_args.StringId != null && !u.HasStringId(_args.StringId))
			return false;

		if (_args.IsAlive.HasValue && u.IsAlive != _args.IsAlive)
			return false;

		if (_args.IsSummon.HasValue && u.IsSummon != _args.IsSummon)
			return false;

		if (_args.IsInCombat.HasValue && u.IsInCombat != _args.IsInCombat)
			return false;

		if ((_args.OwnerGuid.HasValue && u.OwnerGUID != _args.OwnerGuid) || (_args.CharmerGuid.HasValue && u.CharmerGUID != _args.CharmerGuid) || (_args.CreatorGuid.HasValue && u.CreatorGUID != _args.CreatorGuid) || (_args.DemonCreatorGuid.HasValue && u.DemonCreatorGUID != _args.DemonCreatorGuid) || (_args.PrivateObjectOwnerGuid.HasValue && u.PrivateObjectOwner != _args.PrivateObjectOwnerGuid))
			return false;

		if (_args.IgnorePrivateObjects && u.IsPrivateObject)
			return false;

		if (_args.IgnoreNotOwnedPrivateObjects && !u.CheckPrivateObjectOwnerVisibility(_obj))
			return false;

		if (_args.AuraSpellId.HasValue && !u.HasAura((uint)_args.AuraSpellId))
			return false;

		_customizer.Update(u);

		return true;
	}
}