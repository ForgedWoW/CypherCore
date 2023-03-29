// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IUnit;
using Game.Spells;

namespace Scripts.Spells.Paladin;

//183778
[Script]
public class judgment_of_light : ScriptObjectAutoAdd, IUnitOnDamage
{
    public judgment_of_light() : base("judgment_of_light") { }

    public void OnDamage(Unit caster, Unit target, ref double damage)
    {
        var player = caster.AsPlayer;

        if (player != null)
            if (player.Class != PlayerClass.Paladin)
                return;

        if (caster == null || target == null)
            return;

        if (caster.HasAura(PaladinSpells.JUDGMENT_OF_LIGHT) && target.HasAura(PaladinSpells.JUDGMENT_OF_LIGHT_TARGET_DEBUFF))
            if (caster.IsWithinMeleeRange(target))
            {
                caster.CastSpell(PaladinSpells.JUDGMENT_OF_LIGHT_HEAL, true);
                target.RemoveAura(PaladinSpells.JUDGMENT_OF_LIGHT_TARGET_DEBUFF, AuraRemoveMode.EnemySpell);
            }
    }
}