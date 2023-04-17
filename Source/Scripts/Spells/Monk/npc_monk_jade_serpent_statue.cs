// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[CreatureScript(60849)]
public class NPCMonkJadeSerpentStatue : ScriptedAI
{
    public NPCMonkJadeSerpentStatue(Creature c) : base(c) { }

    public override void UpdateAI(uint diff)
    {
        var owner = Me.OwnerUnit;

        if (owner != null)
        {
            var player = owner.AsPlayer;

            if (player != null)
            {
                if (player.Class != PlayerClass.Monk)
                    return;
                else
                {
                    if (player.GetPrimarySpecialization() != TalentSpecialization.MonkMistweaver && Me.IsInWorld)
                        Me.DespawnOrUnsummon();
                }
            }
        }
    }
}