// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum GuildRankRights
{
    None = 0x00,
    GChatListen = 0x01,
    GChatSpeak = 0x02,
    OffChatListen = 0x04,
    OffChatSpeak = 0x08,
    Invite = 0x10,
    Remove = 0x20,
    Roster = 0x40,
    Promote = 0x80,
    Demote = 0x100,
    Unk200 = 0x200,
    Unk400 = 0x400,
    Unk800 = 0x800,
    SetMotd = 0x1000,
    EditPublicNote = 0x2000,
    ViewOffNote = 0x4000,
    EOffNote = 0x8000,
    ModifyGuildInfo = 0x10000,
    WithdrawGoldLock = 0x20000,  // remove money withdraw capacity
    WithdrawRepair = 0x40000,    // withdraw for repair
    WithdrawGold = 0x80000,      // withdraw gold
    CreateGuildEvent = 0x100000, // wotlk
    InAuthenticatedRank = 0x200000,
    EditGuildBankTabInfo = 0x400000,
    Officer = 0x800000,
    All = 0x00DDFFBF
}