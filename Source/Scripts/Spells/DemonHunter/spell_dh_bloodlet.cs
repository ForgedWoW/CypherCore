// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(206473)]
public class SpellDhBloodlet : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.SpellInfo.Id == DemonHunterSpells.THROW_GLAIVE)
            return true;

        return false;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect unnamedParameter, ProcEventInfo eventInfo)
    {
        var caster = Caster;
        var target = eventInfo.ActionTarget;

        if (caster == null || target == null || eventInfo.DamageInfo != null || !SpellInfo.GetEffect(0).IsEffect())
            return;

        var basePoints = SpellInfo.GetEffect(0).BasePoints;
        var dmg = (eventInfo.DamageInfo.Damage * (double)basePoints) / 100.0f;
        var dmgPerTick = (double)dmg / 5.0f;

        // Any remaining damage must be added
        var dot = target.GetAuraEffect(DemonHunterSpells.BLOODLET_DOT, 0, caster.GUID);

        if (dot != null)
            dmgPerTick += (dot.Amount * (dot.GetTotalTicks() - dot.GetTickNumber())) / 5;

        var args = new CastSpellExtraArgs();
        args.AddSpellMod(SpellValueMod.BasePoint0, (int)dmgPerTick);
        args.SetTriggerFlags(TriggerCastFlags.FullMask);
        caster.SpellFactory.CastSpell(target, DemonHunterSpells.BLOODLET_DOT, args);
    }
}