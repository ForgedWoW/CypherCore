// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script("spell_item_unstable_power", ItemSpellIds.UNSTABLE_POWER_AURA_STACK)]
[Script("spell_item_restless_strength", ItemSpellIds.RESTLESS_STRENGTH_AURA_STACK)]
internal class SpellItemZandalarianCharm : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    private readonly uint _spellId;

    public SpellItemZandalarianCharm(uint spellId)
    {
        _spellId = spellId;
    }

    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var spellInfo = eventInfo.SpellInfo;

        if (spellInfo != null)
            if (spellInfo.Id != ScriptSpellId)
                return true;

        return false;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleStackDrop, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleStackDrop(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        Target.RemoveAuraFromStack(_spellId);
    }
}