// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.BattleFields;
using Forged.MapServer.Entities.Players;
using Framework.Constants;

namespace Forged.MapServer.Spells;

public class SpellArea
{
    public uint AreaId { get; set; }
    public int AuraSpell { get; set; }
    public SpellAreaFlag Flags { get; set; }
    public Gender Gender { get; set; }
    public uint QuestEnd { get; set; }

    public uint QuestEndStatus { get; set; }

    // zone/subzone/or 0 is not limited to zone
    public uint QuestStart { get; set; }

    // can be applied only to gender
    public uint QuestStartStatus { get; set; }

    // quest start (quest must be active or rewarded for spell apply)
    // quest end (quest must not be rewarded for spell apply)
    // spell aura must be applied for spell apply)if possitive) and it must not be applied in other case
    public ulong RaceMask { get; set; }

    public uint SpellId { get; set; }
    // can be applied only to races
    // QuestStatus that quest_start must have in order to keep the spell
    // QuestStatus that the quest_end must have in order to keep the spell (if the quest_end's status is different than this, the spell will be dropped)
    // if SPELL_AREA_FLAG_AUTOCAST then auto applied at area enter, in other case just allowed to cast || if SPELL_AREA_FLAG_AUTOREMOVE then auto removed inside area (will allways be removed on leaved even without Id)

    // helpers
    public bool IsFitToRequirements(Player player, uint newZone, uint newArea)
    {
        if (Gender != Gender.None) // not in expected gender
            if (player == null || Gender != player.NativeGender)
                return false;

        if (RaceMask != 0) // not in expected race
            if (player == null || !Convert.ToBoolean(RaceMask & (ulong)SharedConst.GetMaskForRace(player.Race)))
                return false;

        if (AreaId != 0) // not in expected zone
            if (newZone != AreaId && newArea != AreaId)
                return false;

        if (QuestStart != 0) // not in expected required quest state
            if (player == null || ((1 << (int)player.GetQuestStatus(QuestStart)) & QuestStartStatus) == 0)
                return false;

        if (QuestEnd != 0) // not in expected forbidden quest state
            if (player == null || ((1 << (int)player.GetQuestStatus(QuestEnd)) & QuestEndStatus) == 0)
                return false;

        if (AuraSpell != 0) // not have expected aura
            if (player == null || (AuraSpell > 0 && !player.HasAura((uint)AuraSpell)) || (AuraSpell < 0 && player.HasAura((uint)-AuraSpell)))
                return false;

        var bg = player?.Battleground;

        if (bg != null)
            return bg.IsSpellAllowed(SpellId, player);

        // Extra conditions -- leaving the possibility add extra conditions...
        switch (SpellId)
        {
            case 91604: // No fly Zone - Wintergrasp
            {
                var bf = player?.BattleFieldManager.GetBattlefieldToZoneId(player.Location.Map, player.Location.Zone);

                if (bf == null || bf.CanFlyIn || (!player.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed) && !player.HasAuraType(AuraType.Fly)))
                    return false;

                break;
            }
            case 56618: // Horde Controls Factory Phase Shift
            case 56617: // Alliance Controls Factory Phase Shift
            {
                var bf = player?.BattleFieldManager.GetBattlefieldToZoneId(player.Location.Map, player.Location.Zone);

                if (bf is not { TypeId: (int)BattleFieldTypes.WinterGrasp })
                    return false;

                // team that controls the workshop in the specified area
                var team = bf.GetData(newArea);

                switch (team)
                {
                    case TeamIds.Horde:
                        return SpellId == 56618;

                    case TeamIds.Alliance:
                        return SpellId == 56617;
                }

                break;
            }
            case 57940: // Essence of Wintergrasp - Northrend
            case 58045: // Essence of Wintergrasp - Wintergrasp
            {
                var battlefieldWg = player?.BattleFieldManager.GetBattlefieldByBattleId(player.Location.Map, 1);

                if (battlefieldWg != null)
                    return battlefieldWg.IsEnabled && player.TeamId == battlefieldWg.DefenderTeam && battlefieldWg.IsActive;

                break;
            }
            case 74411: // Battleground- Dampening
            {
                var bf = player?.BattleFieldManager.GetBattlefieldToZoneId(player.Location.Map, player.Location.Zone);

                if (bf != null)
                    return bf.IsActive;

                break;
            }
        }

        return true;
    }
}