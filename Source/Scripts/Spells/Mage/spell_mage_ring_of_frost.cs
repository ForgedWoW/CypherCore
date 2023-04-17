// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[Script] // 136511 - Ring of Frost
internal class SpellMageRingOfFrost : AuraScript, IHasAuraEffects
{
    private ObjectGuid _ringOfFrostGUID;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.ProcTriggerSpell));
        AuraEffects.Add(new AuraEffectApplyHandler(Apply, 0, AuraType.ProcTriggerSpell, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectApply));
    }

    private void HandleEffectPeriodic(AuraEffect aurEff)
    {
        var ringOfFrost = GetRingOfFrostMinion();

        if (ringOfFrost)
            Target.SpellFactory.CastSpell(ringOfFrost.Location, MageSpells.RingOfFrostFreeze, new CastSpellExtraArgs(true));
    }

    private void Apply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        List<TempSummon> minions = new();
        Target.GetAllMinionsByEntry(minions, (uint)Global.SpellMgr.GetSpellInfo(MageSpells.RING_OF_FROST_SUMMON, CastDifficulty).GetEffect(0).MiscValue);

        // Get the last summoned RoF, save it and despawn older ones
        foreach (var summon in minions)
        {
            var ringOfFrost = GetRingOfFrostMinion();

            if (ringOfFrost)
            {
                if (summon.GetTimer() > ringOfFrost.GetTimer())
                {
                    ringOfFrost.DespawnOrUnsummon();
                    _ringOfFrostGUID = summon.GUID;
                }
                else
                    summon.DespawnOrUnsummon();
            }
            else
                _ringOfFrostGUID = summon.GUID;
        }
    }

    private TempSummon GetRingOfFrostMinion()
    {
        var creature = ObjectAccessor.GetCreature(Owner, _ringOfFrostGUID);

        if (creature)
            return creature.ToTempSummon();

        return null;
    }
}