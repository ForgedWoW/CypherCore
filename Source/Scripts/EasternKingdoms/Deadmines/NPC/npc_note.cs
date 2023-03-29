// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

public class npc_note : NullCreatureAI
{
    public static readonly Position VanessaSpawn = new(-75.58507f, -819.9636f, 47.06727f, 6.178465f);

    public npc_note(Creature creature) : base(creature)
    {
        creature.SetUnitFlag(UnitFlags.Pacified);
    }

    public override bool OnGossipHello(Player player)
    {
        if (player != null)
            player.AddGossipItem(GossipOptionNpc.None, boss_vanessa_vancleef.INTRUDER_SAY, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 1);

        player.SendGossipMenu(player.GetGossipTextId(Me), Me.GUID);

        return true;
    }

    public override bool OnGossipSelect(Player player, uint UnnamedParameter, uint action)
    {
        player.PlayerTalkClass.ClearMenus();

        if (action == GossipAction.GOSSIP_ACTION_INFO_DEF + 1)
        {
            Me.SummonCreature(DMCreatures.NPC_VANESSA_VANCLEEF, VanessaSpawn, TempSummonType.ManualDespawn);
            Me.TextEmote(boss_vanessa_vancleef.TEXT_INFO, null, true);
            Me.DespawnOrUnsummon();
            player.CloseGossipMenu();
        }

        return true;
    }

    public override void UpdateAI(uint diff) { }
}