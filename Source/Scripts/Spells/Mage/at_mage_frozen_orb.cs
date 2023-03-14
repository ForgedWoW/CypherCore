// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Mage;

[Script]
public class at_mage_frozen_orb : AreaTriggerScript, IAreaTriggerOnInitialize, IAreaTriggerOnUpdate
{
	public uint damageInterval;
	public bool procDone = false;

	public void OnInitialize()
	{
		damageInterval = 500;
		var caster = At.GetCaster();

		if (caster == null)
			return;

		var pos = caster.Location;

		At.MovePositionToFirstCollision(pos, 40.0f, 0.0f);
		At.SetDestination(pos, 4000);
	}

	public void OnUpdate(uint diff)
	{
		var caster = At.GetCaster();

		if (caster == null || !caster.IsPlayer)
			return;

		if (damageInterval <= diff)
		{
			if (!procDone)
				foreach (var guid in At.InsideUnits)
				{
					var unit = ObjectAccessor.Instance.GetUnit(caster, guid);

					if (unit != null)
						if (caster.IsValidAttackTarget(unit))
						{
							if (caster.HasAura(MageSpells.FINGERS_OF_FROST_AURA))
								caster.CastSpell(caster, MageSpells.FINGERS_OF_FROST_VISUAL_UI, true);

							caster.CastSpell(caster, MageSpells.FINGERS_OF_FROST_AURA, true);

							// at->UpdateTimeToTarget(8000); TODO
							procDone = true;

							break;
						}
				}

			caster.CastSpell(At.Location, MageSpells.FROZEN_ORB_DAMAGE, true);
			damageInterval = 500;
		}
		else
		{
			damageInterval -= diff;
		}
	}
}