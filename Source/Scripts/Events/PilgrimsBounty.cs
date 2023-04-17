// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.m_Events.PilgrimsBounty;

internal struct SpellIds
{
    //Pilgrims Bounty
    public const uint WELL_FED_AP_TRIGGER = 65414;
    public const uint WELL_FED_ZM_TRIGGER = 65412;
    public const uint WELL_FED_HIT_TRIGGER = 65416;
    public const uint WELL_FED_HASTE_TRIGGER = 65410;
    public const uint WELL_FED_SPIRIT_TRIGGER = 65415;

    //FeastOnSpells
    //Feastonspells
    public const uint FEAST_ON_TURKEY = 61784;
    public const uint FEAST_ON_CRANBERRIES = 61785;
    public const uint FEAST_ON_SWEET_POTATOES = 61786;
    public const uint FEAST_ON_PIE = 61787;
    public const uint FEAST_ON_STUFFING = 61788;
    public const uint CRANBERRY_HELPINS = 61841;
    public const uint TURKEY_HELPINS = 61842;
    public const uint STUFFING_HELPINS = 61843;
    public const uint SWEET_POTATO_HELPINS = 61844;
    public const uint PIE_HELPINS = 61845;
    public const uint ON_PLATE_EAT_VISUAL = 61826;

    //Theturkinator
    public const uint KILL_COUNTER_VISUAL = 62015;
    public const uint KILL_COUNTER_VISUAL_MAX = 62021;

    //Spiritofsharing
    public const uint THE_SPIRIT_OF_SHARING = 61849;

    //Bountifultablemisc
    public const uint ON_PLATE_TURKEY = 61928;
    public const uint ON_PLATE_CRANBERRIES = 61925;
    public const uint ON_PLATE_STUFFING = 61927;
    public const uint ON_PLATE_SWEET_POTATOES = 61929;
    public const uint ON_PLATE_PIE = 61926;
    public const uint PASS_THE_TURKEY = 66373;
    public const uint PASS_THE_CRANBERRIES = 66372;
    public const uint PASS_THE_STUFFING = 66375;
    public const uint PASS_THE_SWEET_POTATOES = 66376;
    public const uint PASS_THE_PIE = 66374;
    public const uint ON_PLATE_VISUAL_PIE = 61825;
    public const uint ON_PLATE_VISUAL_CRANBERRIES = 61821;
    public const uint ON_PLATE_VISUAL_POTATOES = 61824;
    public const uint ON_PLATE_VISUAL_TURKEY = 61822;
    public const uint ON_PLATE_VISUAL_STUFFING = 61823;
    public const uint A_SERVING_OF_CRANBERRIES_PLATE = 61833;
    public const uint A_SERVING_OF_TURKEY_PLATE = 61835;
    public const uint A_SERVING_OF_STUFFING_PLATE = 61836;
    public const uint A_SERVING_OF_SWEET_POTATOES_PLATE = 61837;
    public const uint A_SERVING_OF_PIE_PLATE = 61838;
    public const uint A_SERVING_OF_CRANBERRIES_CHAIR = 61804;
    public const uint A_SERVING_OF_TURKEY_CHAIR = 61807;
    public const uint A_SERVING_OF_STUFFING_CHAIR = 61806;
    public const uint A_SERVING_OF_SWEET_POTATOES_CHAIR = 61808;
    public const uint A_SERVING_OF_PIE_CHAIR = 61805;
}

internal struct CreatureIds
{
    //BountifulTableMisc
    public const uint BOUNTIFUL_TABLE = 32823;
}

internal struct EmoteIds
{
    //TheTurkinator
    public const uint TURKEY_HUNTER = 0;
    public const uint TURKEY_DOMINATION = 1;
    public const uint TURKEY_SLAUGHTER = 2;
    public const uint TURKEY_TRIUMPH = 3;
}

internal struct SeatIds
{
    //BountifulTableMisc
    public const sbyte PLAYER = 0;
    public const sbyte PLATE_HOLDER = 6;
}

[Script("spell_gen_slow_roasted_turkey", SpellIds.WELL_FED_AP_TRIGGER)]
[Script("spell_gen_cranberry_chutney", SpellIds.WELL_FED_ZM_TRIGGER)]
[Script("spell_gen_spice_bread_stuffing", SpellIds.WELL_FED_HIT_TRIGGER)]
[Script("spell_gen_pumpkin_pie", SpellIds.WELL_FED_SPIRIT_TRIGGER)]
[Script("spell_gen_candied_sweet_potato", SpellIds.WELL_FED_HASTE_TRIGGER)]
internal class SpellPilgrimsBountyBuffFood : AuraScript, IHasAuraEffects
{
    private readonly uint _triggeredSpellId;

