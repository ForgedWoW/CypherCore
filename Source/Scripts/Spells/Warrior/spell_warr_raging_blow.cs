// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells.Auras;

namespace Scripts.Spells.Warrior;

// 85288
[SpellScript(85288)]
public class SpellWarrRagingBlow : SpellScript, ISpellOnHit
{
    private byte _targetHit;

    public void OnHit()
    {
        var player = Caster.AsPlayer;

        if (player != null)
            player.SpellFactory.CastSpell(player, WarriorSpells.ALLOW_RAGING_BLOW, true);

        if (Caster.HasAura(WarriorSpells.BATTLE_TRANCE))
        {
            var target = Caster.AsPlayer.SelectedUnit;
            var targetGUID = target.GUID;
            _targetHit++;

            if (_targetHit == 4)
            {
                //targetGUID.Clear();
                _targetHit = 0;
                Caster.SpellFactory.CastSpell(null, WarriorSpells.BATTLE_TRANCE_BUFF, true);
                var battleTrance = Caster.GetAura(WarriorSpells.BATTLE_TRANCE_BUFF).GetEffect(0);

                //if (battleTrance != null)
                //	battleTrance.Amount;
            }
        }

        if (RandomHelper.randChance(20))
            Caster.SpellHistory.ResetCooldown(85288, true);

        var whirlWind = Caster.GetAura(WarriorSpells.WHIRLWIND_PASSIVE);

        if (whirlWind != null)
            whirlWind.ModStackAmount(-1, AuraRemoveMode.Default, false);
    }
}