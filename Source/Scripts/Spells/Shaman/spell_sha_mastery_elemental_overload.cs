// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 168534 - Mastery: Elemental Overload (passive)
[SpellScript(168534)]
internal class spell_sha_mastery_elemental_overload : AuraScript, IHasAuraEffects
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

        var stormkeeper = eventInfo.Actor.GetAura(ShamanSpells.Stormkeeper);

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
                                           caster.CastSpell(targets, overloadSpellId, args);
                                       },
                                       TimeSpan.FromMilliseconds(400));
    }

    private uint GetTriggeredSpellId(uint triggeringSpellId)
    {
        switch (triggeringSpellId)
        {
            case ShamanSpells.LightningBolt:
                return ShamanSpells.LightningBoltOverload;
            case ShamanSpells.ElementalBlast:
                return ShamanSpells.ElementalBlastOverload;
            case ShamanSpells.Icefury:
                return ShamanSpells.IcefuryOverload;
            case ShamanSpells.LavaBurst:
                return ShamanSpells.LavaBurstOverload;
            case ShamanSpells.ChainLightning:
                return ShamanSpells.ChainLightningOverload;
            case ShamanSpells.LavaBeam:
                return ShamanSpells.LavaBeamOverload;
            
        }

        return 0;
    }
}