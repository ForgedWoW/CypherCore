// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.O;

public sealed class OverrideSpellDataRecord
{
    public byte Flags;
    public uint Id;
    public uint PlayerActionBarFileDataID;
    public uint[] Spells = new uint[SharedConst.MaxOverrideSpell];
}