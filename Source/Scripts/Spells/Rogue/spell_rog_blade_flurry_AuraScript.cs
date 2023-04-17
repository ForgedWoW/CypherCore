// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[Script] // 13877, 33735, (check 51211, 65956) - Blade Flurry
internal class SpellRogBladeFlurryAuraScript : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    private Unit _procTarget = null;

    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        _procTarget = Target.SelectNearbyTarget(eventInfo.ProcTarget);

        return _procTarget != null && eventInfo.DamageInfo != null;
    }

    public override void Register()
    {
        if (ScriptSpellId == RogueSpells.BladeFlurry)
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ModPowerRegenPercent, AuraScriptHookType.EffectProc));
        else
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ModMeleeHaste, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        var damageInfo = eventInfo.DamageInfo;

        if (damageInfo != null)
        {
            CastSpellExtraArgs args = new(aurEff);
            args.AddSpellMod(SpellValueMod.BasePoint0, (int)damageInfo.Damage);
            Target.SpellFactory.CastSpell(_procTarget, RogueSpells.BladeFlurryExtraAttack, args);
        }
    }
}