// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum ReputationFlags : ushort
{
	None = 0x00,
	Visible = 0x01, // makes visible in client (set or can be set at interaction with target of this faction)
	AtWar = 0x02,   // enable AtWar-button in client. player controlled (except opposition team always war state), Flag only set on initial creation
	Hidden = 0x04,  // hidden faction from reputation pane in client (player can gain reputation, but this update not sent to client)
	Header = 0x08,  // Display as header in UI
	Peaceful = 0x10,
	Inactive = 0x20, // player controlled (CMSG_SET_FACTION_INACTIVE)
	ShowPropagated = 0x40,
	HeaderShowsBar = 0x80, // Header has its own reputation bar
	CapitalCityForRaceChange = 0x100,
	Guild = 0x200,
	GarrisonInvasion = 0x400
}