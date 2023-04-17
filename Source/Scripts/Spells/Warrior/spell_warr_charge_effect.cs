// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// 198337 - Charge Effect (dropping Blazing Trail)
[Script] // 218104 - Charge Effect
internal class SpellWarrChargeEffect : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleCharge, 0, SpellEffectName.Charge, SpellScriptHookType.LaunchTarget));
    }

    private void HandleCharge(int effIndex)
    {
        var caster = Caster;
        var target = HitUnit;
        caster.SpellFactory.CastSpell(caster, WarriorSpells.CHARGE_PAUSE_RAGE_DECAY, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, 0));
        caster.SpellFactory.CastSpell(target, WarriorSpells.CHARGE_ROOT_EFFECT, true);
        caster.SpellFactory.CastSpell(target, WarriorSpells.CHARGE_SLOW_EFFECT, true);
    }
}