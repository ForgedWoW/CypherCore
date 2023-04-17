// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 51858 - Siphon of Acherus
internal class SpellQ12641DeathComesFromOnHigh : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        uint spellId;

        switch (HitCreature.Entry)
        {
            case CreatureIds.NEW_AVALON_FORGE:
                spellId = QuestSpellIds.FORGE_CREDIT;

                break;
            case CreatureIds.NEW_AVALON_TOWN_HALL:
                spellId = QuestSpellIds.TOWN_HALL_CREDIT;

                break;
            case CreatureIds.SCARLET_HOLD:
                spellId = QuestSpellIds.SCARLET_HOLD_CREDIT;

                break;
            case CreatureIds.CHAPEL_OF_THE_CRIMSON_FLAME:
                spellId = QuestSpellIds.CHAPEL_CREDIT;

                break;
            default:
                return;
        }

        Caster.SpellFactory.CastSpell((Unit)null, spellId, true);
    }
}