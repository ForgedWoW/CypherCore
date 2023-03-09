// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Game.DataStorage;
using Game.DungeonFinding;
using Game.Entities;
using Game.Groups;
using Game.Maps;

namespace Game.Chat
{
    [CommandGroup("group")]
    class GroupCommands
    {
        [Command("disband", RBACPermissions.CommandGroupDisband)]
        static bool HandleGroupDisbandCommand(CommandHandler handler, string name)
        {
            if (!handler.GetPlayerGroupAndGUIDByName(name, out Player player, out PlayerGroup group, out _))
                return false;

            if (!group)
            {
                handler.SendSysMessage(CypherStrings.GroupNotInGroup, player.GetName());
                return false;
            }

            group.Disband();
            return true;
        }

        [Command("join", RBACPermissions.CommandGroupJoin)]
        static bool HandleGroupJoinCommand(CommandHandler handler, string playerNameGroup, string playerName)
        {
            if (!handler.GetPlayerGroupAndGUIDByName(playerNameGroup, out Player playerSource, out PlayerGroup groupSource, out _, true))
                return false;

            if (!groupSource)
            {
                handler.SendSysMessage(CypherStrings.GroupNotInGroup, playerSource.GetName());
                return false;
            }

            if (!handler.GetPlayerGroupAndGUIDByName(playerName, out Player playerTarget, out PlayerGroup groupTarget, out _, true))
                return false;

            if (groupTarget || playerTarget.Group == groupSource)
            {
                handler.SendSysMessage(CypherStrings.GroupAlreadyInGroup, playerTarget.GetName());
                return false;
            }

            if (groupSource.IsFull)
            {
                handler.SendSysMessage(CypherStrings.GroupFull);
                return false;
            }

            groupSource.AddMember(playerTarget);
            groupSource.BroadcastGroupUpdate();
            handler.SendSysMessage(CypherStrings.GroupPlayerJoined, playerTarget.GetName(), playerSource.GetName());
            return true;
        }

        [Command("leader", RBACPermissions.CommandGroupLeader)]
        static bool HandleGroupLeaderCommand(CommandHandler handler, string name)
        {
            if (!handler.GetPlayerGroupAndGUIDByName(name, out Player player, out PlayerGroup group, out ObjectGuid guid))
                return false;

            if (!group)
            {
                handler.SendSysMessage(CypherStrings.GroupNotInGroup, player.GetName());
                return false;
            }

            if (group.LeaderGUID != guid)
            {
                group.ChangeLeader(guid);
                group.SendUpdate();
            }

            return true;
        }

        [Command("level", RBACPermissions.CommandCharacterLevel, true)]
        static bool HandleGroupLevelCommand(CommandHandler handler, PlayerIdentifier player, short level)
        {
            if (level < 1)
                return false;

            if (player == null)
                player = PlayerIdentifier.FromTargetOrSelf(handler);
            if (player == null)
                return false;

            Player target = player.GetConnectedPlayer();
            if (target == null)
                return false;

            PlayerGroup groupTarget = target.Group;
            if (groupTarget == null)
                return false;

            for (GroupReference it = groupTarget.FirstMember; it != null; it = it.Next())
            {
                target = it.Source;
                if (target != null)
                {
                    uint oldlevel = target.Level;

                    if (level != oldlevel)
                    {
                        target.SetLevel((uint)level);
                        target.InitTalentForLevel();
                        target.                        XP = 0;
                    }

                    if (handler.NeedReportToTarget(target))
                    {
                        if (oldlevel < level)
                            target.SendSysMessage(CypherStrings.YoursLevelUp, handler.GetNameLink(), level);
                        else                                                // if (oldlevel > newlevel)
                            target.SendSysMessage(CypherStrings.YoursLevelDown, handler.GetNameLink(), level);
                    }
                }
            }
            return true;
        }

