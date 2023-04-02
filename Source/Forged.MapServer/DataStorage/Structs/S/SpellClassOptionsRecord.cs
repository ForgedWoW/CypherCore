// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Dynamic;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellClassOptionsRecord
{
    public uint Id;
    public uint ModalNextSpell;
    public FlagArray128 SpellClassMask;
    public byte SpellClassSet;
    public uint SpellID;
}