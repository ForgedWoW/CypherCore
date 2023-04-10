// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Groups;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IGroup;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.DungeonFinding;

internal class LFGGroupScript : ScriptObjectAutoAdd, IGroupOnAddMember, IGroupOnRemoveMember, IGroupOnDisband, IGroupOnChangeLeader, IGroupOnInviteMember
{
    public LFGGroupScript() : base("LFGGroupScript") { }

    // Group Hooks
    public void OnAddMember(PlayerGroup group, ObjectGuid guid)
    {
        if (!Global.LFGMgr.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
            return;

        var gguid = group.GUID;
        var leader = group.LeaderGUID;

        if (leader == guid)
        {
            Log.Logger.Debug("LFGScripts.OnAddMember [{0}]: added [{1} leader {2}]", gguid, guid, leader);
            Global.LFGMgr.SetLeader(gguid, guid);
        }
        else
        {
            var gstate = Global.LFGMgr.GetState(gguid);
            var state = Global.LFGMgr.GetState(guid);
            Log.Logger.Debug("LFGScripts.OnAddMember [{0}]: added [{1} leader {2}] gstate: {3}, state: {4}", gguid, guid, leader, gstate, state);

            if (state == LfgState.Queued)
                Global.LFGMgr.LeaveLfg(guid);

            if (gstate == LfgState.Queued)
                Global.LFGMgr.LeaveLfg(gguid);
        }

        Global.LFGMgr.SetGroup(guid, gguid);
        Global.LFGMgr.AddPlayerToGroup(gguid, guid);
    }

    public void OnChangeLeader(PlayerGroup group, ObjectGuid newLeaderGuid, ObjectGuid oldLeaderGuid)
    {
        if (!Global.LFGMgr.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
            return;

        var gguid = group.GUID;

        Log.Logger.Debug("LFGScripts.OnChangeLeader {0}: old {0} new {0}", gguid, newLeaderGuid, oldLeaderGuid);
        Global.LFGMgr.SetLeader(gguid, newLeaderGuid);
    }

    public void OnDisband(PlayerGroup group)
    {
        if (!Global.LFGMgr.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
            return;

        var gguid = group.GUID;
        Log.Logger.Debug("LFGScripts.OnDisband {0}", gguid);

        Global.LFGMgr.RemoveGroupData(gguid);
    }

    public void OnInviteMember(PlayerGroup group, ObjectGuid guid)
    {
        if (!Global.LFGMgr.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
            return;

        var gguid = group.GUID;
        var leader = group.LeaderGUID;
        Log.Logger.Debug("LFGScripts.OnInviteMember {0}: invite {0} leader {0}", gguid, guid, leader);

        // No gguid ==  new group being formed
        // No leader == after group creation first invite is new leader
        // leader and no gguid == first invite after leader is added to new group (this is the real invite)
        if (!leader.IsEmpty && gguid.IsEmpty)
            Global.LFGMgr.LeaveLfg(leader);
    }

    public void OnRemoveMember(PlayerGroup group, ObjectGuid guid, RemoveMethod method, ObjectGuid kicker, string reason)
    {
        if (!Global.LFGMgr.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
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

            Global.LFGMgr.InitBoot(gguid, kicker, guid, strReason);

            return;
        }

        var state = Global.LFGMgr.GetState(gguid);

        // If group is being formed after proposal success do nothing more
        if (state == LfgState.Proposal && method == RemoveMethod.Default)
        {
            // LfgData: Remove player from group
            Global.LFGMgr.SetGroup(guid, ObjectGuid.Empty);
            Global.LFGMgr.RemovePlayerFromGroup(gguid, guid);

            return;
        }

        Global.LFGMgr.LeaveLfg(guid);
        Global.LFGMgr.SetGroup(guid, ObjectGuid.Empty);
        var players = Global.LFGMgr.RemovePlayerFromGroup(gguid, guid);

        var player = Global.ObjAccessor.FindPlayer(guid);

        if (player)
        {
            if (method == RemoveMethod.Leave &&
                state == LfgState.Dungeon &&
                players >= SharedConst.LFGKickVotesNeeded)
                player.CastSpell(player, SharedConst.LFGSpellDungeonDeserter, true);
            else if (method == RemoveMethod.KickLFG)
                player.RemoveAura(SharedConst.LFGSpellDungeonCooldown);
            //else if (state == LFG_STATE_BOOT)
            // Update internal kick cooldown of kicked

            player. //else if (state == LFG_STATE_BOOT)
                // Update internal kick cooldown of kicked
                Session.SendLfgUpdateStatus(new LfgUpdateData(LfgUpdateType.LeaderUnk1), true);

            if (isLFG && player.Map.IsDungeon) // Teleport player out the dungeon
                Global.LFGMgr.TeleportPlayer(player, true);
        }

        if (isLFG && state != LfgState.FinishedDungeon) // Need more players to finish the dungeon
        {
            var leader = Global.ObjAccessor.FindPlayer(Global.LFGMgr.GetLeader(gguid));

            if (leader)
                leader.Session.SendLfgOfferContinue(Global.LFGMgr.GetDungeon(gguid, false));
        }
    }
}