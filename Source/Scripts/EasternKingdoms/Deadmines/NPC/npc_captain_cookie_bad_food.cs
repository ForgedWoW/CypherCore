// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Framework.Constants;
using static Scripts.EasternKingdoms.Deadmines.Bosses.BossCaptainCookie;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(new uint[]
{
    48276, 48293, 48295, 48298, 48299, 48302
})]
public class NPCCaptainCookieBadFood : ScriptedAI
{
    private readonly InstanceScript _pInstance;

    public NPCCaptainCookieBadFood(Creature pCreature) : base(pCreature)
    {
        _pInstance = pCreature.InstanceScript;
    }

    public override void JustDied(Unit killer)
    {
        Me.DespawnOrUnsummon();
    }

    public override void UpdateAI(uint unnamedParameter)
    {
        if (_pInstance == null)
            return;

        if (_pInstance.GetBossState(DmData.DATA_COOKIE) != EncounterState.InProgress)
            Me.DespawnOrUnsummon();
    }

    public override bool OnGossipHello(Player pPlayer)
    {
        var pInstance = Me.InstanceScript;

        if (pInstance == null)
            return true;

        if (pInstance.GetBossState(DmData.DATA_COOKIE) != EncounterState.InProgress)
            return true;

        pPlayer.SpellFactory.CastSpell(pPlayer, (pPlayer.Map.IsHeroic ? ESpell.NAUSEATED_H : ESpell.NAUSEATED), true);

        Me.DespawnOrUnsummon();

        return true;
    }
}