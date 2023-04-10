// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking.Packets.Query;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.DungeonFinding;

internal class LFGPlayerScript : ScriptObjectAutoAdd, IPlayerOnLogout, IPlayerOnLogin, IPlayerOnMapChanged
{
    public PlayerClass PlayerClass { get; } = PlayerClass.None;
    public LFGPlayerScript() : base("LFGPlayerScript") { }

    public void OnLogin(Player player)
    {
        if (!player.LFGManager.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
            return;

        // Temporal: Trying to determine when group data and LFG data gets desynched
        var guid = player.GUID;
        var gguid = player.LFGManager.GetGroup(guid);

        var group = player.Group;

        if (group)
        {
            var gguid2 = group.GUID;

            if (gguid != gguid2)
            {
                Log.Logger.Error("{0} on group {1} but LFG has group {2} saved... Fixing.", player.Session.GetPlayerInfo(), gguid2.ToString(), gguid.ToString());
                player.LFGManager.SetupGroupMember(guid, group.GUID);
            }
        }

        player.LFGManager.SetTeam(player.GUID, player.Team);
        // @todo - Restore LfgPlayerData and send proper status to player if it was in a group
    }

    // Player Hooks
    public void OnLogout(Player player)
    {
        if (!player.LFGManager.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
            return;

        if (!player.Group)
            player.LFGManager.LeaveLfg(player.GUID);
        else if (player.Session.PlayerDisconnected)
            player.LFGManager.LeaveLfg(player.GUID, true);
    }

    public void OnMapChanged(Player player)
    {
        var map = player.Location.Map;

        if (player.LFGManager.InLfgDungeonMap(player.GUID, map.Id, map.DifficultyID))
        {
            var group = player.Group;

            // This function is also called when players log in
            // if for some reason the LFG system recognises the player as being in a LFG dungeon,
            // but the player was loaded without a valid group, we'll teleport to homebind to prevent
            // crashes or other undefined behaviour
            if (!group)
            {
                player.LFGManager.LeaveLfg(player.GUID);
                player.RemoveAura(SharedConst.LFGSpellLuckOfTheDraw);
                player.TeleportTo(player.Homebind);

                Log.Logger.Error("LFGPlayerScript.OnMapChanged, Player {0} ({1}) is in LFG dungeon map but does not have a valid group! Teleporting to homebind.",
                                 player.GetName(),
                                 player.GUID.ToString());

                return;
            }

            QueryPlayerNamesResponse response = new();

            foreach (var memberSlot in group.MemberSlots)
            {
                player.Session.BuildNameQueryData(memberSlot.Guid, out var nameCacheLookupResult);
                response.Players.Add(nameCacheLookupResult);
            }

            player.SendPacket(response);

            if (player.LFGManager.SelectedRandomLfgDungeon(player.GUID))
                player.SpellFactory.CastSpell(player, SharedConst.LFGSpellLuckOfTheDraw, true);
        }
        else
        {
            var group = player.Group;

            if (group && group.MembersCount == 1)
            {
                player.LFGManager.LeaveLfg(group.GUID);
                group.Disband();

                Log.Logger.Debug("LFGPlayerScript::OnMapChanged, Player {0}({1}) is last in the lfggroup so we disband the group.",
                                 player.GetName(),
                                 player.GUID.ToString());
            }

            player.RemoveAura(SharedConst.LFGSpellLuckOfTheDraw);
        }
    }
}