// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum MailCheckMask
{
    None = 0x00,
    Read = 0x01,
    Returned = 0x02, // This Mail Was Returned. Do Not Allow Returning Mail Back Again.
    Copied = 0x04,   // This Mail Was Copied. Do Not Allow Making A Copy Of Items In Mail.
    CodPayment = 0x08,
    HasBody = 0x10 // This Mail Has Body Text.
}