        [Command("list", RBACPermissions.CommandGroupList)]
        static bool HandleGroupListCommand(CommandHandler handler, StringArguments args)
        {
            // Get ALL the variables!
            Player playerTarget;
            ObjectGuid guidTarget;
            string zoneName = "";
            string onlineState;

            // Parse the guid to uint32...
            ObjectGuid parseGUID = ObjectGuid.Create(HighGuid.Player, args.NextUInt64());

            // ... and try to extract a player out of it.
            if (Global.CharacterCacheStorage.GetCharacterNameByGuid(parseGUID, out string nameTarget))
            {
                playerTarget = Global.ObjAccessor.FindPlayer(parseGUID);
                guidTarget = parseGUID;
            }
            // If not, we return false and end right away.
            else if (!handler.ExtractPlayerTarget(args, out playerTarget, out guidTarget, out nameTarget))
                return false;

            // Next, we need a group. So we define a group variable.
            PlayerGroup groupTarget = null;

            // We try to extract a group from an online player.
            if (playerTarget)
                groupTarget = playerTarget.Group;

            // If not, we extract it from the SQL.
            if (!groupTarget)
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_GROUP_MEMBER);
                stmt.AddValue(0, guidTarget.Counter);
                SQLResult resultGroup = DB.Characters.Query(stmt);
                if (!resultGroup.IsEmpty())
                    groupTarget = Global.GroupMgr.GetGroupByDbStoreId(resultGroup.Read<uint>(0));
            }

            // If both fails, players simply has no party. Return false.
            if (!groupTarget)
            {
                handler.SendSysMessage(CypherStrings.GroupNotInGroup, nameTarget);
                return false;
            }

            // We get the group members after successfully detecting a group.
            var members = groupTarget.MemberSlots;

            // To avoid a cluster fuck, namely trying multiple queries to simply get a group member count...
            handler.SendSysMessage(CypherStrings.GroupType, (groupTarget.IsRaidGroup ? "raid" : "party"), members.Count);
            // ... we simply move the group type and member count print after retrieving the slots and simply output it's size.

            // While rather dirty codestyle-wise, it saves space (if only a little). For each member, we look several informations up.
            foreach (var slot in members)
            {
                // Check for given flag and assign it to that iterator
                string flags = "";
                if (slot.Flags.HasAnyFlag(GroupMemberFlags.Assistant))
                    flags = "Assistant";

                if (slot.Flags.HasAnyFlag(GroupMemberFlags.MainTank))
                {
                    if (!string.IsNullOrEmpty(flags))
                        flags += ", ";
                    flags += "MainTank";
                }

                if (slot.Flags.HasAnyFlag(GroupMemberFlags.MainAssist))
                {
                    if (!string.IsNullOrEmpty(flags))
                        flags += ", ";
                    flags += "MainAssist";
                }

                if (string.IsNullOrEmpty(flags))
                    flags = "None";

                // Check if iterator is online. If is...
                Player p = Global.ObjAccessor.FindPlayer(slot.Guid);
                string phases = "";
                if (p && p.IsInWorld)
                {
                    // ... than, it prints information like "is online", where he is, etc...
                    onlineState = "online";
                    phases = PhasingHandler.FormatPhases(p.PhaseShift);

                    AreaTableRecord area = CliDB.AreaTableStorage.LookupByKey(p.Area);
                    if (area != null)
                    {
                        AreaTableRecord zone = CliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);
                        if (zone != null)
                            zoneName = zone.AreaName[handler.GetSessionDbcLocale()];
                    }
                }
                else
                {
                    // ... else, everything is set to offline or neutral values.
                    zoneName = "<ERROR>";
                    onlineState = "Offline";
                }

