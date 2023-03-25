// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets.Character;
using Forged.RealmServer.Scripting;
using Forged.RealmServer.Scripting.Interfaces.IPlayer;
using Forged.RealmServer.Server;

namespace Forged.RealmServer.Handlers;

public class LogoutHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly ScriptManager _scriptManager;

    public LogoutHandler(WorldSession session, ScriptManager scriptManager)
    {
        _session = session;
        _scriptManager = scriptManager;
    }

    [WorldPacketHandler(ClientOpcodes.LogoutRequest)]
	void HandleLogoutRequest(LogoutRequest packet)
	{


        var instantLogout = (_session.Player.HasPlayerFlag(PlayerFlags.Resting) && !_session.Player.IsInCombat ||
                             _session.Player.IsInFlight ||
                             _session.HasPermission(RBACPermissions.InstantLogout));

		var canLogoutInCombat = _session.Player.HasPlayerFlag(PlayerFlags.Resting);

		var reason = 0;

		if (_session.Player.IsInCombat && !canLogoutInCombat)
			reason = 1;
		else if (_session.Player.IsFalling)
			reason = 3;                               // is jumping or falling
		else if (_session.Player.Duel != null) // is dueling or frozen by GM via freeze command
			reason = 2;                               // FIXME - Need the correct value

        _scriptManager.ForEach<IPlayerCanLogout>(script =>
        {
            if (!script.CanLogout(_session.Player))
                reason = 2;
        });


        LogoutResponse logoutResponse = new();
		logoutResponse.LogoutResult = reason;
		logoutResponse.Instant = instantLogout;
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

        _session.SetLogoutStartTime(_gameTime.GetGameTime);
	}

	[WorldPacketHandler(ClientOpcodes.LogoutCancel)]
	void HandleLogoutCancel(LogoutCancel packet)
	{
		// Player have already logged out serverside, too late to cancel
		if (!_session.Player)
			return;

        _session.SetLogoutStartTime(0);

        _session.SendPacket(new LogoutCancelAck());

		// not remove flags if can't free move - its not set in Logout request code.
		if (_session.Player.CanFreeMove())
		{
            //!we can move again
            _session.Player.SetRooted(false);

            //! Stand Up
            _session.Player.SetStandState(UnitStandStateType.Stand);

            //! DISABLE_ROTATE
            _session.Player.RemoveUnitFlag(UnitFlags.Stunned);
		}
	}
}
