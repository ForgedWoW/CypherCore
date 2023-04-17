// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 88766 - Fulmination
[SpellScript(88766)]
public class SpellShaFulmination : AuraScript, IHasAuraEffects, IAuraCheckProc
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        // Lava Burst cannot add lightning shield stacks without Improved Lightning Shield
        if ((eventInfo.SpellInfo.SpellFamilyFlags[1] & 0x00001000) != 0 && !eventInfo.Actor.HasAura(ShamanSpells.IMPROVED_LIGHTNING_SHIELD))
            return false;

        return eventInfo.Actor.HasAura(ShamanSpells.LIGHTNING_SHIELD);
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        var caster = eventInfo.Actor;
        var target = eventInfo.ActionTarget;
        var aura = caster.GetAura(ShamanSpells.LIGHTNING_SHIELD);

        if (aura != null)
        {
            // Earth Shock releases the charges
            if ((eventInfo.SpellInfo.SpellFamilyFlags[0] & 0x00100000) != 0)
            {
                uint stacks = aura.Charges;

                if (stacks > 1)
                {
                    var triggerSpell = Global.SpellMgr.AssertSpellInfo(aura.SpellInfo.GetEffect(0).TriggerSpell, Difficulty.None);
                    var triggerEffect = triggerSpell.GetEffect(0);

                    double damage;
                    damage = caster.SpellDamageBonusDone(target, triggerSpell, triggerEffect.CalcValue(caster), DamageEffectType.SpellDirect, triggerEffect, stacks - 1);
                    damage = target.SpellDamageBonusTaken(caster, triggerSpell, damage, DamageEffectType.SpellDirect);

                    caster.SpellFactory.CastSpell(target, ShamanSpells.FULMINATION, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)(int)damage));
                    caster.RemoveAura(ShamanSpells.FULMINATION_INFO);

                    var t184P = caster.GetAuraEffect(ShamanSpells.ITEM_T18_ELEMENTAL_4_P_BONUS, 0);

                    if (t184P != null)
                    {
                        var gatheringVortex = caster.GetAura(ShamanSpells.ITEM_T18_GATHERING_VORTEX);

                        if (gatheringVortex != null)
                        {
                            if (gatheringVortex.StackAmount + stacks >= (uint)t184P.Amount)
                                caster.SpellFactory.CastSpell(caster, ShamanSpells.ITEM_T18_LIGHTNING_VORTEX, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

                            var newStacks = (byte)((gatheringVortex.StackAmount + stacks) % t184P.Amount);

                            if (newStacks != 0)
                                gatheringVortex.SetStackAmount(newStacks);
                            else
                                gatheringVortex.Remove();
                        }
                        else
                        {
                            caster.SpellFactory.CastSpell(caster, ShamanSpells.ITEM_T18_GATHERING_VORTEX, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.AuraStack, (int)stacks));
                        }
                    }

                    var t182P = caster.GetAuraEffect(ShamanSpells.ITEM_T18_ELEMENTAL_2_P_BONUS, 0);

                    if (t182P != null)
                        if (RandomHelper.randChance(t182P.Amount))
                        {
                            caster.SpellHistory.ResetCooldown(ShamanSpells.EARTH_SHOCK, true);

                            return;
                        }

                    aura.SetCharges(1);
                    aura.IsUsingCharges = false;
                }
            }
            else
            {
                aura.SetCharges(Math.Min(aura.Charges + 1, (byte)aurEff.Amount));
                aura.IsUsingCharges = false;
                aura.RefreshDuration();

                if (aura.Charges == aurEff.Amount)
                    caster.SpellFactory.CastSpell(caster, ShamanSpells.FULMINATION_INFO, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
            }
        }
    }
}