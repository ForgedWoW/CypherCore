// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;
using Serilog;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenMixologyBonus : AuraScript, IHasAuraEffects
{
    private double _bonus;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override bool Load()
    {
        return Caster && Caster.TypeId == TypeId.Player;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, SpellConst.EffectAll, AuraType.Any));
    }

    private void SetBonusValueForEffect(uint effIndex, int value, AuraEffect aurEff)
    {
        if (aurEff.EffIndex == effIndex)
            _bonus = value;
    }

    private void CalculateAmount(AuraEffect aurEff, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        if (Caster.HasAura((uint)RequiredMixologySpells.Mixology) &&
            Caster.HasSpell(GetEffectInfo(0).TriggerSpell))
        {
            switch ((RequiredMixologySpells)Id)
            {
                case RequiredMixologySpells.WeakTrollsBloodElixir:
                case RequiredMixologySpells.MagebloodElixir:
                    _bonus = amount;

                    break;
                case RequiredMixologySpells.ElixirOfFrostPower:
                case RequiredMixologySpells.LesserFlaskOfToughness:
                case RequiredMixologySpells.LesserFlaskOfResistance:
                    _bonus = MathFunctions.CalculatePct(amount, 80);

                    break;
                case RequiredMixologySpells.ElixirOfMinorDefense:
                case RequiredMixologySpells.ElixirOfLionsStrength:
                case RequiredMixologySpells.ElixirOfMinorAgility:
                case RequiredMixologySpells.MajorTrollsBlloodElixir:
                case RequiredMixologySpells.ElixirOfShadowPower:
                case RequiredMixologySpells.ElixirOfBruteForce:
                case RequiredMixologySpells.MightyTrollsBloodElixir:
                case RequiredMixologySpells.ElixirOfGreaterFirepower:
                case RequiredMixologySpells.OnslaughtElixir:
                case RequiredMixologySpells.EarthenElixir:
                case RequiredMixologySpells.ElixirOfMajorAgility:
                case RequiredMixologySpells.FlaskOfTheTitans:
                case RequiredMixologySpells.FlaskOfRelentlessAssault:
                case RequiredMixologySpells.FlaskOfStoneblood:
                case RequiredMixologySpells.ElixirOfMinorAccuracy:
                    _bonus = MathFunctions.CalculatePct(amount, 50);

                    break;
                case RequiredMixologySpells.ElixirOfProtection:
                    _bonus = 280;

                    break;
                case RequiredMixologySpells.ElixirOfMajorDefense:
                    _bonus = 200;

                    break;
                case RequiredMixologySpells.ElixirOfGreaterDefense:
                case RequiredMixologySpells.ElixirOfSuperiorDefense:
                    _bonus = 140;

                    break;
                case RequiredMixologySpells.ElixirOfFortitude:
                    _bonus = 100;

                    break;
                case RequiredMixologySpells.FlaskOfEndlessRage:
                    _bonus = 82;

                    break;
                case RequiredMixologySpells.ElixirOfDefense:
                    _bonus = 70;

                    break;
                case RequiredMixologySpells.ElixirOfDemonslaying:
                    _bonus = 50;

                    break;
                case RequiredMixologySpells.FlaskOfTheFrostWyrm:
                    _bonus = 47;

                    break;
                case RequiredMixologySpells.WrathElixir:
                    _bonus = 32;

                    break;
                case RequiredMixologySpells.ElixirOfMajorFrostPower:
                case RequiredMixologySpells.ElixirOfMajorFirepower:
                case RequiredMixologySpells.ElixirOfMajorShadowPower:
                    _bonus = 29;

                    break;
                case RequiredMixologySpells.ElixirOfMightyToughts:
                    _bonus = 27;

                    break;
                case RequiredMixologySpells.FlaskOfSupremePower:
                case RequiredMixologySpells.FlaskOfBlindingLight:
                case RequiredMixologySpells.FlaskOfPureDeath:
                case RequiredMixologySpells.ShadowpowerElixir:
                    _bonus = 23;

                    break;
                case RequiredMixologySpells.ElixirOfMightyAgility:
                case RequiredMixologySpells.FlaskOfDistilledWisdom:
                case RequiredMixologySpells.ElixirOfSpirit:
                case RequiredMixologySpells.ElixirOfMightyStrength:
                case RequiredMixologySpells.FlaskOfPureMojo:
                case RequiredMixologySpells.ElixirOfAccuracy:
                case RequiredMixologySpells.ElixirOfDeadlyStrikes:
                case RequiredMixologySpells.ElixirOfMightyDefense:
                case RequiredMixologySpells.ElixirOfExpertise:
                case RequiredMixologySpells.ElixirOfArmorPiercing:
                case RequiredMixologySpells.ElixirOfLightningSpeed:
                    _bonus = 20;

                    break;
                case RequiredMixologySpells.FlaskOfChromaticResistance:
                    _bonus = 17;

                    break;
                case RequiredMixologySpells.ElixirOfMinorFortitude:
                case RequiredMixologySpells.ElixirOfMajorStrength:
                    _bonus = 15;

                    break;
                case RequiredMixologySpells.FlaskOfMightyRestoration:
                    _bonus = 13;

                    break;
                case RequiredMixologySpells.ArcaneElixir:
                    _bonus = 12;

                    break;
                case RequiredMixologySpells.ElixirOfGreaterAgility:
                case RequiredMixologySpells.ElixirOfGiants:
                    _bonus = 11;

                    break;
                case RequiredMixologySpells.ElixirOfAgility:
                case RequiredMixologySpells.ElixirOfGreaterIntellect:
                case RequiredMixologySpells.ElixirOfSages:
                case RequiredMixologySpells.ElixirOfIronskin:
                case RequiredMixologySpells.ElixirOfMightyMageblood:
                    _bonus = 10;

                    break;
                case RequiredMixologySpells.ElixirOfHealingPower:
                    _bonus = 9;

                    break;
                case RequiredMixologySpells.ElixirOfDraenicWisdom:
                case RequiredMixologySpells.GurusElixir:
                    _bonus = 8;

                    break;
                case RequiredMixologySpells.ElixirOfFirepower:
                case RequiredMixologySpells.ElixirOfMajorMageblood:
                case RequiredMixologySpells.ElixirOfMastery:
                    _bonus = 6;

                    break;
                case RequiredMixologySpells.ElixirOfLesserAgility:
                case RequiredMixologySpells.ElixirOfOgresStrength:
                case RequiredMixologySpells.ElixirOfWisdom:
                case RequiredMixologySpells.ElixirOfTheMongoose:
                    _bonus = 5;

                    break;
                case RequiredMixologySpells.StrongTrollsBloodElixir:
                case RequiredMixologySpells.FlaskOfChromaticWonder:
                    _bonus = 4;

                    break;
                case RequiredMixologySpells.ElixirOfEmpowerment:
                    _bonus = -10;

                    break;
                case RequiredMixologySpells.AdeptsElixir:
                    SetBonusValueForEffect(0, 13, aurEff);
                    SetBonusValueForEffect(1, 13, aurEff);
                    SetBonusValueForEffect(2, 8, aurEff);

                    break;
                case RequiredMixologySpells.ElixirOfMightyFortitude:
                    SetBonusValueForEffect(0, 160, aurEff);

                    break;
                case RequiredMixologySpells.ElixirOfMajorFortitude:
                    SetBonusValueForEffect(0, 116, aurEff);
                    SetBonusValueForEffect(1, 6, aurEff);

                    break;
                case RequiredMixologySpells.FelStrengthElixir:
                    SetBonusValueForEffect(0, 40, aurEff);
                    SetBonusValueForEffect(1, 40, aurEff);

                    break;
                case RequiredMixologySpells.FlaskOfFortification:
                    SetBonusValueForEffect(0, 210, aurEff);
                    SetBonusValueForEffect(1, 5, aurEff);

                    break;
                case RequiredMixologySpells.GreaterArcaneElixir:
                    SetBonusValueForEffect(0, 19, aurEff);
                    SetBonusValueForEffect(1, 19, aurEff);
                    SetBonusValueForEffect(2, 5, aurEff);

                    break;
                case RequiredMixologySpells.ElixirOfGianthGrowth:
                    SetBonusValueForEffect(0, 5, aurEff);

                    break;
                default:
                    Log.Logger.Error("SpellId {0} couldn't be processed in spell_gen_mixology_bonus", Id);

                    break;
            }

            amount.Value += _bonus;
        }
    }
}