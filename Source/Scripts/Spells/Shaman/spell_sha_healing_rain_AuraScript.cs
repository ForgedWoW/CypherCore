// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 73920 - Healing Rain (Aura)
[SpellScript(73920)]
internal class SpellShaHealingRainAuraScript : AuraScript, IHasAuraEffects
{
    private ObjectGuid _visualDummy;
    private Position _pos;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public void SetVisualDummy(TempSummon summon)
    {
        _visualDummy = summon.GUID;
        _pos = summon.Location;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleEffecRemoved, 1, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 1, AuraType.PeriodicDummy));
    }

    private void HandleEffectPeriodic(AuraEffect aurEff)
    {
        Target.SpellFactory.CastSpell(_pos, ShamanSpells.HEALING_RAIN_HEAL, new CastSpellExtraArgs(aurEff));
    }

    private void HandleEffecRemoved(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var summon = ObjectAccessor.GetCreature(Target, _visualDummy);

        summon?.DespawnOrUnsummon();
    }
}