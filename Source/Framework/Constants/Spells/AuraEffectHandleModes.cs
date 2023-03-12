// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum AuraEffectHandleModes
{
	Default = 0x0,
	Real = 0x01,                                // Handler Applies/Removes Effect From Unit
	SendForClient = 0x02,                       // Handler Sends Apply/Remove Packet To Unit
	ChangeAmount = 0x04,                        // Handler Updates Effect On Target After Effect Amount Change
	Reapply = 0x08,                             // Handler Updates Effect On Target After Aura Is Reapplied On Target
	Stat = 0x10,                                // Handler Updates Effect On Target When Stat Removal/Apply Is Needed For Calculations By Core
	Skill = 0x20,                               // Handler Updates Effect On Target When Skill Removal/Apply Is Needed For Calculations By Core
	SendForClientMask = (SendForClient | Real), // Any Case Handler Need To Send Packet
	ChangeAmountMask = (ChangeAmount | Real),   // Any Case Handler Applies Effect Depending On Amount
	ChangeAmountSendForClientMask = (ChangeAmountMask | SendForClientMask),
	RealOrReapplyMask = (Reapply | Real)
}