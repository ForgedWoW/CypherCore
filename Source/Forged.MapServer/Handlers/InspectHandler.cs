// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Inspect;
using Forged.MapServer.Networking.Packets.Trait;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

namespace Forged.MapServer.Handlers;

public class InspectHandler : IWorldSessionHandler
{
	[WorldPacketHandler(ClientOpcodes.Inspect, Processing = PacketProcessing.Inplace)]
    private void HandleInspect(Inspect inspect)
	{
		var player = Global.ObjAccessor.GetPlayer(_player, inspect.Target);

		if (!player)
		{
			Log.Logger.Debug("WorldSession.HandleInspectOpcode: Target {0} not found.", inspect.Target.ToString());

			return;
		}

		if (!Player.IsWithinDistInMap(player, SharedConst.InspectDistance, false))
			return;

		if (Player.IsValidAttackTarget(player))
			return;

		InspectResult inspectResult = new();
		inspectResult.DisplayInfo.Initialize(player);

		if (Player.CanBeGameMaster || GetDefaultValue("TalentsInspecting", 1) + (Player.EffectiveTeam == player.EffectiveTeam ? 1 : 0) > 1)
		{
			var talents = player.GetTalentMap(player.GetActiveTalentGroup());

			foreach (var v in talents)
				if (v.Value != PlayerSpellState.Removed)
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

		var guild = Global.GuildMgr.GetGuildById(player.GuildId);

		if (guild)
		{
			InspectGuildData guildData;
			guildData.GuildGUID = guild.GetGUID();
			guildData.NumGuildMembers = guild.GetMembersCount();
			guildData.AchievementPoints = (int)guild.GetAchievementMgr().AchievementPoints;

			inspectResult.GuildData = guildData;
		}

		var heartOfAzeroth = player.GetItemByEntry(PlayerConst.ItemIdHeartOfAzeroth, ItemSearchLocation.Everywhere);

		if (heartOfAzeroth != null)
		{
			var azeriteItem = heartOfAzeroth.AsAzeriteItem;

			if (azeriteItem != null)
				inspectResult.AzeriteLevel = azeriteItem.GetEffectiveLevel();
		}

		inspectResult.ItemLevel = (int)player.GetAverageItemLevel();
		inspectResult.LifetimeMaxRank = player.ActivePlayerData.LifetimeMaxRank;
		inspectResult.TodayHK = player.ActivePlayerData.TodayHonorableKills;
		inspectResult.YesterdayHK = player.ActivePlayerData.YesterdayHonorableKills;
		inspectResult.LifetimeHK = player.ActivePlayerData.LifetimeHonorableKills;
		inspectResult.HonorLevel = player.PlayerData.HonorLevel;

		SendPacket(inspectResult);
	}

	[WorldPacketHandler(ClientOpcodes.QueryInspectAchievements, Processing = PacketProcessing.Inplace)]
    private void HandleQueryInspectAchievements(QueryInspectAchievements inspect)
	{
		var player = Global.ObjAccessor.GetPlayer(_player, inspect.Guid);

		if (!player)
		{
			Log.Logger.Debug("WorldSession.HandleQueryInspectAchievements: [{0}] inspected unknown Player [{1}]", Player.GUID.ToString(), inspect.Guid.ToString());

			return;
		}

		if (!Player.IsWithinDistInMap(player, SharedConst.InspectDistance, false))
			return;

		if (Player.IsValidAttackTarget(player))
			return;

		player.SendRespondInspectAchievements(Player);
	}
}