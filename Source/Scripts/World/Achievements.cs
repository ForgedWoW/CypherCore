// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAchievement;
using Framework.Constants;

namespace Scripts.World.Achievements;

internal struct AreaIds
{
    //Tilted
    public const uint AREA_ARGENT_TOURNAMENT_FIELDS = 4658;
    public const uint AREA_RING_OF_ASPIRANTS = 4670;
    public const uint AREA_RING_OF_ARGENT_VALIANTS = 4671;
    public const uint AREA_RING_OF_ALLIANCE_VALIANTS = 4672;
    public const uint AREA_RING_OF_HORDE_VALIANTS = 4673;
    public const uint AREA_RING_OF_CHAMPIONS = 4669;
}

internal struct AuraIds
{
    //Flirt With Disaster
    public const uint AURA_PERFUME_FOREVER = 70235;
    public const uint AURA_PERFUME_ENCHANTRESS = 70234;
    public const uint AURA_PERFUME_VICTORY = 70233;
}

internal struct VehicleIds
{
    //BgSA Artillery
    public const uint ANTI_PERSONNAL_CANNON = 27894;
}

[Script("achievement_arena_2v2_kills", ArenaTypes.Team2V2)]
[Script("achievement_arena_3v3_kills", ArenaTypes.Team3V3)]
[Script("achievement_arena_5v5_kills", ArenaTypes.Team5V5)]
internal class AchievementArenaKills : ScriptObjectAutoAddDBBound, IAchievementCriteriaOnCheck
{
    private readonly ArenaTypes _arenaType;

    public AchievementArenaKills(string name, ArenaTypes arenaType) : base(name)
    {
        _arenaType = arenaType;
    }

    public bool OnCheck(Player source, Unit target)
    {
        // this checks GetBattleground() for Null already
        if (!source.InArena)
            return false;

        return source.Battleground.GetArenaType() == _arenaType;
    }
}

[Script]
internal class AchievementTilted : ScriptObjectAutoAddDBBound, IAchievementCriteriaOnCheck
{
    public AchievementTilted() : base("achievement_tilted") { }

    public bool OnCheck(Player player, Unit target)
    {
        if (!player)
            return false;

        var checkArea = player.Area == AreaIds.AREA_ARGENT_TOURNAMENT_FIELDS ||
                        player.Area == AreaIds.AREA_RING_OF_ASPIRANTS ||
                        player.Area == AreaIds.AREA_RING_OF_ARGENT_VALIANTS ||
                        player.Area == AreaIds.AREA_RING_OF_ALLIANCE_VALIANTS ||
                        player.Area == AreaIds.AREA_RING_OF_HORDE_VALIANTS ||
                        player.Area == AreaIds.AREA_RING_OF_CHAMPIONS;

        return checkArea && player.Duel != null && player.Duel.IsMounted;
    }
}

[Script]
internal class AchievementFlirtWithDisasterPerfCheck : ScriptObjectAutoAddDBBound, IAchievementCriteriaOnCheck
{
    public AchievementFlirtWithDisasterPerfCheck() : base("achievement_flirt_with_disaster_perf_check") { }

    public bool OnCheck(Player player, Unit target)
    {
        if (!player)
            return false;

        if (player.HasAura(AuraIds.AURA_PERFUME_FOREVER) ||
            player.HasAura(AuraIds.AURA_PERFUME_ENCHANTRESS) ||
            player.HasAura(AuraIds.AURA_PERFUME_VICTORY))
            return true;

        return false;
    }
}

[Script]
internal class AchievementKilledExpOrHonorTarget : ScriptObjectAutoAddDBBound, IAchievementCriteriaOnCheck
{
    public AchievementKilledExpOrHonorTarget() : base("achievement_killed_exp_or_honor_target") { }

    public bool OnCheck(Player player, Unit target)
    {
        return target && player.IsHonorOrXPTarget(target);
    }
}

[Script] // 7433 - Newbie
internal class AchievementNewbie : ScriptObjectAutoAddDBBound, IAchievementOnCompleted
{
    public AchievementNewbie() : base("achievement_newbie") { }

    public void OnCompleted(Player player, AchievementRecord achievement)
    {
        player.Session.BattlePetMgr.UnlockSlot(BattlePetSlots.Slot1);
        // TODO: Unlock trap
    }
}

[Script] // 6566 - Just a Pup
internal class AchievementJustAPup : ScriptObjectAutoAddDBBound, IAchievementOnCompleted
{
    public AchievementJustAPup() : base("achievement_just_a_pup") { }

    public void OnCompleted(Player player, AchievementRecord achievement)
    {
        player.Session.BattlePetMgr.UnlockSlot(BattlePetSlots.Slot2);
    }
}