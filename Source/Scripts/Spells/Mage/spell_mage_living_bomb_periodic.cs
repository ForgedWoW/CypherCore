// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 217694 - Living Bomb
internal class spell_mage_living_bomb_periodic : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 2, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (TargetApplication.RemoveMode != AuraRemoveMode.Expire)
            return;

        var caster = Caster;

        if (caster)
            caster.CastSpell(Target, MageSpells.LivingBombExplosion, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, aurEff.Amount));
    }
}