// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 19512 Apply Salve
internal class SpellQ61246129ApplySalve : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        return Caster.IsTypeId(TypeId.Player);
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster.AsPlayer;

        if (CastItem)
        {
            var creatureTarget = HitCreature;

            if (creatureTarget)
            {
                uint newEntry = 0;

                switch (caster.Team)
                {
                    case TeamFaction.Horde:
                        if (creatureTarget.Entry == CreatureIds.SICKLY_GAZELLE)
                            newEntry = CreatureIds.CURED_GAZELLE;

                        break;
                    case TeamFaction.Alliance:
                        if (creatureTarget.Entry == CreatureIds.SICKLY_DEER)
                            newEntry = CreatureIds.CURED_DEER;

                        break;
                }

                if (newEntry != 0)
                {
                    creatureTarget.UpdateEntry(newEntry);
                    creatureTarget.DespawnOrUnsummon(Misc.DespawnTime);
                    caster.KilledMonsterCredit(newEntry);
                }
            }
        }
    }
}