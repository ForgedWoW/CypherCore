// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 71563 - Deadly Precision Dummy
internal class SpellItemDeadlyPrecisionDummy : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHit));
    }

    private void HandleDummy(int effIndex)
    {
        var spellInfo = Global.SpellMgr.GetSpellInfo(ItemSpellIds.DEADLY_PRECISION, CastDifficulty);
        CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
        args.AddSpellMod(SpellValueMod.AuraStack, (int)spellInfo.StackAmount);
        Caster.SpellFactory.CastSpell(Caster, spellInfo.Id, args);
    }
}