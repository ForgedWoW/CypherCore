// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(43265)] // 43265 - Death and Decay
internal class SpellDkDeathAndDecayAuraScript : AuraScript, IHasAuraEffects
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
                caster.SpellFactory.CastSpell(at.Location, DeathKnightSpells.DEATH_AND_DECAY_DAMAGE);
        }
    }
}

[SpellScript(52212)]
internal class SpellDkDeathAndDecayDamageProcs : SpellScript, ISpellAfterHit
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
                caster.SpellFactory.CastSpell(target, DeathKnightSpells.FESTERING_WOUND, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, 1));
    }
}