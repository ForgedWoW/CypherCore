// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellTotemsRecord
{
    public uint Id;
    public uint SpellID;
    public ushort[] RequiredTotemCategoryID = new ushort[SpellConst.MaxTotems];
    public uint[] Totem = new uint[SpellConst.MaxTotems];
}