// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record CriteriaTreeRecord
{
    public uint Amount;
    public uint CriteriaID;
    public string Description;
    public CriteriaTreeFlags Flags;
    public uint Id;
    public int Operator;
    public int OrderIndex;
    public uint Parent;
}