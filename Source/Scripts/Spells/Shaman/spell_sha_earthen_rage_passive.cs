// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 170374 - Earthen Rage (Passive)
[SpellScript(170374)]
public class SpellShaEarthenRagePassive : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    private ObjectGuid _procTargetGuid;

    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo procInfo)
    {
        return procInfo.SpellInfo != null && procInfo.SpellInfo.Id != ShamanSpells.EarthenRageDamage;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    public ObjectGuid GetProcTargetGuid()
    {
        return _procTargetGuid;
    }

    private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        _procTargetGuid = eventInfo.ProcTarget.GUID;
        eventInfo.Actor.SpellFactory.CastSpell(eventInfo.Actor, ShamanSpells.EarthenRagePeriodic, true);
    }
}