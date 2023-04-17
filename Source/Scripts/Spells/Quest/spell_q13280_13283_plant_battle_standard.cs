// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 4338 Plant Alliance Battle Standard
internal class SpellQ1328013283PlantBattleStandard : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;
        var target = HitUnit;
        var triggeredSpellID = QuestSpellIds.ALLIANCE_BATTLE_STANDARD_STATE;

        caster.HandleEmoteCommand(Emote.OneshotRoar);

        if (caster.IsVehicle)
        {
            var player = caster.VehicleKit1.GetPassenger(0);

            if (player)
                player.AsPlayer.KilledMonsterCredit(CreatureIds.KING_OF_THE_MOUNTAINT_KC);
        }

        if (SpellInfo.Id == QuestSpellIds.PLANT_HORDE_BATTLE_STANDARD)
            triggeredSpellID = QuestSpellIds.HORDE_BATTLE_STANDARD_STATE;

        target.RemoveAllAuras();
        target.SpellFactory.CastSpell(target, triggeredSpellID, true);
    }
}