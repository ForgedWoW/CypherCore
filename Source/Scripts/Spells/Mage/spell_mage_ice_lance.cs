﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // Ice Lance - 30455
internal class spell_mage_ice_lance : SpellScript, IHasSpellEffects
{
    private readonly List<ObjectGuid> _orderedTargets = new();
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(IndexTarget, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.LaunchTarget));
        SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void IndexTarget(int effIndex)
    {
        _orderedTargets.Add(HitUnit.GUID);
    }

    private void HandleOnHit(int effIndex)
    {
        var caster = Caster;
        var target = HitUnit;

        var index = _orderedTargets.IndexOf(target.GUID);

        if (index == 0 // only primary Target triggers these benefits
            &&
            target.HasAuraState(AuraStateType.Frozen, SpellInfo, caster))
        {
            // Thermal Void
            var thermalVoid = caster.GetAura(MageSpells.ThermalVoid);

            if (!thermalVoid.SpellInfo.Effects.Empty())
            {
                var icyVeins = caster.GetAura(MageSpells.IcyVeins);

                icyVeins?.SetDuration(icyVeins.Duration + thermalVoid.SpellInfo.GetEffect(0).CalcValue(caster) * Time.IN_MILLISECONDS);
            }

            // Chain Reaction
            if (caster.HasAura(MageSpells.ChainReactionDummy))
                caster.CastSpell(caster, MageSpells.ChainReaction, true);
        }

        // put Target index for chain value Multiplier into EFFECT_1 base points, otherwise triggered spell doesn't know which Damage Multiplier to apply
        CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
        args.AddSpellMod(SpellValueMod.BasePoint1, index);
        caster.CastSpell(target, MageSpells.IceLanceTrigger, args);
    }
}