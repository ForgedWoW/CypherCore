// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[Script] // 70664 - Druid T10 Restoration 4P Bonus (Rejuvenation)
internal class SpellDruT10Restoration4PBonusDummy : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var spellInfo = eventInfo.SpellInfo;

        if (spellInfo == null ||
            spellInfo.Id == DruidSpellIds.RejuvenationT10Proc)
            return false;

        var healInfo = eventInfo.HealInfo;

        if (healInfo == null ||
            healInfo.Heal == 0)
            return false;

        var caster = eventInfo.Actor.AsPlayer;

        if (!caster)
            return false;

        return caster.Group || caster != eventInfo.ProcTarget;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        var amount = (int)eventInfo.HealInfo.Heal;
        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, (int)eventInfo.HealInfo.Heal);
        eventInfo.Actor.SpellFactory.CastSpell((Unit)null, DruidSpellIds.RejuvenationT10Proc, args);
    }
}