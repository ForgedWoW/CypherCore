// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Groups;

namespace Forged.RealmServer.Entities;

public partial class Player
{
	public bool IsUsingLfg => _lFGManager.GetState(GUID) != LfgState.None;

	public PlayerGroup GroupInvite
	{
		get => _groupInvite;
		set => _groupInvite = value;
	}

	public PlayerGroup Group => _group.Target;

	public GroupReference GroupRef => _group;

	public byte SubGroup => _group.SubGroup;

	public GroupUpdateFlags GroupUpdateFlag => _groupUpdateFlags;

	public PlayerGroup OriginalGroup => _originalGroup.Target;

	public GroupReference OriginalGroupRef => _originalGroup;

	public byte OriginalSubGroup => _originalGroup.SubGroup;

	public bool PassOnGroupLoot
	{
		get => _bPassOnGroupLoot;
		set => _bPassOnGroupLoot = value;
	}

	public bool InRandomLfgDungeon
	{
		get
		{
			if (_lFGManager.SelectedRandomLfgDungeon(GUID))
			{
				var map = Map;

				return _lFGManager.InLfgDungeonMap(GUID, map.Id, map.DifficultyID);
			}

			return false;
		}
	}

	public PartyResult CanUninviteFromGroup(ObjectGuid guidMember = default)
	{
		var grp = Group;

		if (!grp)
			return PartyResult.NotInGroup;

		if (grp.IsLFGGroup)
		{
			var gguid = grp.GUID;

			if (_lFGManager.GetKicksLeft(gguid) == 0)
				return PartyResult.PartyLfgBootLimit;

			var state = _lFGManager.GetState(gguid);

			if (_lFGManager.IsVoteKickActive(gguid))
				return PartyResult.PartyLfgBootInProgress;

			if (grp.MembersCount <= SharedConst.LFGKickVotesNeeded)
				return PartyResult.PartyLfgBootTooFewPlayers;

			if (state == LfgState.FinishedDungeon)
				return PartyResult.PartyLfgBootDungeonComplete;

			var player = _objectAccessor.FindConnectedPlayer(guidMember);

			if (!player._lootRolls.Empty())
				return PartyResult.PartyLfgBootLootRolls;

			// @todo Should also be sent when anyone has recently left combat, with an aprox ~5 seconds timer.
			for (var refe = grp.FirstMember; refe != null; refe = refe.Next())
				if (refe.Source && refe.Source.IsInMap(this) && refe.Source.IsInCombat)
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

	public void SetBattlegroundOrBattlefieldRaid(PlayerGroup group, byte subgroup)
	{
		//we must move references from m_group to m_originalGroup
		SetOriginalGroup(Group, SubGroup);

		_group.Unlink();
		_group.Link(group, this);
		_group.SubGroup = subgroup;
	}

	public void RemoveFromBattlegroundOrBattlefieldRaid()
	{
		//remove existing reference
		_group.Unlink();
		var group = OriginalGroup;

		if (group)
		{
			_group.Link(group, this);
			_group.SubGroup = OriginalSubGroup;
		}

		SetOriginalGroup(null);
	}

	public void SetOriginalGroup(PlayerGroup group, byte subgroup = 0)
	{
		if (!group)
		{
			_originalGroup.Unlink();
		}
		else
		{
			_originalGroup.Link(group, this);
			_originalGroup.SubGroup = subgroup;
		}
	}

	public bool IsInGroup(ObjectGuid groupGuid)
	{
		var group = Group;

		if (group != null)
			if (group.GUID == groupGuid)
				return true;

		var originalGroup = OriginalGroup;

		if (originalGroup != null)
			if (originalGroup.GUID == groupGuid)
				return true;

		return false;
	}

	public void SetGroup(PlayerGroup group, byte subgroup = 0)
	{
		if (!group)
		{
			_group.Unlink();
		}
		else
		{
			_group.Link(group, this);
			_group.SubGroup = subgroup;
		}

		UpdateObjectVisibility(false);
	}

	public void SetPartyType(GroupCategory category, GroupType type)
	{
		byte value = PlayerData.PartyType;
		value &= (byte)~(0xFF << ((byte)category * 4));
		value |= (byte)((byte)type << ((byte)category * 4));
		SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.PartyType), value);
	}

	public void ResetGroupUpdateSequenceIfNeeded(PlayerGroup group)
	{
		var category = group.GroupCategory;

		// Rejoining the last group should not reset the sequence
		if (_groupUpdateSequences[(int)category].GroupGuid != group.GUID)
		{
			GroupUpdateCounter groupUpdate;
			groupUpdate.GroupGuid = group.GUID;
			groupUpdate.UpdateSequenceNumber = 1;
			_groupUpdateSequences[(int)category] = groupUpdate;
		}
	}

	public int NextGroupUpdateSequenceNumber(GroupCategory category)
	{
		return _groupUpdateSequences[(int)category].UpdateSequenceNumber++;
	}

	public bool IsAtGroupRewardDistance(WorldObject pRewardSource)
	{
		if (!pRewardSource || !IsInMap(pRewardSource))
			return false;

		WorldObject player = GetCorpse();

		if (!player || IsAlive)
			player = this;

		if (player.Map.IsDungeon)
			return true;

		return pRewardSource.GetDistance(player) <= _worldConfig.GetFloatValue(WorldCfg.GroupXpDistance);
	}

	public void SetGroupUpdateFlag(GroupUpdateFlags flag)
	{
		_groupUpdateFlags |= flag;
	}

	public void RemoveGroupUpdateFlag(GroupUpdateFlags flag)
	{
		_groupUpdateFlags &= ~flag;
	}

	public bool IsGroupVisibleFor(Player p)
	{
		switch (_worldConfig.GetIntValue(WorldCfg.GroupVisibility))
		{
			default:
				return IsInSameGroupWith(p);
			case 1:
				return IsInSameRaidWith(p);
			case 2:
				return Team == p.Team;
			case 3:
				return false;
		}
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

	public void RemoveFromGroup(RemoveMethod method = RemoveMethod.Default)
	{
		RemoveFromGroup(Group, GUID, method);
	}

	public static void RemoveFromGroup(PlayerGroup group, ObjectGuid guid, RemoveMethod method = RemoveMethod.Default, ObjectGuid kicker = default, string reason = null)
	{
		if (!group)
			return;

		group.RemoveMember(guid, method, kicker, reason);
	}

	Player GetNextRandomRaidMember(float radius)
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
				IsWithinDistInMap(target, radius) &&
				!target.HasInvisibilityAura &&
				!IsHostileTo(target))
				nearMembers.Add(target);
		}

		if (nearMembers.Empty())
			return null;

		var randTarget = RandomHelper.IRand(0, nearMembers.Count - 1);

		return nearMembers[randTarget];
	}

	void SendUpdateToOutOfRangeGroupMembers()
	{
		if (_groupUpdateFlags == GroupUpdateFlags.None)
			return;

		var group = Group;

		if (group)
			group.UpdatePlayerOutOfRange(this);

		_groupUpdateFlags = GroupUpdateFlags.None;

		var pet = CurrentPet;

		if (pet)
			pet.ResetGroupUpdateFlag();
	}
}