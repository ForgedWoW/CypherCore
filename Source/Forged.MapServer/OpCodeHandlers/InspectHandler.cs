// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Linq;
using Forged.MapServer.Globals;
using Forged.MapServer.Guilds;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Inspect;
using Forged.MapServer.Networking.Packets.Trait;
using Forged.MapServer.Server;
using Framework.Constants;
using Framework.Util;
using Game.Common.Handlers;
using Microsoft.Extensions.Configuration;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class InspectHandler : IWorldSessionHandler
{
    private readonly IConfiguration _configuration;
    private readonly GuildManager _guildManager;
    private readonly ObjectAccessor _objectAccessor;
    private readonly WorldSession _session;

    public InspectHandler(WorldSession session, IConfiguration configuration, ObjectAccessor objectAccessor, GuildManager guildManager)
    {
        _session = session;
        _configuration = configuration;
        _objectAccessor = objectAccessor;
        _guildManager = guildManager;
    }

    [WorldPacketHandler(ClientOpcodes.Inspect, Processing = PacketProcessing.Inplace)]
    private void HandleInspect(Inspect inspect)
    {
        var player = _objectAccessor.GetPlayer(_session.Player, inspect.Target);

        if (player == null)
        {
            Log.Logger.Debug("WorldSession.HandleInspectOpcode: Target {0} not found.", inspect.Target.ToString());

            return;
        }

        if (!_session.Player.Location.IsWithinDistInMap(player, SharedConst.InspectDistance, false))
            return;

        if (_session.Player.WorldObjectCombat.IsValidAttackTarget(player))
            return;

        InspectResult inspectResult = new();
        inspectResult.DisplayInfo.Initialize(player);

        if (_session.Player.CanBeGameMaster || _configuration.GetDefaultValue("TalentsInspecting", 1) + (_session.Player.EffectiveTeam == player.EffectiveTeam ? 1 : 0) > 1)
        {
            var talents = player.GetTalentMap(player.GetActiveTalentGroup());

            foreach (var v in talents.Where(v => v.Value != PlayerSpellState.Removed))
                inspectResult.Talents.Add((ushort)v.Key);
        }

        inspectResult.TalentTraits = new TraitInspectInfo();
        var traitConfig = player.GetTraitConfig((int)(uint)player.ActivePlayerData.ActiveCombatTraitConfigID);

        if (traitConfig != null)
        {
            inspectResult.TalentTraits.Config = new TraitConfigPacket(traitConfig);
            inspectResult.TalentTraits.ChrSpecializationID = (int)(uint)player.ActivePlayerData.ActiveCombatTraitConfigID;
        }

        inspectResult.TalentTraits.Level = (int)player.Level;

        var guild = _guildManager.GetGuildById(player.GuildId);

        if (guild != null)
        {
            InspectGuildData guildData;
            guildData.GuildGUID = guild.GetGUID();
            guildData.NumGuildMembers = guild.GetMembersCount();
            guildData.AchievementPoints = (int)guild.GetAchievementMgr().AchievementPoints;

            inspectResult.GuildData = guildData;
        }

        var heartOfAzeroth = player.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);

        var azeriteItem = heartOfAzeroth?.AsAzeriteItem;

        if (azeriteItem != null)
            inspectResult.AzeriteLevel = azeriteItem.GetEffectiveLevel();

        inspectResult.ItemLevel = (int)player.GetAverageItemLevel();
        inspectResult.LifetimeMaxRank = player.ActivePlayerData.LifetimeMaxRank;
        inspectResult.TodayHK = player.ActivePlayerData.TodayHonorableKills;
        inspectResult.YesterdayHK = player.ActivePlayerData.YesterdayHonorableKills;
        inspectResult.LifetimeHK = player.ActivePlayerData.LifetimeHonorableKills;
        inspectResult.HonorLevel = player.PlayerData.HonorLevel;

        _session.SendPacket(inspectResult);
    }

    [WorldPacketHandler(ClientOpcodes.QueryInspectAchievements, Processing = PacketProcessing.Inplace)]
    private void HandleQueryInspectAchievements(QueryInspectAchievements inspect)
    {
        var player = _objectAccessor.GetPlayer(_session.Player, inspect.Guid);

        if (player == null)
        {
            Log.Logger.Debug("WorldSession.HandleQueryInspectAchievements: [{0}] inspected unknown Player [{1}]", _session.Player.GUID.ToString(), inspect.Guid.ToString());

            return;
        }

        if (!_session.Player.Location.IsWithinDistInMap(player, SharedConst.InspectDistance, false))
            return;

        if (_session.Player.WorldObjectCombat.IsValidAttackTarget(player))
            return;

        player.SendRespondInspectAchievements(_session.Player);
    }
}