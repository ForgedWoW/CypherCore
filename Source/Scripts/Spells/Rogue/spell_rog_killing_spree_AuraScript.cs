// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[Script]
internal class spell_rog_killing_spree_AuraScript : AuraScript, IHasAuraEffects
{
    private readonly List<ObjectGuid> _targets = new();
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    public void AddTarget(Unit target)
    {
        _targets.Add(target.GUID);
    }

    private void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Target.CastSpell(Target, RogueSpells.KillingSpreeDmgBuff, true);
    }

    private void HandleEffectPeriodic(AuraEffect aurEff)
    {
        while (!_targets.Empty())
        {
            var guid = _targets.SelectRandom();
            var target = Global.ObjAccessor.GetUnit(Target, guid);

            if (target != null)
            {
                Target.CastSpell(target, RogueSpells.KillingSpreeTeleport, true);
                Target.CastSpell(target, RogueSpells.KillingSpreeWeaponDmg, true);

                break;
            }
            else
            {
                _targets.Remove(guid);
            }
        }
    }

    private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Target.RemoveAura(RogueSpells.KillingSpreeDmgBuff);
    }
}