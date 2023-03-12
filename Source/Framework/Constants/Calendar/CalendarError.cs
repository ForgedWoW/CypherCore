// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum CalendarError
{
	Ok = 0,
	GuildEventsExceeded = 1,
	EventsExceeded = 2,
	SelfInvitesExceeded = 3,
	OtherInvitesExceeded = 4,
	Permissions = 5,
	EventInvalid = 6,
	NotInvited = 7,
	Internal = 8,
	GuildPlayerNotInGuild = 9,
	AlreadyInvitedToEventS = 10,
	PlayerNotFound = 11,
	NotAllied = 12,
	IgnoringYouS = 13,
	InvitesExceeded = 14,
	InvalidDate = 16,
	InvalidTime = 17,

	NeedsTitle = 19,
	EventPassed = 20,
	EventLocked = 21,
	DeleteCreatorFailed = 22,
	SystemDisabled = 24,
	RestrictedAccount = 25,
	ArenaEventsExceeded = 26,
	RestrictedLevel = 27,
	UserSquelched = 28,
	NoInvite = 29,

	EventWrongServer = 36,
	InviteWrongServer = 37,
	NoGuildInvites = 38,
	InvalidSignup = 39,
	NoModerator = 40
}