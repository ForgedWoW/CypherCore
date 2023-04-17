// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenMountedCharge : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        var spell = Global.SpellMgr.GetSpellInfo(ScriptSpellId, Difficulty.None);

        if (spell.HasEffect(SpellEffectName.ScriptEffect))
            SpellEffects.Add(new EffectHandler(HandleScriptEffect, SpellConst.EffectFirstFound, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));

        if (spell.GetEffect(0).IsEffect(SpellEffectName.Charge))
            SpellEffects.Add(new EffectHandler(HandleChargeEffect, 0, SpellEffectName.Charge, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScriptEffect(int effIndex)
    {
        var target = HitUnit;

        switch (effIndex)
        {
            case 0: // On spells wich trigger the damaging spell (and also the visual)
            {
                uint spellId;

                switch (SpellInfo.Id)
                {
                    case GenericSpellIds.TRIGGER_TRIAL_CHAMPION:
                        spellId = GenericSpellIds.CHARGING20_K1;

                        break;
                    case GenericSpellIds.TRIGGER_FACTION_MOUNTS:
                        spellId = GenericSpellIds.CHARGING_EFFECT8_K5;

                        break;
                    default:
                        return;
                }

                // If Target isn't a training dummy there's a chance of failing the charge
                if (!target.IsCharmedOwnedByPlayerOrPlayer &&
                    RandomHelper.randChance(12.5f))
                    spellId = GenericSpellIds.MISS_EFFECT;

                var vehicle = Caster.VehicleBase;

                if (vehicle)
                    vehicle.SpellFactory.CastSpell(target, spellId, false);
                else
                    Caster.SpellFactory.CastSpell(target, spellId, false);

                break;
            }
            case 1: // On damaging spells, for removing a defend layer
            case 2:
            {
                var auras = target.GetAppliedAurasQuery();

                foreach (var pair in auras.HasSpellIds(62552, 62719, 64100, 66482).GetResults())
                {
                    var aura = pair.Base;

                    if (aura != null)
                    {
                        aura.ModStackAmount(-1, AuraRemoveMode.EnemySpell);
                        // Remove dummys from rider (Necessary for updating visual shields)
                        var rider = target.Charmer;

                        if (rider)
                        {
                            var defend = rider.GetAura(aura.Id);

                            defend?.ModStackAmount(-1, AuraRemoveMode.EnemySpell);
                        }

                        break;
                    }
                }

                break;
            }
        }
    }

    private void HandleChargeEffect(int effIndex)
    {
        uint spellId;

        switch (SpellInfo.Id)
        {
            case GenericSpellIds.CHARGING_EFFECT8_K5:
                spellId = GenericSpellIds.DAMAGE8_K5;

                break;
            case GenericSpellIds.CHARGING20_K1:
            case GenericSpellIds.CHARGING20_K2:
                spellId = GenericSpellIds.DAMAGE20_K;

                break;
            case GenericSpellIds.CHARGING_EFFECT45_K1:
            case GenericSpellIds.CHARGING_EFFECT45_K2:
                spellId = GenericSpellIds.DAMAGE45_K;

                break;
            default:
                return;
        }

        var rider = Caster.Charmer;

        if (rider)
            rider.SpellFactory.CastSpell(HitUnit, spellId, false);
        else
            Caster.SpellFactory.CastSpell(HitUnit, spellId, false);
    }
}