                // Now we can print those informations for every single member of each group!
                handler.SendSysMessage(CypherStrings.GroupPlayerNameGuid, slot.Name, onlineState,
                    zoneName, phases, slot.Guid.ToString(), flags, LFGQueue.GetRolesString(slot.Roles));
            }

            // And finish after every iterator is done.
            return true;
        }

        [Command("remove", RBACPermissions.CommandGroupRemove)]
        static bool HandleGroupRemoveCommand(CommandHandler handler, string name)
        {
            if (!handler.GetPlayerGroupAndGUIDByName(name, out Player player, out PlayerGroup group, out ObjectGuid guid))
                return false;

            if (!group)
            {
                handler.SendSysMessage(CypherStrings.GroupNotInGroup, player.GetName());
                return false;
            }

            group.RemoveMember(guid);
            return true;
        }

        [Command("repair", RBACPermissions.CommandRepairitems, true)]
        static bool HandleGroupRepairCommand(CommandHandler handler, PlayerIdentifier playerTarget)
        {
            if (playerTarget == null)
                playerTarget = PlayerIdentifier.FromTargetOrSelf(handler);
            if (playerTarget == null || !playerTarget.IsConnected())
                return false;

            PlayerGroup groupTarget = playerTarget.GetConnectedPlayer().Group;
            if (groupTarget == null)
                return false;

            for (GroupReference it = groupTarget.FirstMember; it != null; it = it.Next())
            {
                Player target = it.Source;
                if (target != null)
                    target.DurabilityRepairAll(false, 0, false);
            }

            return true;
        }

        [Command("revive", RBACPermissions.CommandRevive, true)]
        static bool HandleGroupReviveCommand(CommandHandler handler, PlayerIdentifier playerTarget)
        {
            if (playerTarget == null)
                playerTarget = PlayerIdentifier.FromTargetOrSelf(handler);
            if (playerTarget == null || !playerTarget.IsConnected())
                return false;

            PlayerGroup groupTarget = playerTarget.GetConnectedPlayer().Group;
            if (groupTarget == null)
                return false;

            for (GroupReference it = groupTarget.FirstMember; it != null; it = it.Next())
            {
                Player target = it.Source;
                if (target)
                {
                    target.ResurrectPlayer(target.Session.HasPermission(RBACPermissions.ResurrectWithFullHps) ? 1.0f : 0.5f);
                    target.SpawnCorpseBones();
                    target.SaveToDB();
                }
            }

            return true;
        }

        [Command("summon", RBACPermissions.CommandGroupSummon)]
        static bool HandleGroupSummonCommand(CommandHandler handler, PlayerIdentifier playerTarget)
        {
            if (playerTarget == null)
                playerTarget = PlayerIdentifier.FromTargetOrSelf(handler);
            if (playerTarget == null || !playerTarget.IsConnected())
                return false;

            Player target = playerTarget.GetConnectedPlayer();

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            PlayerGroup group = target.Group;

            string nameLink = handler.GetNameLink(target);

            if (!group)
            {
                handler.SendSysMessage(CypherStrings.NotInGroup, nameLink);
                return false;
            }

            Player gmPlayer = handler.GetSession().Player;
            Map gmMap = gmPlayer.Map;
            bool toInstance = gmMap.Instanceable;
            bool onlyLocalSummon = false;

            // make sure people end up on our instance of the map, disallow far summon if intended destination is different from actual destination
            // note: we could probably relax this further by checking permanent saves and the like, but eh
            // :close enough:
            if (toInstance)
            {
                Player groupLeader = Global.ObjAccessor.GetPlayer(gmMap, group.LeaderGUID);
                if (!groupLeader || (groupLeader.Location.MapId != gmMap.Id) || (groupLeader.InstanceId1 != gmMap.InstanceId))
                {
                    handler.SendSysMessage(CypherStrings.PartialGroupSummon);
                    onlyLocalSummon = true;
                }
            }

            for (GroupReference refe = group.FirstMember; refe != null; refe = refe.Next())
            {
                Player player = refe.Source;

                if (!player || player == gmPlayer || player.Session == null)
                    continue;

                // check online security
                if (handler.HasLowerSecurity(player, ObjectGuid.Empty))
                    continue;

                string plNameLink = handler.GetNameLink(player);

                if (player.IsBeingTeleported)
                {
                    handler.SendSysMessage(CypherStrings.IsTeleported, plNameLink);
                    continue;
                }

                if (toInstance)
                {
                    Map playerMap = player.Map;

                    if ((onlyLocalSummon || (playerMap.Instanceable && playerMap.Id == gmMap.Id)) && // either no far summon allowed or we're in the same map as player (no map switch)
                        ((playerMap.Id != gmMap.Id) || (playerMap.InstanceId != gmMap.InstanceId))) // so we need to be in the same map and instance of the map, otherwise skip
                    {
                        // cannot summon from instance to instance
                        handler.SendSysMessage(CypherStrings.CannotSummonInstInst, plNameLink);
                        continue;
                    }
                }

                handler.SendSysMessage(CypherStrings.Summoning, plNameLink, "");
                if (handler.NeedReportToTarget(player))
                    player.SendSysMessage(CypherStrings.SummonedBy, handler.GetNameLink());

                // stop flight if need
                if (player.IsInFlight)
                    player.FinishTaxiFlight();                
                else
                    player.SaveRecallPosition(); // save only in non-flight case

                // before GM
                var pos = new Position();
                gmPlayer.GetClosePoint(pos, player.CombatReach);
                pos.Orientation = player.Location.Orientation;
                player.TeleportTo(gmPlayer.Location.MapId, pos, 0, gmPlayer.InstanceId1);
            }

            return true;
        }

        [CommandGroup("set")]
        class GroupSetCommands
        {
            [Command("assistant", RBACPermissions.CommandGroupAssistant)]
            static bool HandleGroupSetAssistantCommand(CommandHandler handler, string name)
            {
                return GroupFlagCommand(name, handler, GroupMemberFlags.Assistant);
            }

            [Command("leader", RBACPermissions.CommandGroupLeader)]
            static bool HandleGroupSetLeaderCommand(CommandHandler handler, string name)
            {
                return HandleGroupLeaderCommand(handler, name);
            }

            [Command("mainassist", RBACPermissions.CommandGroupMainassist)]
            static bool HandleGroupSetMainAssistCommand(CommandHandler handler, string name)
            {
                return GroupFlagCommand(name, handler, GroupMemberFlags.MainAssist);
            }

            [Command("maintank", RBACPermissions.CommandGroupMaintank)]
            static bool HandleGroupSetMainTankCommand(CommandHandler handler, string name)
            {
                return GroupFlagCommand(name, handler, GroupMemberFlags.MainTank);
            }

            static bool GroupFlagCommand(string name, CommandHandler handler, GroupMemberFlags flag)
            {
                if (!handler.GetPlayerGroupAndGUIDByName(name, out Player player, out PlayerGroup group, out ObjectGuid guid))
                    return false;

                if (!group)
                {
                    handler.SendSysMessage(CypherStrings.NotInGroup, player.GetName());
                    return false;
                }

                if (!group.IsRaidGroup)
                {
                    handler.SendSysMessage(CypherStrings.GroupNotInRaidGroup, player.GetName());
                    return false;
                }

                if (flag == GroupMemberFlags.Assistant && group.IsLeader(guid))
                {
                    handler.SendSysMessage(CypherStrings.LeaderCannotBeAssistant, player.GetName());
                    return false;
                }

                if (group.GetMemberFlags(guid).HasAnyFlag(flag))
                {
                    group.SetGroupMemberFlag(guid, false, flag);
                    handler.SendSysMessage(CypherStrings.GroupRoleChanged, player.GetName(), "no longer", flag);
                }
                else
                {
                    group.SetGroupMemberFlag(guid, true, flag);
                    handler.SendSysMessage(CypherStrings.GroupRoleChanged, player.GetName(), "now", flag);
                }
                return true;
            }
        }
    }
}
