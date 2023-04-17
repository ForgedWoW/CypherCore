// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 205247 - Soul Effigy aura
[SpellScript(205247)]
public class SpellWarlockSoulEffigyAura : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.PeriodicDummy, AuraScriptHookType.EffectProc));
    }

    private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        var caster = Caster;

        if (caster == null)
            return;

        var owner = caster.ToTempSummon().GetSummoner();

        if (owner == null)
            return;

        if (eventInfo.SpellInfo != null && eventInfo.SpellInfo.IsPositive)
            return;

        var damage = MathFunctions.CalculatePct(eventInfo.DamageInfo.Damage, aurEff.Amount);

        if (damage == 0)
            return;

        var guid = owner.VariableStorage.GetValue<ObjectGuid>("Spells.SoulEffigyTargetGuid", ObjectGuid.Empty);

        var target = ObjectAccessor.Instance.GetUnit(owner, guid);

        if (target != null)
        {
            caster.SpellFactory.CastSpell(target, WarlockSpells.SOUL_EFFIGY_VISUAL, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(owner.GUID));
            var targetGuid = target.GUID;
            var ownerGuid = owner.GUID;

            //C++ TO C# CONVERTER TASK: Only lambdas having all locals passed by reference can be converted to C#:
            //ORIGINAL LINE: caster->GetScheduler().Schedule(750ms, [caster, targetGuid, damage, ownerGuid](TaskContext)
            caster.Events.AddEvent(() =>
                                   {
                                       var target = ObjectAccessor.Instance.GetUnit(caster, targetGuid);
                                       var owner = ObjectAccessor.Instance.GetUnit(caster, ownerGuid);

                                       if (target == null || owner == null)
                                           return;

                                       var args = new CastSpellExtraArgs(TriggerCastFlags.FullMask);
                                       caster.SpellFactory.CastSpell(target, WarlockSpells.SOUL_EFFIGY_DAMAGE, new CastSpellExtraArgs(SpellValueMod.BasePoint0, 0).SetTriggerFlags(TriggerCastFlags.FullMask).SetOriginalCaster(owner.GUID));
                                   },
                                   TimeSpan.FromMilliseconds(750));
        }
    }
}