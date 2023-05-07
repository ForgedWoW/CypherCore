// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellReagentsRecord
{
    public uint Id;
    public int[] Reagent = new int[SpellConst.MaxReagents];
    public ushort[] ReagentCount = new ushort[SpellConst.MaxReagents];
    public short[] ReagentRecraftCount = new short[SpellConst.MaxReagents];
    public byte[] ReagentSource = new byte[SpellConst.MaxReagents];
    public uint SpellID;
}