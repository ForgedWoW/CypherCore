// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Chrono;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenTurkeyMarker : AuraScript, IHasAuraEffects
{
    private readonly List<uint> _applyTimes = new();
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectAfterApply));
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicDummy));
    }

    private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        // store stack apply times, so we can pop them while they expire
        _applyTimes.Add(GameTime.GetGameTimeMS());
        var target = Target;

        // on stack 15 cast the Achievement crediting spell
        if (StackAmount >= 15)
            target.SpellFactory.CastSpell(target, GenericSpellIds.TURKEY_VENGEANCE, new CastSpellExtraArgs(aurEff).SetOriginalCaster(CasterGUID));
    }

    private void OnPeriodic(AuraEffect aurEff)
    {
        var removeCount = 0;

        // pop expired times off of the stack
        while (!_applyTimes.Empty() && _applyTimes.FirstOrDefault() + MaxDuration < GameTime.GetGameTimeMS())
        {
            _applyTimes.RemoveAt(0);
            removeCount++;
        }

        if (removeCount != 0)
            ModStackAmount(-removeCount, AuraRemoveMode.Expire);
    }
}