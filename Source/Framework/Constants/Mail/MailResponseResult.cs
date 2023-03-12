// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum MailResponseResult
{
	Ok = 0,
	EquipError = 1,
	CannotSendToSelf = 2,
	NotEnoughMoney = 3,
	RecipientNotFound = 4,
	NotYourTeam = 5,
	InternalError = 6,
	DisabledForTrialAcc = 14,
	RecipientCapReached = 15,
	CantSendWrappedCod = 16,
	MailAndChatSuspended = 17,
	TooManyAttachments = 18,
	MailAttachmentInvalid = 19,
	ItemHasExpired = 21
}