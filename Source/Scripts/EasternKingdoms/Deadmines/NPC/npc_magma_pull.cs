// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Framework.Constants;
using Scripts.EasternKingdoms.Deadmines.Bosses;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(49454)]
public class NPCMagmaPull : ScriptedAI
{
    public static readonly Position VanessaNightmare1 = new(-230.717f, -563.0139f, 51.31293f, 1.047198f);
    public static readonly Position GlubtokNightmare1 = new(-229.3403f, -560.3629f, 51.31293f, 5.742133f);

    public InstanceScript Instance;
    public bool Pullplayers;
    public bool Csummon;
    public Player PlayerGUID;
    public uint PongTimer;

    public NPCMagmaPull(Creature creature) : base(creature)
    {
        Instance = creature.InstanceScript;
    }

    public override void Reset()
    {
        Pullplayers = true;
        Csummon = true;
        PongTimer = 2000;
    }

    public void AfterTeleportPlayer(Player player)
    {
        PlayerGUID = player;
    }

    public override void UpdateAI(uint diff)
    {
        if (PongTimer <= diff)
        {
            if (Pullplayers)
            {
                var players = new List<Unit>();
                var checker = new AnyPlayerInObjectRangeCheck(Me, 150.0f);
                var searcher = new PlayerListSearcher(Me, players, checker);
                Cell.VisitGrid(Me, searcher, 150f);

                foreach (var item in players)
                {
                    item.AddAura(BossVanessaVancleef.Spells.EFFECT_1, item);
                    item.NearTeleportTo(-205.7569f, -579.0972f, 42.98623f, 2.3f);
                }

                Me.Whisper(BossVanessaVancleef.VANESSA_NIGHTMARE_6, PlayerGUID, true);
                Me.DespawnOrUnsummon(TimeSpan.FromMilliseconds(3000));

                if (!Me.FindNearestPlayer(50))
                    Pullplayers = false;
            }

            if (Csummon)
            {
                Me.SummonCreature(DmCreatures.NPC_VANESSA_NIGHTMARE, VanessaNightmare1, TempSummonType.ManualDespawn);
                Me.SummonCreature(DmCreatures.NPC_GLUBTOK_NIGHTMARE, GlubtokNightmare1, TempSummonType.ManualDespawn);
                Csummon = false;
            }
        }
        else
            PongTimer -= diff;
    }
}