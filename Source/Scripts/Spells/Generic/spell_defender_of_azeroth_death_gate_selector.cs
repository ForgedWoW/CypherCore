// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellDefenderOfAzerothDeathGateSelector : SpellScript, IHasSpellEffects
{
    private readonly (WorldLocation, uint) _orgrimmarInnLoc = (new WorldLocation(1, 1573.18f, -4441.62f, 16.06f, 1.818284034729003906f), 8618);
    private readonly (WorldLocation, uint) _stormwindInnLoc = (new WorldLocation(0, -8868.1f, 675.82f, 97.9f, 5.164778709411621093f), 5148);
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var player = HitUnit.AsPlayer;

        if (player == null)
            return;

        if (player.GetQuestStatus(QuestIds.DEFENDER_OF_AZEROTH_ALLIANCE) == QuestStatus.None &&
            player.GetQuestStatus(QuestIds.DEFENDER_OF_AZEROTH_HORDE) == QuestStatus.None)
            return;

        (WorldLocation Loc, uint AreaId) bindLoc = player.Team == TeamFaction.Alliance ? _stormwindInnLoc : _orgrimmarInnLoc;
        player.SetHomebind(bindLoc.Loc, bindLoc.AreaId);
        player.SendBindPointUpdate();
        player.SendPlayerBound(player.GUID, bindLoc.AreaId);

        player.SpellFactory.CastSpell(player, player.Team == TeamFaction.Alliance ? GenericSpellIds.DEATH_GATE_TELEPORT_STORMWIND : GenericSpellIds.DEATH_GATE_TELEPORT_ORGRIMMAR);
    }
}