// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 40056 Knockdown Fel Cannon: Choose Loc
internal class SpellQ11010Q11102Q11023ChooseLoc : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;
        // Check for player that is in 65 y range
        List<Unit> playerList = new();
        AnyPlayerInObjectRangeCheck checker = new(caster, 65.0f);
        PlayerListSearcher searcher = new(caster, playerList, checker);
        Cell.VisitGrid(caster, searcher, 65.0f);

        foreach (Player player in playerList)
            // Check if found player Target is on fly Mount or using flying form
            if (player.HasAuraType(AuraType.Fly) ||
                player.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed))
                // Summom Fel Cannon (bunny version) at found player
                caster.SummonCreature(CreatureIds.FEL_CANNON2, player.Location.X, player.Location.Y, player.Location.Z);
    }
}