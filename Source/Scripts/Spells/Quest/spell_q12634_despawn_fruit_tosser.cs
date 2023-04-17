// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 51840 Despawn Fruit Tosser
internal class SpellQ12634DespawnFruitTosser : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var spellId = QuestSpellIds.BANANAS_FALL_TO_GROUND;

        switch (RandomHelper.URand(0, 3))
        {
            case 1:
                spellId = QuestSpellIds.ORANGE_FALLS_TO_GROUND;

                break;
            case 2:
                spellId = QuestSpellIds.PAPAYA_FALLS_TO_GROUND;

                break;
        }

        // sometimes, if you're lucky, you get a dwarf
        if (RandomHelper.randChance(5))
            spellId = QuestSpellIds.SUMMON_ADVENTUROUS_DWARF;

        Caster.SpellFactory.CastSpell(Caster, spellId, true);
    }
}