    private bool _handled;

    public SpellPilgrimsBountyBuffFood(uint triggeredSpellId)
    {
        _triggeredSpellId = triggeredSpellId;
        _handled = false;
    }

    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleTriggerSpell, 2, AuraType.PeriodicTriggerSpell));
    }

    private void HandleTriggerSpell(AuraEffect aurEff)
    {
        PreventDefaultAction();

        if (_handled)
            return;

        _handled = true;
        Target.SpellFactory.CastSpell(Target, _triggeredSpellId, true);
    }
}

/* 61784 - Feast On Turkey
 * 61785 - Feast On Cranberries
 * 61786 - Feast On Sweet Potatoes
 * 61787 - Feast On Pie
 * 61788 - Feast On Stuffing */
[Script]
internal class SpellPilgrimsBountyFeastOnSpellScript : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;

        uint spellId = 0;

        switch (SpellInfo.Id)
        {
            case SpellIds.FEAST_ON_TURKEY:
                spellId = SpellIds.TURKEY_HELPINS;

                break;
            case SpellIds.FEAST_ON_CRANBERRIES:
                spellId = SpellIds.CRANBERRY_HELPINS;

                break;
            case SpellIds.FEAST_ON_SWEET_POTATOES:
                spellId = SpellIds.SWEET_POTATO_HELPINS;

                break;
            case SpellIds.FEAST_ON_PIE:
                spellId = SpellIds.PIE_HELPINS;

                break;
            case SpellIds.FEAST_ON_STUFFING:
                spellId = SpellIds.STUFFING_HELPINS;

                break;
            default:
                return;
        }

        var vehicle = caster.VehicleKit1;

        if (vehicle != null)
        {
            var target = vehicle.GetPassenger(0);

            if (target != null)
            {
                var player = target.AsPlayer;

                if (player != null)
                {
                    player.SpellFactory.CastSpell(player, SpellIds.ON_PLATE_EAT_VISUAL, true);

                    caster.SpellFactory.CastSpell(player,
                                                  spellId,
                                                  new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                                      .SetOriginalCaster(player.GUID));
                }
            }
        }

        var aura = caster.GetAura((uint)EffectValue);

        if (aura != null)
        {
            if (aura.StackAmount == 1)
                caster.RemoveAura((uint)aura.SpellInfo.GetEffect(0).CalcValue());

            aura.ModStackAmount(-1);
        }
    }
}

[Script] // 62014 - Turkey Tracker
internal class SpellPilgrimsBountyTurkeyTrackerSpellScript : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var caster = Caster.AsCreature;
        var target = HitUnit;

        if (target == null ||
            caster == null)
            return;

        if (target.HasAura(SpellIds.KILL_COUNTER_VISUAL_MAX))
            return;

        var aura = target.GetAura(SpellInfo.Id);

        if (aura != null)
        {
            switch (aura.StackAmount)
            {
                case 10:
                    caster.AI.Talk(EmoteIds.TURKEY_HUNTER, target);

                    break;
                case 20:
                    caster.AI.Talk(EmoteIds.TURKEY_DOMINATION, target);

                    break;
                case 30:
                    caster.AI.Talk(EmoteIds.TURKEY_SLAUGHTER, target);

                    break;
                case 40:
                    caster.AI.Talk(EmoteIds.TURKEY_TRIUMPH, target);
                    target.SpellFactory.CastSpell(target, SpellIds.KILL_COUNTER_VISUAL_MAX, true);
                    target.RemoveAura(SpellInfo.Id);

                    break;
                default:
                    return;
            }

            target.SpellFactory.CastSpell(target, SpellIds.KILL_COUNTER_VISUAL, true);
        }
    }
}

