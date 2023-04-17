// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockDepths.TombOfSeven;

internal struct SpellIds
{
    //Gloomrel
    public const uint SMELT_DARK_IRON = 14891;
    public const uint LEARN_SMELT = 14894;

    //Doomrel
    public const uint SHADOWBOLTVOLLEY = 15245;
    public const uint IMMOLATE = 12742;
    public const uint CURSEOFWEAKNESS = 12493;
    public const uint DEMONARMOR = 13787;
    public const uint SUMMON_VOIDWALKERS = 15092;
}

internal struct QuestIds
{
    public const uint SPECTRAL_CHALICE = 4083;
}

internal struct TextIds
{
    public const uint GOSSIP_SELECT_DOOMREL = 1828;
    public const uint GOSSIP_MENU_ID_CONTINUE = 1;

    public const uint GOSSIP_MENU_CHALLENGE = 1947;
    public const uint GOSSIP_MENU_ID_CHALLENGE = 0;
}

internal struct MiscConst
{
    public const uint DATA_SKILLPOINT_MIN = 230;

    public const string GOSSIP_ITEM_TEACH1 = "Teach me the art of smelting dark iron";
    public const string GOSSIP_ITEM_TEACH2 = "Continue...";
    public const string GOSSIP_ITEM_TEACH3 = "[PH] Continue...";
    public const string GOSSIP_ITEM_TRIBUTE = "I want to pay tribute";
}

internal enum Phases
{
    PhaseOne = 1,
    PhaseTwo = 2
}

[Script]
internal class BossGloomrel : ScriptedAI
{
    private readonly InstanceScript _instance;

    public BossGloomrel(Creature creature) : base(creature)
    {
        _instance = creature.InstanceScript;
    }

    public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
    {
        var action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
        player.ClearGossipMenu();

        switch (action)
        {
            case GossipAction.GOSSIP_ACTION_INFO_DEF + 1:
                player.AddGossipItem(GossipOptionNpc.None, MiscConst.GOSSIP_ITEM_TEACH2, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 11);
                player.SendGossipMenu(2606, Me.GUID);

                break;
            case GossipAction.GOSSIP_ACTION_INFO_DEF + 11:
                player.CloseGossipMenu();
                player.SpellFactory.CastSpell(player, SpellIds.LEARN_SMELT, false);

                break;
            case GossipAction.GOSSIP_ACTION_INFO_DEF + 2:
                player.AddGossipItem(GossipOptionNpc.None, MiscConst.GOSSIP_ITEM_TEACH3, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 22);
                player.SendGossipMenu(2604, Me.GUID);

                break;
            case GossipAction.GOSSIP_ACTION_INFO_DEF + 22:
                player.CloseGossipMenu();
                //are 5 minutes expected? go template may have data to despawn when used at quest
                _instance.DoRespawnGameObject(_instance.GetGuidData(DataTypes.DATA_GO_CHALICE), TimeSpan.FromMinutes(5));

                break;
        }

        return true;
    }

    public override bool OnGossipHello(Player player)
    {
        if (player.GetQuestRewardStatus(QuestIds.SPECTRAL_CHALICE) &&
            player.GetSkillValue(SkillType.Mining) >= MiscConst.DATA_SKILLPOINT_MIN &&
            !player.HasSpell(SpellIds.SMELT_DARK_IRON))
            player.AddGossipItem(GossipOptionNpc.None, MiscConst.GOSSIP_ITEM_TEACH1, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 1);

        if (!player.GetQuestRewardStatus(QuestIds.SPECTRAL_CHALICE) &&
            player.GetSkillValue(SkillType.Mining) >= MiscConst.DATA_SKILLPOINT_MIN)
            player.AddGossipItem(GossipOptionNpc.None, MiscConst.GOSSIP_ITEM_TRIBUTE, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 2);

        player.SendGossipMenu(player.GetGossipTextId(Me), Me.GUID);

        return true;
    }
}

