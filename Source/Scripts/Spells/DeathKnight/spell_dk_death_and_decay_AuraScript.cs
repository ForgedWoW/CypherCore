// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(43265)] // 43265 - Death and Decay
internal class spell_dk_death_and_decay_AuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleDummyTick, 2, AuraType.PeriodicDummy));
    }

    private void HandleDummyTick(AuraEffect aurEff)
    {
        var caster = Caster;

        if (caster)
        {
            var at = caster.GetAreaTrigger(DeathKnightSpells.DEATH_AND_DECAY);

            if (at != null)
                caster.CastSpell(at.Location, DeathKnightSpells.DEATH_AND_DECAY_DAMAGE);
        }
    }
}

[SpellScript(52212)]
internal class spell_dk_death_and_decay_damage_procs : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var caster = Caster;
        var target = HitUnit;

        if (caster == null || target == null)
            return;

        var pestilence = caster.GetAura(DeathKnightSpells.PESTILENCE);

        if (pestilence != null)
            if (RandomHelper.randChance(pestilence.GetEffect(0).Amount))
                caster.CastSpell(target, DeathKnightSpells.FESTERING_WOUND, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, 1));
    }
}