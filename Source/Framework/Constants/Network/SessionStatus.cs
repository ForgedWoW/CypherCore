// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

// Player state
public enum SessionStatus
{
	Authed = 0,               // Player authenticated (_player == NULL, m_playerRecentlyLogout = false or will be reset before handler call, m_GUID have garbage)
	Loggedin,                 // Player in game (_player != NULL, m_GUID == _player.GetGUID(), inWorld())
	Transfer,                 // Player transferring to another map (_player != NULL, m_GUID == _player.GetGUID(), !inWorld())
	LoggedinOrRecentlyLogout, // _player != NULL or _player == NULL && m_playerRecentlyLogout && m_playerLogout, m_GUID store last _player guid)
}