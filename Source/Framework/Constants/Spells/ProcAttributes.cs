// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum ProcAttributes
{
    ReqExpOrHonor = 0x01,             // requires proc target to give exp or honor for aura proc
    TriggeredCanProc = 0x02,          // aura can proc even with triggered spells
    ReqPowerCost = 0x04,              // requires triggering spell to have a power cost for aura proc
    ReqSpellmod = 0x08,               // requires triggering spell to be affected by proccing aura to drop charges
    UseStacksForCharges = 0x10,       // consuming proc drops a stack from proccing aura instead of charge
    ReduceProc60 = 0x80,              // aura should have a reduced chance to proc if level of proc Actor > 60
    CantProcFromItemCast = 0x0000100, // do not allow aura proc if proc is caused by a spell casted by item

    AllAllowed = ReqExpOrHonor | TriggeredCanProc | ReqPowerCost | ReqSpellmod | UseStacksForCharges | ReduceProc60 | CantProcFromItemCast
}