// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warrior;

[Script] // 97462 - Rallying Cry
internal class spell_warr_rallying_cry : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override bool Load()
    {
        return Caster.IsTypeId(TypeId.Player);
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
        args.AddSpellMod(SpellValueMod.BasePoint0, (int)HitUnit.CountPctFromMaxHealth(EffectValue));

        Caster.CastSpell(HitUnit, WarriorSpells.RALLYING_CRY, args);
    }
}