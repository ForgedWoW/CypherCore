// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Groups;
using Framework.Constants;
using Framework.Util;

namespace Forged.MapServer.Entities.Players;

public partial class Player
{
    public PartyResult CanUninviteFromGroup(ObjectGuid guidMember = default)
    {
        var grp = Group;

        if (!grp)
            return PartyResult.NotInGroup;

        if (grp.IsLFGGroup)
        {
            var gguid = grp.GUID;

            if (LFGManager.GetKicksLeft(gguid) == 0)
                return PartyResult.PartyLfgBootLimit;

            var state = LFGManager.GetState(gguid);

            if (LFGManager.IsVoteKickActive(gguid))
                return PartyResult.PartyLfgBootInProgress;

            if (grp.MembersCount <= SharedConst.LFGKickVotesNeeded)
                return PartyResult.PartyLfgBootTooFewPlayers;

            if (state == LfgState.FinishedDungeon)
                return PartyResult.PartyLfgBootDungeonComplete;

            var player = ObjectAccessor.FindConnectedPlayer(guidMember);

            if (!player._lootRolls.Empty())
                return PartyResult.PartyLfgBootLootRolls;

            // @todo Should also be sent when anyone has recently left combat, with an aprox ~5 seconds timer.
            for (var refe = grp.FirstMember; refe != null; refe = refe.Next())
                if (refe.Source && refe.Source.Location.IsInMap(this) && refe.Source.IsInCombat)
                    return PartyResult.PartyLfgBootInCombat;

            /* Missing support for these types
                return ERR_PARTY_LFG_BOOT_COOLDOWN_S;
                return ERR_PARTY_LFG_BOOT_NOT_ELIGIBLE_S;
            */
        }
        else
        {
            if (!grp.IsLeader(GUID) && !grp.IsAssistant(GUID))
                return PartyResult.NotLeader;

            if (InBattleground)
                return PartyResult.InviteRestricted;

            if (grp.IsLeader(guidMember))
                return PartyResult.NotLeader;
        }

        return PartyResult.Ok;
    }

    public bool IsAtGroupRewardDistance(WorldObject pRewardSource)
    {
        if (!pRewardSource || !Location.IsInMap(pRewardSource))
            return false;

        WorldObject player = GetCorpse();

        if (!player || IsAlive)
            player = this;

        if (player.Location.Map.IsDungeon)
            return true;

        return pRewardSource.Location.GetDistance(player) <= Configuration.GetDefaultValue("MaxGroupXPDistance", 74.0f);
    }

    public bool IsGroupVisibleFor(Player p)
    {
        return Configuration.GetDefaultValue("Visibility:GroupMode", 1) switch
        {
            1 => IsInSameRaidWith(p),
            2 => Team == p.Team,
            3 => false,
            _ => IsInSameGroupWith(p)
        };
    }

    public bool IsInGroup(ObjectGuid groupGuid)
    {
        var group = Group;

        if (group != null)
            if (group.GUID == groupGuid)
                return true;

        var originalGroup = OriginalGroup;

        if (originalGroup == null)
            return false;

        return originalGroup.GUID == groupGuid;
    }

    public bool IsInSameGroupWith(Player p)
    {
        return p == this ||
               (Group &&
                Group == p.Group &&
                Group.SameSubGroup(this, p));
    }

    public bool IsInSameRaidWith(Player p)
    {
        return p == this || (Group != null && Group == p.Group);
    }

    public int NextGroupUpdateSequenceNumber(GroupCategory category)
    {
        return _groupUpdateSequences[(int)category].UpdateSequenceNumber++;
    }

    public void RemoveFromBattlegroundOrBattlefieldRaid()
    {
        //remove existing reference
        GroupRef.Unlink();
        var group = OriginalGroup;

        if (group)
        {
            GroupRef.Link(group, this);
            GroupRef.SubGroup = OriginalSubGroup;
        }

        SetOriginalGroup(null);
    }

    public void RemoveFromGroup(RemoveMethod method = RemoveMethod.Default)
    {
        PlayerComputators.RemoveFromGroup(Group, GUID, method);
    }

    public void RemoveGroupUpdateFlag(GroupUpdateFlags flag)
    {
        GroupUpdateFlag &= ~flag;
    }

    public void ResetGroupUpdateSequenceIfNeeded(PlayerGroup group)
    {
        var category = group.GroupCategory;

        // Rejoining the last group should not reset the sequence
        if (_groupUpdateSequences[(int)category].GroupGuid == group.GUID)
            return;

        GroupUpdateCounter groupUpdate;
        groupUpdate.GroupGuid = group.GUID;
        groupUpdate.UpdateSequenceNumber = 1;
        _groupUpdateSequences[(int)category] = groupUpdate;
    }

    public void SetBattlegroundOrBattlefieldRaid(PlayerGroup group, byte subgroup)
    {
        //we must move references from m_group to m_originalGroup
        SetOriginalGroup(Group, SubGroup);

        GroupRef.Unlink();
        GroupRef.Link(group, this);
        GroupRef.SubGroup = subgroup;
    }
    public void SetGroup(PlayerGroup group, byte subgroup = 0)
    {
        if (!group)
        {
            GroupRef.Unlink();
        }
        else
        {
            GroupRef.Link(group, this);
            GroupRef.SubGroup = subgroup;
        }

        UpdateObjectVisibility(false);
    }

    public void SetGroupUpdateFlag(GroupUpdateFlags flag)
    {
        GroupUpdateFlag |= flag;
    }

    public void SetOriginalGroup(PlayerGroup group, byte subgroup = 0)
    {
        if (!group)
        {
            OriginalGroupRef.Unlink();
        }
        else
        {
            OriginalGroupRef.Link(group, this);
            OriginalGroupRef.SubGroup = subgroup;
        }
    }
    public void SetPartyType(GroupCategory category, GroupType type)
    {
        byte value = PlayerData.PartyType;
        value &= (byte)~(0xFF << ((byte)category * 4));
        value |= (byte)((byte)type << ((byte)category * 4));
        SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.PartyType), value);
    }
    public void UninviteFromGroup()
    {
        var group = GroupInvite;

        if (!group)
            return;

        group.RemoveInvite(this);

        if (group.IsCreated)
        {
            if (group.MembersCount <= 1) // group has just 1 member => disband
                group.Disband(true);
        }
        else
        {
            if (group.InviteeCount <= 1)
                group.RemoveAllInvites();
        }
    }
    private Player GetNextRandomRaidMember(float radius)
    {
        var group = Group;

        if (!group)
            return null;

        List<Player> nearMembers = new();

        for (var refe = group.FirstMember; refe != null; refe = refe.Next())
        {
            var target = refe.Source;

            // IsHostileTo check duel and controlled by enemy
            if (target &&
                target != this &&
                Location.IsWithinDistInMap(target, radius) &&
                !target.HasInvisibilityAura &&
                !WorldObjectCombat.IsHostileTo(target))
                nearMembers.Add(target);
        }

        if (nearMembers.Empty())
            return null;

        var randTarget = RandomHelper.IRand(0, nearMembers.Count - 1);

        return nearMembers[randTarget];
    }

    private void SendUpdateToOutOfRangeGroupMembers()
    {
        if (GroupUpdateFlag == GroupUpdateFlags.None)
            return;

        var group = Group;

        if (group)
            group.UpdatePlayerOutOfRange(this);

        GroupUpdateFlag = GroupUpdateFlags.None;

        var pet = CurrentPet;

        if (pet)
            pet.ResetGroupUpdateFlag();
    }
}