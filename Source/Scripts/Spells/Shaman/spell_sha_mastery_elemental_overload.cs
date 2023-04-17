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

// 168534 - Mastery: Elemental Overload (passive)
[SpellScript(168534)]
internal class SpellShaMasteryElementalOverload : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProc, 0, AuraType.Dummy));
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        var spellInfo = eventInfo.SpellInfo;

        if (spellInfo == null ||
            !eventInfo.ProcSpell)
            return false;

        if (GetTriggeredSpellId(spellInfo.Id) == 0)
            return false;

        var chance = aurEff.Amount; // Mastery % amount

        if (spellInfo.Id == ShamanSpells.ChainLightning)
            chance /= 3.0f;

        var stormkeeper = eventInfo.Actor.GetAura(ShamanSpells.STORMKEEPER);

        if (stormkeeper != null)
            if (eventInfo.ProcSpell.AppliedMods.Contains(stormkeeper))
                chance = 100.0f;

        return RandomHelper.randChance(chance);
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        PreventDefaultAction();

        var caster = procInfo.Actor;

        var targets = new CastSpellTargetArg(procInfo.ProcTarget);
        var overloadSpellId = GetTriggeredSpellId(procInfo.SpellInfo.Id);
        var originalCastId = procInfo.ProcSpell.CastId;

        caster.Events.AddEventAtOffset(() =>
                                       {
                                           if (targets.Targets == null)
                                               return;

                                           targets.Targets.Update(caster);

                                           CastSpellExtraArgs args = new();
                                           args.OriginalCastId = originalCastId;
                                           caster.SpellFactory.CastSpell(targets, overloadSpellId, args);
                                       },
                                       TimeSpan.FromMilliseconds(400));
    }

    private uint GetTriggeredSpellId(uint triggeringSpellId)
    {
        switch (triggeringSpellId)
        {
            case ShamanSpells.LIGHTNING_BOLT:
                return ShamanSpells.LIGHTNING_BOLT_OVERLOAD;
            case ShamanSpells.ElementalBlast:
                return ShamanSpells.ELEMENTAL_BLAST_OVERLOAD;
            case ShamanSpells.ICEFURY:
                return ShamanSpells.ICEFURY_OVERLOAD;
            case ShamanSpells.LavaBurst:
                return ShamanSpells.LAVA_BURST_OVERLOAD;
            case ShamanSpells.ChainLightning:
                return ShamanSpells.CHAIN_LIGHTNING_OVERLOAD;
            case ShamanSpells.LAVA_BEAM:
                return ShamanSpells.LAVA_BEAM_OVERLOAD;
        }

        return 0;
    }
}