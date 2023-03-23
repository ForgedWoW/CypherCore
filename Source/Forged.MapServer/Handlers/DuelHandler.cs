// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Duel;

namespace Game;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.CanDuel)]
	void HandleCanDuel(CanDuel packet)
	{
		var player = Global.ObjAccessor.FindPlayer(packet.TargetGUID);

		if (!player)
			return;

		CanDuelResult response = new();
		response.TargetGUID = packet.TargetGUID;
		response.Result = player.Duel == null;
		SendPacket(response);

		if (response.Result)
		{
			if (Player.IsMounted)
				Player.CastSpell(player, 62875);
			else
				Player.CastSpell(player, 7266);
		}
	}

	[WorldPacketHandler(ClientOpcodes.DuelResponse)]
	void HandleDuelResponse(DuelResponse duelResponse)
	{
		if (duelResponse.Accepted && !duelResponse.Forfeited)
			HandleDuelAccepted(duelResponse.ArbiterGUID);
		else
			HandleDuelCancelled();
	}

	void HandleDuelAccepted(ObjectGuid arbiterGuid)
	{
		var player = Player;

		if (player.Duel == null || player == player.Duel.Initiator || player.Duel.State != DuelState.Challenged)
			return;

		var target = player.Duel.Opponent;

		if (target.PlayerData.DuelArbiter != arbiterGuid)
			return;

		Log.outDebug(LogFilter.Network, "Player 1 is: {0} ({1})", player.GUID.ToString(), player.GetName());
		Log.outDebug(LogFilter.Network, "Player 2 is: {0} ({1})", target.GUID.ToString(), target.GetName());

		var now = GameTime.GetGameTime();
		player.Duel.StartTime = now + 3;
		target.Duel.StartTime = now + 3;

		player.Duel.State = DuelState.Countdown;
		target.Duel.State = DuelState.Countdown;

		DuelCountdown packet = new(3000);

		player.SendPacket(packet);
		target.SendPacket(packet);

		player.EnablePvpRules();
		target.EnablePvpRules();
	}

	void HandleDuelCancelled()
	{
		var player = Player;

		// no duel requested
		if (player.Duel == null || player.Duel.State == DuelState.Completed)
			return;

		// player surrendered in a duel using /forfeit
		if (player.Duel.State == DuelState.InProgress)
		{
			player.CombatStopWithPets(true);
			player.Duel.Opponent.CombatStopWithPets(true);

			player.CastSpell(Player, 7267, true); // beg
			player.DuelComplete(DuelCompleteType.Won);

			return;
		}

		player.DuelComplete(DuelCompleteType.Interrupted);
	}
}