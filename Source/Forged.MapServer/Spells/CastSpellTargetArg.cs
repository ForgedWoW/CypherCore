// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Spells;

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
				Targets = new SpellCastTargets
                {
                    UnitTarget = unitTarget
                };
            }
			else
			{
				var goTarget = target.AsGameObject;

				if (goTarget != null)
				{
					Targets = new SpellCastTargets
                    {
                        GOTarget = goTarget
                    };
                }
				else
				{
					var itemTarget = target.AsItem;

					if (itemTarget != null)
					{
						Targets = new SpellCastTargets
                        {
                            ItemTarget = itemTarget
                        };
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
		Targets = new SpellCastTargets
        {
            ItemTarget = itemTarget
        };
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