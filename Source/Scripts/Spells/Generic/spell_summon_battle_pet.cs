// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script] // 118301 - Summon Battle Pet
internal class SpellSummonBattlePet : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleSummon, 0, SpellEffectName.Summon, SpellScriptHookType.EffectHit));
    }

    private void HandleSummon(int effIndex)
    {
        var creatureId = (uint)SpellValue.EffectBasePoints[effIndex];

        if (Global.ObjectMgr.GetCreatureTemplate(creatureId) != null)
        {
            PreventHitDefaultEffect(effIndex);

            var caster = Caster;
            var properties = CliDB.SummonPropertiesStorage.LookupByKey((uint)EffectInfo.MiscValueB);
            var duration = (uint)SpellInfo.CalcDuration(caster);
            var pos = HitDest;

            Creature summon = caster.Map.SummonCreature(creatureId, pos, properties, duration, caster, SpellInfo.Id);

            summon?.SetImmuneToAll(true);
        }
    }
}