// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(118455)]
public class spell_hun_beast_cleave_proc : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        if (!Caster)
            return;

        if (eventInfo.Actor.GUID != Target.GUID)
            return;

        if (eventInfo.DamageInfo.SpellInfo != null && eventInfo.DamageInfo.SpellInfo.Id == HunterSpells.BEAST_CLEAVE_DAMAGE)
            return;

        var player = Caster.AsPlayer;

        if (player != null)
            if (Target.HasAura(aurEff.SpellInfo.Id, player.GUID))
            {
                var args = new CastSpellExtraArgs(TriggerCastFlags.FullMask);
                args.AddSpellMod(SpellValueMod.BasePoint0, eventInfo.DamageInfo.Damage * 0.75f);

                Target.CastSpell(Target, HunterSpells.BEAST_CLEAVE_DAMAGE, args);
            }
    }
}