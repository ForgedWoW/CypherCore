// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Phasing;
using Forged.MapServer.Quest;
using Forged.MapServer.Scripting;

namespace Scripts.Draenor;

// 79243 - Baros Alexston
internal struct MiscConst
{
    // Quest
    public const uint QUEST_ESTABLISH_YOUR_GARRISON = 34586;

    // Gossip
    public const uint GOSSIP_OPTION_ESTABLISH_GARRISON = 0;

    // Text
    public const uint SAY_START_CONSTRUCTION = 0;

    // Spells
    public const uint SPELL_QUEST34586_KILLCREDIT = 161033;
    public const uint SPELL_CREATE_GARRISON_SHADOWMOON_VALLEY_ALLIANCE = 156020;
    public const uint SPELL_DESPAWN_ALL_SUMMONS_GARRISON_INTRO_ONLY = 160938;

    public static Position GarrisonLevelOneCreationPlayerPosition = new(1904.58f, 312.906f, 88.9542f, 4.303615f);
}

[Script]
internal class NPCBarosAlexston : ScriptedAI
{
    public NPCBarosAlexston(Creature creature) : base(creature) { }

    public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
    {
        if (gossipListId == MiscConst.GOSSIP_OPTION_ESTABLISH_GARRISON)
        {
            player.CloseGossipMenu();
            player.SpellFactory.CastSpell(player, MiscConst.SPELL_QUEST34586_KILLCREDIT, true);
            player.SpellFactory.CastSpell(player, MiscConst.SPELL_CREATE_GARRISON_SHADOWMOON_VALLEY_ALLIANCE, true);
            player.SpellFactory.CastSpell(player, MiscConst.SPELL_DESPAWN_ALL_SUMMONS_GARRISON_INTRO_ONLY, true);
            player.NearTeleportTo(MiscConst.GarrisonLevelOneCreationPlayerPosition);

            PhasingHandler.OnConditionChange(player);
        }

        return true;
    }

    public override void OnQuestAccept(Player player, Quest quest)
    {
        if (quest.Id == MiscConst.QUEST_ESTABLISH_YOUR_GARRISON)
            Talk(MiscConst.SAY_START_CONSTRUCTION, player);
    }
}