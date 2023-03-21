// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Spells;

public class CastSpellTargetArg
{
	public SpellCastTargets Targets;

	public CastSpellTargetArg()
	{
		Targets = new SpellCastTargets();
	}

	public CastSpellTargetArg(WorldObject target)
	{
		if (target != null)
		{
			var unitTarget = target.AsUnit;

			if (unitTarget != null)
			{
				Targets = new SpellCastTargets();
				Targets.UnitTarget = unitTarget;
			}
			else
			{
				var goTarget = target.AsGameObject;

				if (goTarget != null)
				{
					Targets = new SpellCastTargets();
					Targets.GOTarget = goTarget;
				}
				else
				{
					var itemTarget = target.AsItem;

					if (itemTarget != null)
					{
						Targets = new SpellCastTargets();
						Targets.ItemTarget = itemTarget;
					}
				}
				// error when targeting anything other than units and gameobjects
			}
		}
		else
		{
			Targets = new SpellCastTargets(); // nullptr is allowed
		}
	}

	public CastSpellTargetArg(Item itemTarget)
	{
		Targets = new SpellCastTargets();
		Targets.ItemTarget = itemTarget;
	}

	public CastSpellTargetArg(Position dest)
	{
		Targets = new SpellCastTargets();
		Targets.SetDst(dest);
	}

	public CastSpellTargetArg(SpellCastTargets targets)
	{
		Targets = new SpellCastTargets();
		Targets = targets;
	}
}