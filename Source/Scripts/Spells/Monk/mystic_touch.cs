// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[Script]
public class MysticTouch : ScriptObjectAutoAdd, IPlayerOnDealDamage
{
    public MysticTouch() : base("mystic_touch") { }
    public PlayerClass PlayerClass => PlayerClass.Monk;

    public void OnDamage(Player caster, Unit target, ref double damage, SpellInfo spellProto)
    {
        var player = caster.AsPlayer;

        if (player != null)
            if (player.Class != PlayerClass.Monk)
                return;

        if (caster == null || target == null)
            return;

        if (target.HasAura(MonkSpells.MYSTIC_TOUCH_TARGET_DEBUFF))
            return;

        if (caster.HasAura(MonkSpells.MYSTIC_TOUCH) && !target.HasAura(MonkSpells.MYSTIC_TOUCH_TARGET_DEBUFF))
            if (caster.IsWithinMeleeRange(target))
                caster.SpellFactory.CastSpell(MonkSpells.MYSTIC_TOUCH_TARGET_DEBUFF, true);
    }
}