[Script("spell_pilgrims_bounty_well_fed_turkey", SpellIds.WELL_FED_AP_TRIGGER)]
[Script("spell_pilgrims_bounty_well_fed_cranberry", SpellIds.WELL_FED_ZM_TRIGGER)]
[Script("spell_pilgrims_bounty_well_fed_stuffing", SpellIds.WELL_FED_HIT_TRIGGER)]
[Script("spell_pilgrims_bounty_well_fed_sweet_potatoes", SpellIds.WELL_FED_HASTE_TRIGGER)]
[Script("spell_pilgrims_bounty_well_fed_pie", SpellIds.WELL_FED_SPIRIT_TRIGGER)]
internal class SpellPilgrimsBountyWellFedSpellScript : SpellScript, IHasSpellEffects
{
    private readonly uint _triggeredSpellId;

    public SpellPilgrimsBountyWellFedSpellScript(uint triggeredSpellId)
    {
        _triggeredSpellId = triggeredSpellId;
    }

    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        var target = HitPlayer;

        if (target == null)
            return;

        var aura = target.GetAura(SpellInfo.Id);

        if (aura != null)
            if (aura.StackAmount == 5)
                target.SpellFactory.CastSpell(target, _triggeredSpellId, true);

        var turkey = target.GetAura(SpellIds.TURKEY_HELPINS);
        var cranberies = target.GetAura(SpellIds.CRANBERRY_HELPINS);
        var stuffing = target.GetAura(SpellIds.STUFFING_HELPINS);
        var sweetPotatoes = target.GetAura(SpellIds.SWEET_POTATO_HELPINS);
        var pie = target.GetAura(SpellIds.PIE_HELPINS);

        if ((turkey != null && turkey.StackAmount == 5) &&
            (cranberies != null && cranberies.StackAmount == 5) &&
            (stuffing != null && stuffing.StackAmount == 5) &&
            (sweetPotatoes != null && sweetPotatoes.StackAmount == 5) &&
            (pie != null && pie.StackAmount == 5))
        {
            target.SpellFactory.CastSpell(target, SpellIds.THE_SPIRIT_OF_SHARING, true);
            target.RemoveAura(SpellIds.TURKEY_HELPINS);
            target.RemoveAura(SpellIds.CRANBERRY_HELPINS);
            target.RemoveAura(SpellIds.STUFFING_HELPINS);
            target.RemoveAura(SpellIds.SWEET_POTATO_HELPINS);
            target.RemoveAura(SpellIds.PIE_HELPINS);
        }
    }
}

[Script("spell_pilgrims_bounty_on_plate_turkey", SpellIds.ON_PLATE_TURKEY, SpellIds.PASS_THE_TURKEY, SpellIds.ON_PLATE_VISUAL_TURKEY, SpellIds.A_SERVING_OF_TURKEY_CHAIR)]
[Script("spell_pilgrims_bounty_on_plate_cranberries", SpellIds.ON_PLATE_CRANBERRIES, SpellIds.PASS_THE_CRANBERRIES, SpellIds.ON_PLATE_VISUAL_CRANBERRIES, SpellIds.A_SERVING_OF_CRANBERRIES_CHAIR)]
[Script("spell_pilgrims_bounty_on_plate_stuffing", SpellIds.ON_PLATE_STUFFING, SpellIds.PASS_THE_STUFFING, SpellIds.ON_PLATE_VISUAL_STUFFING, SpellIds.A_SERVING_OF_STUFFING_CHAIR)]
[Script("spell_pilgrims_bounty_on_plate_sweet_potatoes", SpellIds.ON_PLATE_SWEET_POTATOES, SpellIds.PASS_THE_SWEET_POTATOES, SpellIds.ON_PLATE_VISUAL_POTATOES, SpellIds.A_SERVING_OF_SWEET_POTATOES_CHAIR)]
[Script("spell_pilgrims_bounty_on_plate_pie", SpellIds.ON_PLATE_PIE, SpellIds.PASS_THE_PIE, SpellIds.ON_PLATE_VISUAL_PIE, SpellIds.A_SERVING_OF_PIE_CHAIR)]
internal class SpellPilgrimsBountyOnPlateSpellScript : SpellScript, IHasSpellEffects
{
    private readonly uint _triggeredSpellId1;
    private readonly uint _triggeredSpellId2;
    private readonly uint _triggeredSpellId3;
    private readonly uint _triggeredSpellId4;

