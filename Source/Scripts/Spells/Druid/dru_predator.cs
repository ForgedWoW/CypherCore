// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[Script]
public class DruPredator : ScriptObjectAutoAdd, IPlayerOnPVPKill, IPlayerOnCreatureKill
{
    public DruPredator() : base("dru_predator") { }

    public void OnCreatureKill(Player killer, Creature killed)
    {
        if (killer.Class == PlayerClass.Druid)
            return;

        if (!killer.HasAura(DruidSpells.Predator))
            return;

        if (killer.SpellHistory.HasCooldown(DruidSpells.TigerFury))
            killer.SpellHistory.ResetCooldown(DruidSpells.TigerFury);
    }

    public void OnPVPKill(Player killer, Player killed)
    {
        if (killer.Class == PlayerClass.Druid)
            return;

        if (!killer.HasAura(DruidSpells.Predator))
            return;

        if (killer.SpellHistory.HasCooldown(DruidSpells.TigerFury))
            killer.SpellHistory.ResetCooldown(DruidSpells.TigerFury);
    }
}