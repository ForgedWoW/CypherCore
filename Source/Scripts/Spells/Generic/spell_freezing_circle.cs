// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script] // 34779 - Freezing Circle
internal class SpellFreezingCircle : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDamage, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDamage(int effIndex)
    {
        var caster = Caster;
        uint spellId = 0;
        var map = caster.Map;

        if (map.IsDungeon)
            spellId = map.IsHeroic ? GenericSpellIds.FREEZING_CIRCLE_PIT_OF_SARON_HEROIC : GenericSpellIds.FREEZING_CIRCLE_PIT_OF_SARON_NORMAL;
        else
            spellId = map.Id == Misc.MAP_ID_BLOOD_IN_THE_SNOW_SCENARIO ? GenericSpellIds.FREEZING_CIRCLE_SCENARIO : GenericSpellIds.FREEZING_CIRCLE;

        var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, CastDifficulty);

        if (spellInfo != null)
            if (!spellInfo.Effects.Empty())
                HitDamage = spellInfo.GetEffect(0).CalcValue();
    }
}