    public SpellPilgrimsBountyOnPlateSpellScript(uint triggeredSpellId1, uint triggeredSpellId2, uint triggeredSpellId3, uint triggeredSpellId4)
    {
        _triggeredSpellId1 = triggeredSpellId1;
        _triggeredSpellId2 = triggeredSpellId2;
        _triggeredSpellId3 = triggeredSpellId3;
        _triggeredSpellId4 = triggeredSpellId4;
    }

    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private Vehicle GetTable(Unit target)
    {
        if (target.IsPlayer)
        {
            var vehBase = target.VehicleBase;

            if (vehBase != null)
            {
                var table = vehBase.Vehicle1;

                if (table != null)
                    if (table.GetCreatureEntry() == CreatureIds.BOUNTIFUL_TABLE)
                        return table;
            }
        }
        else
        {
            var veh = target.Vehicle1;

            if (veh != null)
                if (veh.GetCreatureEntry() == CreatureIds.BOUNTIFUL_TABLE)
                    return veh;
        }

        return null;
    }

    private Unit GetPlateInSeat(Vehicle table, sbyte seat)
    {
        var holderUnit = table.GetPassenger(SeatIds.PLATE_HOLDER);

        if (holderUnit != null)
        {
            var holder = holderUnit.VehicleKit1;

            if (holder != null)
                return holder.GetPassenger(seat);
        }

        return null;
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;
        var target = HitUnit;

        if (!target ||
            caster == target)
            return;

        var table = GetTable(caster);

        if (!table ||
            table != GetTable(target))
            return;

        var casterChair = caster.VehicleKit1;

        if (casterChair != null)
        {
            var casterPlr = casterChair.GetPassenger(SeatIds.PLAYER);

            if (casterPlr != null)
            {
                if (casterPlr == target)
                    return;

                casterPlr.SpellFactory.CastSpell(casterPlr, _triggeredSpellId2, true); //Credit for Sharing is Caring(always)

                var seat = target.TransSeat;

                if (target.IsPlayer &&
                    target.VehicleBase)
                    seat = target.VehicleBase.TransSeat;

                var plate = GetPlateInSeat(table, seat);

                if (plate != null)
                {
                    if (target.IsPlayer) //Food Fight case
                    {
                        casterPlr.SpellFactory.CastSpell(target, _triggeredSpellId1, true);
                        caster.SpellFactory.CastSpell(target.VehicleBase, _triggeredSpellId4, true); //CanEat-chair(always)
                    }
                    else
                    {
                        casterPlr.SpellFactory.CastSpell(plate, _triggeredSpellId3, true); //Food Visual on plate
                        caster.SpellFactory.CastSpell(target, _triggeredSpellId4, true);   //CanEat-chair(always)
                    }
                }
            }
        }
    }
}

[Script("spell_pilgrims_bounty_a_serving_of_cranberries", SpellIds.A_SERVING_OF_CRANBERRIES_PLATE)]
[Script("spell_pilgrims_bounty_a_serving_of_turkey", SpellIds.A_SERVING_OF_TURKEY_PLATE)]
[Script("spell_pilgrims_bounty_a_serving_of_stuffing", SpellIds.A_SERVING_OF_STUFFING_PLATE)]
[Script("spell_pilgrims_bounty_a_serving_of_potatoes", SpellIds.A_SERVING_OF_SWEET_POTATOES_PLATE)]
[Script("spell_pilgrims_bounty_a_serving_of_pie", SpellIds.A_SERVING_OF_PIE_PLATE)]
internal class SpellPilgrimsBountyAServingOfAuraScript : AuraScript, IHasAuraEffects
{
    private readonly uint _triggeredSpellId;

    public SpellPilgrimsBountyAServingOfAuraScript(uint triggeredSpellId)
    {
        _triggeredSpellId = triggeredSpellId;
    }

    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;
        target.SpellFactory.CastSpell(target, (uint)aurEff.Amount, true);
        HandlePlate(target, true);
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;
        target.RemoveAura((uint)aurEff.Amount);
        HandlePlate(target, false);
    }

    private void HandlePlate(Unit target, bool apply)
    {
        var table = target.Vehicle1;

        if (table != null)
        {
            var holderUnit = table.GetPassenger(SeatIds.PLATE_HOLDER);

            if (holderUnit != null)
            {
                var holder = holderUnit.VehicleKit1;

                if (holder != null)
                {
                    var plate = holder.GetPassenger(target.TransSeat);

                    if (plate != null)
                    {
                        if (apply)
                            target.SpellFactory.CastSpell(plate, _triggeredSpellId, true);
                        else
                            plate.RemoveAura(_triggeredSpellId);
                    }
                }
            }
        }
    }
}