// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Forged.RealmServer.DungeonFinding;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking;
using Game.Common.Handlers;
using Forged.RealmServer.Networking.Packets;
using Serilog;
using System.Runtime.Serialization;
using Forged.RealmServer.Cache;
using Forged.RealmServer.Globals;

namespace Forged.RealmServer;

public class LFGHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly GameTime _gameTime;
    private readonly LFGManager _lFGManager;
    private readonly CliDB _cliDB;
    private readonly GameObjectManager _objectManager;
    private readonly CharacterCache _characterCache;

    public LFGHandler(WorldSession session, GameTime gameTime, LFGManager lFGManager, CliDB cliDB, GameObjectManager objectManager, CharacterCache characterCache)
    {
        _session = session;
        _gameTime = gameTime;
        _lFGManager = lFGManager;
        _cliDB = cliDB;
        _objectManager = objectManager;
        _characterCache = characterCache;
    }

	[WorldPacketHandler(ClientOpcodes.DfJoin)]
	void HandleLfgJoin(DFJoin dfJoin)
	{
		if (!_lFGManager.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser) ||
			(_session.Player.Group &&
			_session.Player.Group.LeaderGUID != _session.Player.GUID &&
			(_session.Player.Group.MembersCount == MapConst.MaxGroupSize || !_session.Player.Group.IsLFGGroup)))
			return;

		if (dfJoin.Slots.Empty())
		{
			Log.Logger.Debug("CMSG_DF_JOIN {0} no dungeons selected", _session.GetPlayerInfo());

			return;
		}

		List<uint> newDungeons = new();

		foreach (var slot in dfJoin.Slots)
		{
			var dungeon = slot & 0x00FFFFFF;

			if (_cliDB.LFGDungeonsStorage.ContainsKey(dungeon))
				newDungeons.Add(dungeon);
		}

		Log.Logger.Debug("CMSG_DF_JOIN {0} roles: {1}, Dungeons: {2}", _session.GetPlayerInfo(), dfJoin.Roles, newDungeons.Count);

		_lFGManager.JoinLfg(_session.Player, dfJoin.Roles, newDungeons);
	}

	[WorldPacketHandler(ClientOpcodes.DfLeave)]
	void HandleLfgLeave(DFLeave dfLeave)
	{
		var group = _session.Player.Group;

		Log.Logger.Debug("CMSG_DF_LEAVE {0} in group: {1} sent guid {2}.", _session.GetPlayerInfo(), group ? 1 : 0, dfLeave.Ticket.RequesterGuid.ToString());

		// Check cheating - only leader can leave the queue
		if (!group || group.LeaderGUID == dfLeave.Ticket.RequesterGuid)
			_lFGManager.LeaveLfg(dfLeave.Ticket.RequesterGuid);
	}

	[WorldPacketHandler(ClientOpcodes.DfProposalResponse)]
	void HandleLfgProposalResult(DFProposalResponse dfProposalResponse)
	{
		Log.Logger.Debug("CMSG_LFG_PROPOSAL_RESULT {0} proposal: {1} accept: {2}", _session.GetPlayerInfo(), dfProposalResponse.ProposalID, dfProposalResponse.Accepted ? 1 : 0);
		_lFGManager.UpdateProposal(dfProposalResponse.ProposalID, _session.Player.GUID, dfProposalResponse.Accepted);
	}

	[WorldPacketHandler(ClientOpcodes.DfSetRoles)]
	void HandleLfgSetRoles(DFSetRoles dfSetRoles)
	{
		var guid = _session.Player.GUID;
		var group = _session.Player.Group;

		if (!group)
		{
			Log.Logger.Debug(
						"CMSG_DF_SET_ROLES {0} Not in group",
                        _session.GetPlayerInfo());

			return;
		}

		var gguid = group.GUID;
		Log.Logger.Debug("CMSG_DF_SET_ROLES: Group {0}, Player {1}, Roles: {2}", gguid.ToString(), _session.GetPlayerInfo(), dfSetRoles.RolesDesired);
		_lFGManager.UpdateRoleCheck(gguid, guid, dfSetRoles.RolesDesired);
	}

	[WorldPacketHandler(ClientOpcodes.DfBootPlayerVote)]
	void HandleLfgSetBootVote(DFBootPlayerVote dfBootPlayerVote)
	{
		var guid = _session.Player.GUID;
		Log.Logger.Debug("CMSG_LFG_SET_BOOT_VOTE {0} agree: {1}", _session.GetPlayerInfo(), dfBootPlayerVote.Vote ? 1 : 0);
		_lFGManager.UpdateBoot(guid, dfBootPlayerVote.Vote);
	}

	[WorldPacketHandler(ClientOpcodes.DfTeleport)]
	void HandleLfgTeleport(DFTeleport dfTeleport)
	{
		Log.Logger.Debug("CMSG_DF_TELEPORT {0} out: {1}", _session.GetPlayerInfo(), dfTeleport.TeleportOut ? 1 : 0);
		_lFGManager.TeleportPlayer(_session.Player, dfTeleport.TeleportOut, true);
	}
}