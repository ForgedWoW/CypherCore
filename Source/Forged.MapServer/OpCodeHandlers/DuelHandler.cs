// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Duel;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class DuelHandler : IWorldSessionHandler
{
    private readonly ObjectAccessor _objectAccessor;
    private readonly WorldSession _session;

    public DuelHandler(WorldSession session, ObjectAccessor objectAccessor)
    {
        _session = session;
        _objectAccessor = objectAccessor;
    }

    [WorldPacketHandler(ClientOpcodes.CanDuel)]
    private void HandleCanDuel(CanDuel packet)
    {
        var player = _objectAccessor.FindPlayer(packet.TargetGUID);

        if (player == null)
            return;

        CanDuelResult response = new()
        {
            TargetGUID = packet.TargetGUID,
            Result = player.Duel == null
        };

        _session.SendPacket(response);

        if (!response.Result)
            return;

        _session.Player.SpellFactory.CastSpell(_session.Player.IsMounted ? 62875u : 7266u);
    }

    private void HandleDuelAccepted(ObjectGuid arbiterGuid)
    {
        if (_session.Player.Duel == null || _session.Player == _session.Player.Duel.Initiator || _session.Player.Duel.State != DuelState.Challenged)
            return;

        if (_session.Player.Duel.Opponent.PlayerData.DuelArbiter != arbiterGuid)
            return;

        Log.Logger.Debug("Player 1 is: {0} ({1})", _session.Player.GUID.ToString(), _session.Player.GetName());
        Log.Logger.Debug("Player 2 is: {0} ({1})", _session.Player.Duel.Opponent.GUID.ToString(), _session.Player.Duel.Opponent.GetName());

        var now = GameTime.CurrentTime;
        _session.Player.Duel.StartTime = now + 3;
        _session.Player.Duel.Opponent.Duel.StartTime = now + 3;

        _session.Player.Duel.State = DuelState.Countdown;
        _session.Player.Duel.Opponent.Duel.State = DuelState.Countdown;

        DuelCountdown packet = new(3000);

        _session.Player.SendPacket(packet);
        _session.Player.Duel.Opponent.SendPacket(packet);

        _session.Player.EnablePvpRules();
        _session.Player.Duel.Opponent.EnablePvpRules();
    }

    private void HandleDuelCancelled()
    {
        // no duel requested
        if (_session.Player.Duel == null || _session.Player.Duel.State == DuelState.Completed)
            return;

        // player surrendered in a duel using /forfeit
        if (_session.Player.Duel.State == DuelState.InProgress)
        {
            _session.Player.CombatStopWithPets(true);
            _session.Player.Duel.Opponent.CombatStopWithPets(true);

            _session.Player.SpellFactory.CastSpell(_session.Player, 7267, true); // beg
            _session.Player.DuelComplete(DuelCompleteType.Won);

            return;
        }

        _session.Player.DuelComplete(DuelCompleteType.Interrupted);
    }

    [WorldPacketHandler(ClientOpcodes.DuelResponse)]
    private void HandleDuelResponse(DuelResponse duelResponse)
    {
        if (duelResponse.Accepted && !duelResponse.Forfeited)
            HandleDuelAccepted(duelResponse.ArbiterGUID);
        else
            HandleDuelCancelled();
    }
}