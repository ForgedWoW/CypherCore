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

namespace Scripts.Spells.Mage;

[SpellScript(76613)]
public class SpellMasteryIciclesProc : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public static readonly uint[][] Icicles =
    {
        new uint[]
        {
            148012, 148017, 148013
        },
        new uint[]
        {
            148013, 148018, 148014
        },
        new uint[]
        {
            148014, 148019, 148015
        },
        new uint[]
        {
            148015, 148020, 148016
        },
        new uint[]
        {
            148016, 148021, 148012
        }
    };

    public static readonly uint[] IcicleAuras =
    {
        214124, 214125, 214126, 214127, 214130
    };

    public static readonly uint[] IcicleHits =
    {
        148017, 148018, 148019, 148020, 148021
    };

    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var spellCanProc = (eventInfo.SpellInfo.Id == MageSpells.FROSTBOLT || eventInfo.SpellInfo.Id == MageSpells.FROSTBOLT_TRIGGER);

        if (spellCanProc)
            return true;

        return false;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void OnProc(AuraEffect unnamedParameter, ProcEventInfo eventInfo)
    {
        var target = eventInfo.DamageInfo.Victim;
        var caster = eventInfo.DamageInfo.Attacker;

        if (target == null || caster == null)
            return;

        var player = caster.AsPlayer;

        if (player == null)
            return;

        // Calculate damage
        var hitDamage = eventInfo.DamageInfo.Damage + eventInfo.DamageInfo.Absorb;

        // if hitDamage == 0 we have a miss, so we need to except this variant
        if (hitDamage != 0)
        {
            var icilesAddSecond = false;

            if (caster.HasAura(MageSpells.ICE_NINE))
                if (RandomHelper.randChance(20))
                    icilesAddSecond = true;

            hitDamage *= (uint)((player.ActivePlayerData.Mastery * 2.25f) / 100.0f);

            // Prevent huge hits on player after hitting low level creatures
            if (player.Level > target.Level)
                hitDamage = (uint)Math.Min((int)hitDamage, (int)target.MaxHealth);

            // We need to get the first free icicle slot
            sbyte icicleFreeSlot = -1;       // -1 means no free slot
            sbyte icicleSecondFreeSlot = -1; // -1 means no free slot

            for (sbyte lI = 0; lI < 5; ++lI)
                if (!player.HasAura(IcicleAuras[lI]))
                {
                    icicleFreeSlot = lI;

                    if (icilesAddSecond && icicleFreeSlot != 5)
                        icicleSecondFreeSlot = (sbyte)(lI + 1);

                    break;
                }

            if (icicleFreeSlot == -1)
            {
                // We need to find the icicle with the smallest duration.
                sbyte smallestIcicle = 0;
                var minDuration = 0xFFFFFF;

                for (sbyte i = 0; i < 5; i++)
                {
                    var tmpCurrentAura = player.GetAura(IcicleAuras[i]);

                    if (tmpCurrentAura != null)
                        if (minDuration > tmpCurrentAura.Duration)
                        {
                            minDuration = tmpCurrentAura.Duration;
                            smallestIcicle = i;
                        }
                }

                // Launch the icicle with the smallest duration
                var currentIcicleAuraEffect = player.GetAuraEffect(IcicleAuras[smallestIcicle], 0);

                if (currentIcicleAuraEffect != null)
                {
                    var basePoints = currentIcicleAuraEffect.Amount;

                    if (caster.HasAura(MageSpells.BLACK_ICE))
                        if (RandomHelper.randChance(20))
                            basePoints *= 2;

                    player.SpellFactory.CastSpell(target, IcicleHits[smallestIcicle], true);
                    player.SpellFactory.CastSpell(target, MageSpells.ICICLE_DAMAGE, new CastSpellExtraArgs(SpellValueMod.BasePoint0, basePoints));
                    player.RemoveAura(IcicleAuras[smallestIcicle]);
                }

                icicleFreeSlot = smallestIcicle;
                // No break because we'll add the icicle in the next case
            }

            switch (icicleFreeSlot)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                {
                    var currentIcicleAura = player.AddAura(IcicleAuras[icicleFreeSlot], player);

                    if (currentIcicleAura != null)
                    {
                        var effect = currentIcicleAura.GetEffect(0);

                        if (effect != null)
                            effect.SetAmount(hitDamage);

                        player.AddAura(MageSpells.ICICLE_AURA, player);

                        if (caster.HasSpell(MageSpells.GLACIAL_SPIKE))
                        {
                            var glacialSpikeProc = player.GetAura(MageSpells.ICICLE_AURA);

                            if (glacialSpikeProc != null)
                                if (glacialSpikeProc.StackAmount == 5)
                                    player.SpellFactory.CastSpell(player, MageSpells.GLACIAL_SPIKE_PROC, true);
                        }
                    }

                    break;
                }
            }

            if (icicleSecondFreeSlot == -1 && icilesAddSecond)
            {
                // We need to find the icicle with the smallest duration.
                sbyte smallestIcicle = 0;
                var minDuration = 0xFFFFFF;

                for (sbyte i = 0; i < 5; i++)
                {
                    var tmpCurrentAura = player.GetAura(IcicleAuras[i]);

                    if (tmpCurrentAura != null)
                        if (minDuration > tmpCurrentAura.Duration)
                        {
                            minDuration = tmpCurrentAura.Duration;
                            smallestIcicle = i;
                        }
                }

                // Launch the icicle with the smallest duration
                var currentIcicleAuraEffect = player.GetAuraEffect(IcicleAuras[smallestIcicle], 0);

                if (currentIcicleAuraEffect != null)
                {
                    var basePoints = currentIcicleAuraEffect.Amount;

                    if (caster.HasAura(MageSpells.BLACK_ICE))
                        if (RandomHelper.randChance(20))
                            basePoints *= 2;

                    player.SpellFactory.CastSpell(target, IcicleHits[smallestIcicle], true);
                    player.SpellFactory.CastSpell(target, MageSpells.ICICLE_DAMAGE, new CastSpellExtraArgs(SpellValueMod.BasePoint0, basePoints));
                    player.RemoveAura(IcicleAuras[smallestIcicle]);
                }

                icicleSecondFreeSlot = smallestIcicle;
                // No break because we'll add the icicle in the next case
            }

            switch (icicleSecondFreeSlot)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                {
                    var currentIcicleAura = player.AddAura(IcicleAuras[icicleSecondFreeSlot], player);

                    if (currentIcicleAura != null)
                    {
                        var effect = currentIcicleAura.GetEffect(0);

                        if (effect != null)
                            effect.SetAmount(hitDamage);

                        player.AddAura(MageSpells.ICICLE_AURA, player);

                        if (caster.HasSpell(MageSpells.GLACIAL_SPIKE))
                        {
                            var glacialSpikeProc = player.GetAura(MageSpells.ICICLE_AURA);

                            if (glacialSpikeProc != null)
                                if (glacialSpikeProc.StackAmount == 5)
                                    player.SpellFactory.CastSpell(player, MageSpells.GLACIAL_SPIKE_PROC, true);
                        }
                    }

                    break;
                }
            }
        }
    }
}