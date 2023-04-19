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
    public LFGPlayerScript() : base("LFGPlayerScript") { }
    public PlayerClass PlayerClass { get; } = PlayerClass.None;

    public void OnLogin(Player player)
    {
        if (!player.LFGManager.IsOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser))
            return;

        // Temporal: Trying to determine when group data and LFG data gets desynched
        var guid = player.GUID;
        var gguid = player.LFGManager.GetGroup(guid);


        if (player.Group != null)
        {
            var gguid2 = player.Group.GUID;

            if (gguid != gguid2)
            {
                Log.Logger.Error("{0} on group {1} but LFG has group {2} saved... Fixing.", player.Session.GetPlayerInfo(), gguid2.ToString(), gguid.ToString());
                player.LFGManager.SetupGroupMember(guid, player.Group.GUID);
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

        if (player.Group == null)
            player.LFGManager.LeaveLfg(player.GUID);
        else if (player.Session.PlayerDisconnected)
            player.LFGManager.LeaveLfg(player.GUID, true);
    }

    public void OnMapChanged(Player player)
    {
        var map = player.Location.Map;

        if (player.LFGManager.InLfgDungeonMap(player.GUID, map.Id, map.DifficultyID))
        {
            // This function is also called when players log in
            // if for some reason the LFG system recognises the player as being in a LFG dungeon,
            // but the player was loaded without a valid group, we'll teleport to homebind to prevent
            // crashes or other undefined behaviour
            if (player.Group == null)
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

            foreach (var memberSlot in player.Group.MemberSlots)
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
            if (player.Group is { MembersCount: 1 })
            {
                player.LFGManager.LeaveLfg(player.Group.GUID);
                player.Group.Disband();

                Log.Logger.Debug("LFGPlayerScript::OnMapChanged, Player {0}({1}) is last in the lfggroup so we disband the group.",
                                 player.GetName(),
                                 player.GUID.ToString());
            }

            player.RemoveAura(SharedConst.LFGSpellLuckOfTheDraw);
        }
    }
}