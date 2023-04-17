// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Framework.Constants;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

public class NPCNote : NullCreatureAI
{
    public static readonly Position VanessaSpawn = new(-75.58507f, -819.9636f, 47.06727f, 6.178465f);

    public NPCNote(Creature creature) : base(creature)
    {
        creature.SetUnitFlag(UnitFlags.Pacified);
    }

    public override bool OnGossipHello(Player player)
    {
        if (player != null)
            player.AddGossipItem(GossipOptionNpc.None, BossVanessaVancleef.INTRUDER_SAY, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 1);

        player.SendGossipMenu(player.GetGossipTextId(Me), Me.GUID);

        return true;
    }

    public override bool OnGossipSelect(Player player, uint unnamedParameter, uint action)
    {
        player.PlayerTalkClass.ClearMenus();

        if (action == GossipAction.GOSSIP_ACTION_INFO_DEF + 1)
        {
            Me.SummonCreature(DmCreatures.NPC_VANESSA_VANCLEEF, VanessaSpawn, TempSummonType.ManualDespawn);
            Me.TextEmote(BossVanessaVancleef.TEXT_INFO, null, true);
            Me.DespawnOrUnsummon();
            player.CloseGossipMenu();
        }

        return true;
    }

    public override void UpdateAI(uint diff) { }
}