[Script]
internal class BossDoomrel : ScriptedAI
{
    private readonly InstanceScript _instance;
    private bool _voidwalkers;

    public BossDoomrel(Creature creature) : base(creature)
    {
        Initialize();
        _instance = creature.InstanceScript;
    }

    public override void Reset()
    {
        Initialize();

        Me.Faction = (uint)FactionTemplates.Friendly;

        // was set before event start, so set again
        Me.SetImmuneToPC(true);

        if (_instance.GetData(DataTypes.DATA_GHOSTKILL) >= 7)
            Me.ReplaceAllNpcFlags(NPCFlags.None);
        else
            Me.ReplaceAllNpcFlags(NPCFlags.Gossip);
    }

    public override void JustEngagedWith(Unit who)
    {
        Scheduler.Schedule(TimeSpan.FromSeconds(10),
                           task =>
                           {
                               DoCastVictim(SpellIds.SHADOWBOLTVOLLEY);
                               task.Repeat(TimeSpan.FromSeconds(12));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(18),
                           task =>
                           {
                               var target = SelectTarget(SelectTargetMethod.Random, 0, 100.0f, true);

                               if (target)
                                   DoCast(target, SpellIds.IMMOLATE);

                               task.Repeat(TimeSpan.FromSeconds(25));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(5),
                           task =>
                           {
                               DoCastVictim(SpellIds.CURSEOFWEAKNESS);
                               task.Repeat(TimeSpan.FromSeconds(45));
                           });

        Scheduler.Schedule(TimeSpan.FromSeconds(16),
                           task =>
                           {
                               DoCast(Me, SpellIds.DEMONARMOR);
                               task.Repeat(TimeSpan.FromMinutes(5));
                           });
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (!_voidwalkers &&
            !HealthAbovePct(50))
        {
            DoCastVictim(SpellIds.SUMMON_VOIDWALKERS, new CastSpellExtraArgs(true));
            _voidwalkers = true;
        }
    }

    public override void EnterEvadeMode(EvadeReason why)
    {
        base.EnterEvadeMode(why);

        _instance.SetGuidData(DataTypes.DATA_EVENSTARTER, ObjectGuid.Empty);
    }

    public override void JustDied(Unit killer)
    {
        _instance.SetData(DataTypes.DATA_GHOSTKILL, 1);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Scheduler.Update(diff, () => DoMeleeAttackIfReady());
    }

    public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
    {
        var action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
        player.ClearGossipMenu();

        switch (action)
        {
            case GossipAction.GOSSIP_ACTION_INFO_DEF + 1:
                player.InitGossipMenu(TextIds.GOSSIP_SELECT_DOOMREL);
                player.AddGossipItem(TextIds.GOSSIP_SELECT_DOOMREL, TextIds.GOSSIP_MENU_ID_CONTINUE, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 2);
                player.SendGossipMenu(2605, Me.GUID);

                break;
            case GossipAction.GOSSIP_ACTION_INFO_DEF + 2:
                player.CloseGossipMenu();

                //start event here
                Me. //start event here
                    Faction = (int)FactionTemplates.DarkIronDwarves;

                Me.SetImmuneToPC(false);
                Me.AI.AttackStart(player);

                _instance.SetGuidData(DataTypes.DATA_EVENSTARTER, player.GUID);

                break;
        }

        return true;
    }

    public override bool OnGossipHello(Player player)
    {
        player.InitGossipMenu(TextIds.GOSSIP_MENU_CHALLENGE);
        player.AddGossipItem(TextIds.GOSSIP_MENU_CHALLENGE, TextIds.GOSSIP_MENU_ID_CHALLENGE, GossipSender.GOSSIP_SENDER_MAIN, GossipAction.GOSSIP_ACTION_INFO_DEF + 1);
        player.SendGossipMenu(2601, Me.GUID);

        return true;
    }

    private void Initialize()
    {
        _voidwalkers = false;
    }
}