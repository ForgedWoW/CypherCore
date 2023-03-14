// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Mage;

[Script]
public class at_mage_flame_patch : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUpdate
{
	public int timeInterval;

	public void OnCreate()
	{
		timeInterval = 1000;
	}

	public void OnUpdate(uint diff)
	{
		var caster = At.GetCaster();

		if (caster == null)
			return;

		if (caster.TypeId != TypeId.Player)
			return;

		timeInterval += (int)diff;

		if (timeInterval < 1000)
			return;

		caster.CastSpell(At.Location, MageSpells.FLAME_PATCH_AOE_DMG, true);

		timeInterval -= 1000;
	}
}