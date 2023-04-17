// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IUnit;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

//183778
[Script]
public class JudgmentOfLight : ScriptObjectAutoAdd, IUnitOnDamage
{
    public JudgmentOfLight() : base("judgment_of_light") { }

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
                caster.SpellFactory.CastSpell(PaladinSpells.JUDGMENT_OF_LIGHT_HEAL, true);
                target.RemoveAura(PaladinSpells.JUDGMENT_OF_LIGHT_TARGET_DEBUFF, AuraRemoveMode.EnemySpell);
            }
    }
}