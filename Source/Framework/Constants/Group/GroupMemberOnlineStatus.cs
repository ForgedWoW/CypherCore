// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum GroupMemberOnlineStatus
{
	Offline = 0x00,
	Online = 0x01, // Lua_UnitIsConnected
	PVP = 0x02,    // Lua_UnitIsPVP
	Dead = 0x04,   // Lua_UnitIsDead
	Ghost = 0x08,  // Lua_UnitIsGhost
	PVPFFA = 0x10, // Lua_UnitIsPVPFreeForAll
	Unk3 = 0x20,   // used in calls from Lua_GetPlayerMapPosition/Lua_GetBattlefieldFlagPosition
	AFK = 0x40,    // Lua_UnitIsAFK
	DND = 0x80,    // Lua_UnitIsDND
	RAF = 0x100,
	Vehicle = 0x200, // Lua_UnitInVehicle
}