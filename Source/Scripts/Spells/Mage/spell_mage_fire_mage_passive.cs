// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[SpellScript(137019)]
public class SpellMageFireMagePassive : AuraScript, IHasAuraEffects
{
    private readonly SpellModifier _mod = null;

    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 4, AuraType.Dummy, AuraEffectHandleModes.Real));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 4, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void HandleApply(AuraEffect aurEffect, AuraEffectHandleModes unnamedParameter)
    {
        var player = Caster.AsPlayer;

        if (player == null)
            return;

        var mod = new SpellModifierByClassMask(aurEffect.Base);
        mod.Op = SpellModOp.CritChance;
        mod.Type = SpellModType.Flat;
        mod.SpellId = MageSpells.FIRE_MAGE_PASSIVE;
        mod.Value = 200;
        mod.Mask[0] = 0x2;

        player.AddSpellMod(mod, true);
    }

    private void HandleRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var player = Caster.AsPlayer;

        if (player == null)
            return;

        if (_mod != null)
            player.AddSpellMod(_mod, false);
    }
}