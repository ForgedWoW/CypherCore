// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[Script]
public class FeralSpells : ScriptObjectAutoAdd, IPlayerOnLogin
{
    public FeralSpells() : base("feral_spells") { }

    public PlayerClass PlayerClass
    {
        get { return PlayerClass.Druid; }
    }

    public void OnLogin(Player player)
    {
        if (player.GetPrimarySpecialization() != TalentSpecialization.DruidCat)
            return;

        if (player.Level >= 5 && !player.HasSpell(DruidSpells.Shred))
            player.LearnSpell(DruidSpells.Shred, false);

        if (player.Level >= 20 && !player.HasSpell(DruidSpells.Rip))
            player.LearnSpell(DruidSpells.Rip, false);

        if (player.Level >= 24 && !player.HasSpell(DruidSpells.Rake))
            player.LearnSpell(DruidSpells.Rake, false);

        if (player.Level >= 32 && !player.HasSpell(DruidSpells.FerociousBite))
            player.LearnSpell(DruidSpells.FerociousBite, false);
    }
}