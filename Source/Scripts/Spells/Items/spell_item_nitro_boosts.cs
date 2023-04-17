// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script]
internal class SpellItemNitroBoosts : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        if (!CastItem)
            return false;

        return true;
    }


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;
        var areaEntry = CliDB.AreaTableStorage.LookupByKey(caster.Area);
        var success = true;

        if (areaEntry != null &&
            areaEntry.IsFlyable() &&
            !caster.Map.IsDungeon)
            success = RandomHelper.randChance(95);

        caster.SpellFactory.CastSpell(caster, success ? ItemSpellIds.NITRO_BOOSTS_SUCCESS : ItemSpellIds.NITRO_BOOSTS_BACKFIRE, new CastSpellExtraArgs(CastItem));
    }
}