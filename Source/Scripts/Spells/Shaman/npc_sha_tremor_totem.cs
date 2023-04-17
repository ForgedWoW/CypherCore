// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

//8143
[CreatureScript(8143)]
public class NPCShaTremorTotem : ScriptedAI
{
    public enum SpellRelated
    {
        TremorTotemDispell = 8146
    }

    public NPCShaTremorTotem(Creature c) : base(c) { }

    public void OnUpdate(uint diff)
    {
        if (diff <= 1000)
        {
            var playerList = Me.GetPlayerListInGrid(30.0f);

            if (playerList.Count != 0)
                foreach (Player target in playerList)
                    if (target.IsFriendlyTo(Me.OwnerUnit))
                        if (target.HasAuraType(AuraType.ModFear) || target.HasAuraType(AuraType.ModFear2) || target.HasAuraType(AuraType.ModCharm))
                            Me.SpellFactory.CastSpell(target, SpellRelated.TremorTotemDispell, true);
        }
    }
}