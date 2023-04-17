// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 205178 - Soul Effigy target
[SpellScript(205178)]
public class SpellWarlockSoulEffigyTarget : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.Dummy));
    }

    private void PeriodicTick(AuraEffect unnamedParameter)
    {
        var caster = Caster;

        if (caster == null)
            return;

        if (!caster.VariableStorage.Exist("Spells.SoulEffigyGuid"))
        {
            Remove();

            return;
        }

        var guid = caster.VariableStorage.GetValue<ObjectGuid>("Spells.SoulEffigyGuid", ObjectGuid.Empty);

        if (!ObjectAccessor.Instance.GetUnit(caster, guid))
            Remove();
    }

    private void OnRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster == null)
            return;

        var guid = caster.VariableStorage.GetValue<ObjectGuid>("Spells.SoulEffigyGuid", ObjectGuid.Empty);

        var effigy = ObjectAccessor.Instance.GetUnit(caster, guid);

        if (effigy != null)
            effigy.ToTempSummon().DespawnOrUnsummon();
    }

    private void OnApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;
        var target = Target;

        if (caster == null || target == null)
            return;

        caster.VariableStorage.Set("Spells.SoulEffigyTargetGuid", target.GUID);
    }
}