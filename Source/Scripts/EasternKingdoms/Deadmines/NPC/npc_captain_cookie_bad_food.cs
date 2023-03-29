// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using static Scripts.EasternKingdoms.Deadmines.Bosses.boss_captain_cookie;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(new uint[]
{
    48276, 48293, 48295, 48298, 48299, 48302
})]
public class npc_captain_cookie_bad_food : ScriptedAI
{
    private readonly InstanceScript _pInstance;

    public npc_captain_cookie_bad_food(Creature pCreature) : base(pCreature)
    {
        _pInstance = pCreature.InstanceScript;
    }

    public override void JustDied(Unit killer)
    {
        Me.DespawnOrUnsummon();
    }

    public override void UpdateAI(uint UnnamedParameter)
    {
        if (_pInstance == null)
            return;

        if (_pInstance.GetBossState(DMData.DATA_COOKIE) != EncounterState.InProgress)
            Me.DespawnOrUnsummon();
    }

    public override bool OnGossipHello(Player pPlayer)
    {
        var pInstance = Me.InstanceScript;

        if (pInstance == null)
            return true;

        if (pInstance.GetBossState(DMData.DATA_COOKIE) != EncounterState.InProgress)
            return true;

        pPlayer.CastSpell(pPlayer, (pPlayer.Map.IsHeroic ? eSpell.NAUSEATED_H : eSpell.NAUSEATED), true);

        Me.DespawnOrUnsummon();

        return true;
    }
}