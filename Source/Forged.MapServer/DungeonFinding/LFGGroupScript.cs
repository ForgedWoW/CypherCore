// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.OpCodeHandlers;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IGroup;
using Framework.Constants;
using Game.Common;
using Serilog;

namespace Forged.MapServer.DungeonFinding;

internal class LFGGroupScript : ScriptObjectAutoAdd, IGroupOnAddMember, IGroupOnRemoveMember, IGroupOnDisband, IGroupOnChangeLeader, IGroupOnInviteMember
{
    private readonly LFGManager _lfgManager;
    private readonly ObjectAccessor _objectAccessor;

    public LFGGroupScript(ClassFactory classFactory) : base("LFGGroupScript")
    {
        _lfgManager = classFactory.Resolve<LFGManager>();
        _objectAccessor = classFactory.Resolve<ObjectAccessor>();
    }

    // Group Hooks
    public void OnAddMember(PlayerGroup group, ObjectGuid guid)
    {
        if (!_lfgManager.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
            return;

        var gguid = group.GUID;
        var leader = group.LeaderGUID;

        if (leader == guid)
        {
            Log.Logger.Debug("LFGScripts.OnAddMember [{0}]: added [{1} leader {2}]", gguid, guid, leader);
            _lfgManager.SetLeader(gguid, guid);
        }
        else
        {
            var gstate = _lfgManager.GetState(gguid);
            var state = _lfgManager.GetState(guid);
            Log.Logger.Debug("LFGScripts.OnAddMember [{0}]: added [{1} leader {2}] gstate: {3}, state: {4}", gguid, guid, leader, gstate, state);

            if (state == LfgState.Queued)
                _lfgManager.LeaveLfg(guid);

            if (gstate == LfgState.Queued)
                _lfgManager.LeaveLfg(gguid);
        }

        _lfgManager.SetGroup(guid, gguid);
        _lfgManager.AddPlayerToGroup(gguid, guid);
    }

    public void OnChangeLeader(PlayerGroup group, ObjectGuid newLeaderGuid, ObjectGuid oldLeaderGuid)
    {
        if (!_lfgManager.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
            return;

        var gguid = group.GUID;

        Log.Logger.Debug("LFGScripts.OnChangeLeader {0}: old {0} new {0}", gguid, newLeaderGuid, oldLeaderGuid);
        _lfgManager.SetLeader(gguid, newLeaderGuid);
    }

    public void OnDisband(PlayerGroup group)
    {
        if (!_lfgManager.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
            return;

        var gguid = group.GUID;
        Log.Logger.Debug("LFGScripts.OnDisband {0}", gguid);

        _lfgManager.RemoveGroupData(gguid);
    }

    public void OnInviteMember(PlayerGroup group, ObjectGuid guid)
    {
        if (!_lfgManager.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
            return;

        var gguid = group.GUID;
        var leader = group.LeaderGUID;
        Log.Logger.Debug("LFGScripts.OnInviteMember {0}: invite {0} leader {0}", gguid, guid, leader);

        // No gguid ==  new group being formed
        // No leader == after group creation first invite is new leader
        // leader and no gguid == first invite after leader is added to new group (this is the real invite)
        if (!leader.IsEmpty && gguid.IsEmpty)
            _lfgManager.LeaveLfg(leader);
    }

    public void OnRemoveMember(PlayerGroup group, ObjectGuid guid, RemoveMethod method, ObjectGuid kicker, string reason)
    {
        if (!_lfgManager.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
            return;

        var gguid = group.GUID;
        Log.Logger.Debug("LFGScripts.OnRemoveMember [{0}]: remove [{1}] Method: {2} Kicker: {3} Reason: {4}", gguid, guid, method, kicker, reason);

        var isLFG = group.IsLFGGroup;

        if (isLFG && method == RemoveMethod.Kick) // Player have been kicked
        {
            // @todo - Update internal kick cooldown of kicker
            var strReason = "";

            if (!string.IsNullOrEmpty(reason))
                strReason = reason;

            _lfgManager.InitBoot(gguid, kicker, guid, strReason);

            return;
        }

        var state = _lfgManager.GetState(gguid);

        // If group is being formed after proposal success do nothing more
        if (state == LfgState.Proposal && method == RemoveMethod.Default)
        {
            // LfgData: Remove player from group
            _lfgManager.SetGroup(guid, ObjectGuid.Empty);
            _lfgManager.RemovePlayerFromGroup(gguid, guid);

            return;
        }

        _lfgManager.LeaveLfg(guid);
        _lfgManager.SetGroup(guid, ObjectGuid.Empty);
        var players = _lfgManager.RemovePlayerFromGroup(gguid, guid);

        var player = _objectAccessor.FindPlayer(guid);

        if (player != null)
        {
            if (method == RemoveMethod.Leave &&
                state == LfgState.Dungeon &&
                players >= SharedConst.LFGKickVotesNeeded)
                player.SpellFactory.CastSpell(player, SharedConst.LFGSpellDungeonDeserter, true);
            else if (method == RemoveMethod.KickLFG)
                player.RemoveAura(SharedConst.LFGSpellDungeonCooldown);
            //else if (state == LFG_STATE_BOOT)
            // Update internal kick cooldown of kicked

            //else if (state == LFG_STATE_BOOT)
            // Update internal kick cooldown of kicked
            player.Session.PacketRouter.OpCodeHandler<LFGHandler>().SendLfgUpdateStatus(new LfgUpdateData(LfgUpdateType.LeaderUnk1), true);

            if (isLFG && player.Location.Map.IsDungeon) // Teleport player out the dungeon
                _lfgManager.TeleportPlayer(player, true);
        }

        if (!isLFG || state == LfgState.FinishedDungeon) // Need more players to finish the dungeon
            return;

        var leader = _objectAccessor.FindPlayer(_lfgManager.GetLeader(gguid));

        leader?.Session.PacketRouter.OpCodeHandler<LFGHandler>().SendLfgOfferContinue(_lfgManager.GetDungeon(gguid, false));
    }
}