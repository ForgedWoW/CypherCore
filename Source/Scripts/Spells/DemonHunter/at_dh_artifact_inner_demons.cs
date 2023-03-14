// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.DemonHunter;

[Script]
public class at_dh_artifact_inner_demons : AreaTriggerScript, IAreaTriggerOnInitialize, IAreaTriggerOnRemove
{
	public void OnInitialize()
	{
		var caster = At.GetCaster();

		if (caster == null)
			return;

		var guid = caster.VariableStorage.GetValue<ObjectGuid>("Spells.InnerDemonsTarget", ObjectGuid.Empty);
		var target = ObjectAccessor.Instance.GetUnit(caster, guid);

		if (target != null)
		{
			List<Vector3> splinePoints = new();
			var orientation = caster.Location.Orientation;
			var posX = caster.Location.X - 7 * (float)Math.Cos(orientation);
			var posY = caster.Location.Y - 7 * (float)Math.Sin(orientation); // Start from behind the caster
			splinePoints.Add(new Vector3(posX, posY, caster.Location.Z));
			splinePoints.Add(new Vector3(target.Location.X, target.Location.Y, target.Location.Z));

			At.InitSplines(splinePoints, 1000);
		}
		else
		{
			caster.VariableStorage.Remove("Spells.InnerDemonsTarget");
		}
	}

	public void OnRemove()
	{
		var caster = At.GetCaster();

		if (caster == null)
			return;

		caster.CastSpell(At, DemonHunterSpells.INNER_DEMONS_DAMAGE, true);
	}
}