// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_mounted_charge : SpellScript, IHasSpellEffects
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
                    case GenericSpellIds.TriggerTrialChampion:
                        spellId = GenericSpellIds.Charging20k1;

                        break;
                    case GenericSpellIds.TriggerFactionMounts:
                        spellId = GenericSpellIds.ChargingEffect8k5;

                        break;
                    default:
                        return;
                }

                // If Target isn't a training dummy there's a chance of failing the charge
                if (!target.IsCharmedOwnedByPlayerOrPlayer &&
                    RandomHelper.randChance(12.5f))
                    spellId = GenericSpellIds.MissEffect;

                var vehicle = Caster.VehicleBase;

                if (vehicle)
                    vehicle.CastSpell(target, spellId, false);
                else
                    Caster.CastSpell(target, spellId, false);

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
            case GenericSpellIds.ChargingEffect8k5:
                spellId = GenericSpellIds.Damage8k5;

                break;
            case GenericSpellIds.Charging20k1:
            case GenericSpellIds.Charging20k2:
                spellId = GenericSpellIds.Damage20k;

                break;
            case GenericSpellIds.ChargingEffect45k1:
            case GenericSpellIds.ChargingEffect45k2:
                spellId = GenericSpellIds.Damage45k;

                break;
            default:
                return;
        }

        var rider = Caster.Charmer;

        if (rider)
            rider.CastSpell(HitUnit, spellId, false);
        else
            Caster.CastSpell(HitUnit, spellId, false);
    }
}