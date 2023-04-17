// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 8063 Deviate Fish
internal class SpellItemDeviateFish : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        return Caster.TypeId == TypeId.Player;
    }


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;
        var spellId = RandomHelper.RAND(ItemSpellIds.SLEEPY, ItemSpellIds.INVIGORATE, ItemSpellIds.SHRINK, ItemSpellIds.PARTY_TIME, ItemSpellIds.HEALTHY_SPIRIT, ItemSpellIds.REJUVENATION);
        caster.SpellFactory.CastSpell(caster, spellId, true);
    }
}