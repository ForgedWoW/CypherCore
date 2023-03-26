// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Conditions;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Spells;

public class WorldObjectSpellTargetCheck : ICheck<WorldObject>
{
	internal WorldObject Caster;
	internal SpellInfo SpellInfo;
    private readonly WorldObject _referer;
    private readonly SpellTargetCheckTypes _targetSelectionType;
    private readonly ConditionSourceInfo _condSrcInfo;
    private readonly List<Condition> _condList;
    private readonly SpellTargetObjectTypes _objectType;

	public WorldObjectSpellTargetCheck(WorldObject caster, WorldObject referer, SpellInfo spellInfo, SpellTargetCheckTypes selectionType, List<Condition> condList, SpellTargetObjectTypes objectType)
	{
		Caster = caster;
		_referer = referer;
		SpellInfo = spellInfo;
		_targetSelectionType = selectionType;
		_condList = condList;
		_objectType = objectType;

		if (condList != null)
			_condSrcInfo = new ConditionSourceInfo(null, caster);
	}

	public virtual bool Invoke(WorldObject target)
	{
		if (SpellInfo.CheckTarget(Caster, target, true) != SpellCastResult.SpellCastOk)
			return false;

		var unitTarget = target.AsUnit;
		var corpseTarget = target.AsCorpse;

		if (corpseTarget != null)
		{
			// use owner for party/assistance checks
			var owner = Global.ObjAccessor.FindPlayer(corpseTarget.OwnerGUID);

			if (owner != null)
				unitTarget = owner;
			else
				return false;
		}

		var refUnit = _referer.AsUnit;

		if (unitTarget != null)
		{
			// do only faction checks here
			switch (_targetSelectionType)
			{
				case SpellTargetCheckTypes.Enemy:
					if (unitTarget.IsTotem)
						return false;

					// TODO: restore IsValidAttackTarget for corpses using corpse owner (faction, etc)
					if (!target.IsCorpse && !Caster.IsValidAttackTarget(unitTarget, SpellInfo))
						return false;

					break;
				case SpellTargetCheckTypes.Ally:
					if (unitTarget.IsTotem)
						return false;

					// TODO: restore IsValidAttackTarget for corpses using corpse owner (faction, etc)
					if (!target.IsCorpse && !Caster.IsValidAssistTarget(unitTarget, SpellInfo))
						return false;

					break;
				case SpellTargetCheckTypes.Party:
					if (refUnit == null)
						return false;

					if (unitTarget.IsTotem)
						return false;

					// TODO: restore IsValidAttackTarget for corpses using corpse owner (faction, etc)
					if (!target.IsCorpse && !Caster.IsValidAssistTarget(unitTarget, SpellInfo))
						return false;

					if (!refUnit.IsInPartyWith(unitTarget))
						return false;

					break;
				case SpellTargetCheckTypes.RaidClass:
					if (!refUnit)
						return false;

					if (refUnit.Class != unitTarget.Class)
						return false;

					goto case SpellTargetCheckTypes.Raid;
				case SpellTargetCheckTypes.Raid:
					if (refUnit == null)
						return false;

					if (unitTarget.IsTotem)
						return false;

					// TODO: restore IsValidAttackTarget for corpses using corpse owner (faction, etc)
					if (!target.IsCorpse && !Caster.IsValidAssistTarget(unitTarget, SpellInfo))
						return false;

					if (!refUnit.IsInRaidWith(unitTarget))
						return false;

					break;
				case SpellTargetCheckTypes.Summoned:
					if (!unitTarget.IsSummon)
						return false;

					if (unitTarget.ToTempSummon().GetSummonerGUID() != Caster.GUID)
						return false;

					break;
				case SpellTargetCheckTypes.Threat:
					if (!_referer.IsUnit || _referer.AsUnit.GetThreatManager().GetThreat(unitTarget, true) <= 0.0f)
						return false;

					break;
				case SpellTargetCheckTypes.Tap:
					if (_referer.TypeId != TypeId.Unit || unitTarget.TypeId != TypeId.Player)
						return false;

					if (!_referer.AsCreature.IsTappedBy(unitTarget.AsPlayer))
						return false;

					break;
			}

			switch (_objectType)
			{
				case SpellTargetObjectTypes.Corpse:
				case SpellTargetObjectTypes.CorpseAlly:
				case SpellTargetObjectTypes.CorpseEnemy:
					if (unitTarget.IsAlive)
						return false;

					break;
			}
		}

		if (_condSrcInfo == null)
			return true;

		_condSrcInfo.mConditionTargets[0] = target;

		return Global.ConditionMgr.IsObjectMeetToConditions(_condSrcInfo, _condList);
	}
}