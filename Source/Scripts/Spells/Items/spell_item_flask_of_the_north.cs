// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;
using Serilog;

namespace Scripts.Spells.Items;

[Script] // 67019 Flask of the North
internal class SpellItemFlaskOfTheNorth : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;
        List<uint> possibleSpells = new();

        switch (caster.Class)
        {
            case PlayerClass.Warlock:
            case PlayerClass.Mage:
            case PlayerClass.Priest:
                possibleSpells.Add(ItemSpellIds.FLASK_OF_THE_NORTH_SP);

                break;
            case PlayerClass.Deathknight:
            case PlayerClass.Warrior:
                possibleSpells.Add(ItemSpellIds.FLASK_OF_THE_NORTH_STR);

                break;
            case PlayerClass.Rogue:
            case PlayerClass.Hunter:
                possibleSpells.Add(ItemSpellIds.FLASK_OF_THE_NORTH_AP);

                break;
            case PlayerClass.Druid:
            case PlayerClass.Paladin:
                possibleSpells.Add(ItemSpellIds.FLASK_OF_THE_NORTH_SP);
                possibleSpells.Add(ItemSpellIds.FLASK_OF_THE_NORTH_STR);

                break;
            case PlayerClass.Shaman:
                possibleSpells.Add(ItemSpellIds.FLASK_OF_THE_NORTH_SP);
                possibleSpells.Add(ItemSpellIds.FLASK_OF_THE_NORTH_AP);

                break;
        }

        if (possibleSpells.Empty())
        {
            Log.Logger.Warning("Missing spells for class {0} in script spell_item_flask_of_the_north", caster.Class);

            return;
        }

        caster.SpellFactory.CastSpell(caster, possibleSpells.SelectRandom(), true);
    }
}