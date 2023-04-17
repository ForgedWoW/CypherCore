// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

//262231
[SpellScript(262231)]
public class WarMachine : ScriptObjectAutoAdd, IPlayerOnPVPKill, IPlayerOnCreatureKill
{
    public WarMachine() : base("war_machine") { }

    public void OnCreatureKill(Player killer, Creature killed)
    {
        if (killer.Class != PlayerClass.Warrior)
            return;

        if (!killer.HasAura(WarriorSpells.WARRRIOR_WAR_MACHINE_BUFF) && killer.HasAura(WarriorSpells.WAR_MACHINE))
            killer.SpellFactory.CastSpell(null, WarriorSpells.WARRRIOR_WAR_MACHINE_BUFF, true);
    }

    public void OnPVPKill(Player killer, Player killed)
    {
        if (killer.Class != PlayerClass.Warrior)
            return;

        if (!killer.HasAura(WarriorSpells.WARRRIOR_WAR_MACHINE_BUFF) && killer.HasAura(WarriorSpells.WAR_MACHINE))
            killer.SpellFactory.CastSpell(null, WarriorSpells.WARRRIOR_WAR_MACHINE_BUFF, true);
    }
}