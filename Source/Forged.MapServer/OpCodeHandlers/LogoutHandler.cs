// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Chrono;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Character;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class LogoutHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;

    public LogoutHandler(WorldSession session)
    {
        _session = session;
    }

    [WorldPacketHandler(ClientOpcodes.LogoutCancel)]
    private void HandleLogoutCancel(LogoutCancel packet)
    {
        // Player have already logged out serverside, too late to cancel
        if (_session.Player == null)
            return;

        _session.SetLogoutStartTime(0);

        _session.SendPacket(new LogoutCancelAck());

        // not remove flags if can't free move - its not set in Logout request code.
        if (!_session.Player.CanFreeMove())
            return;

        //!we can move again
        _session.Player.SetRooted(false);

        //! Stand Up
        _session.Player.SetStandState(UnitStandStateType.Stand);

        //! DISABLE_ROTATE
        _session.Player.RemoveUnitFlag(UnitFlags.Stunned);
    }

    [WorldPacketHandler(ClientOpcodes.LogoutRequest)]
    private void HandleLogoutRequest(LogoutRequest packet)
    {
        if (!_session.Player.GetLootGUID().IsEmpty)
            _session.Player.SendLootReleaseAll();

        var instantLogout = _session.Player.HasPlayerFlag(PlayerFlags.Resting) && !_session.Player.IsInCombat ||
                            _session.Player.IsInFlight ||
                            _session.HasPermission(RBACPermissions.InstantLogout);

        var canLogoutInCombat = _session.Player.HasPlayerFlag(PlayerFlags.Resting);

        var reason = 0;

        if (_session.Player.IsInCombat && !canLogoutInCombat)
            reason = 1;
        else if (_session.Player.IsFalling)
            reason = 3;                               // is jumping or falling
        else if (_session.Player.Duel != null || _session.Player.HasAura(9454)) // is dueling or frozen by GM via freeze command
            reason = 2;                               // FIXME - Need the correct value

        LogoutResponse logoutResponse = new()
        {
            LogoutResult = reason,
            Instant = instantLogout
        };

        _session.SendPacket(logoutResponse);

        if (reason != 0)
        {
            _session.SetLogoutStartTime(0);

            return;
        }

        // instant logout in taverns/cities or on taxi or for admins, gm's, mod's if its enabled in worldserver.conf
        if (instantLogout)
        {
            _session.LogoutPlayer(true);

            return;
        }

        // not set flags if player can't free move to prevent lost state at logout cancel
        if (_session.Player.CanFreeMove())
        {
            if (_session.Player.StandState == UnitStandStateType.Stand)
                _session.Player.SetStandState(UnitStandStateType.Sit);

            _session.Player.SetRooted(true);
            _session.Player.SetUnitFlag(UnitFlags.Stunned);
        }

        _session.SetLogoutStartTime(GameTime.CurrentTime);
    }
}