﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.Structs.C;
using Framework.Constants;

namespace Forged.MapServer.Achievements;

public class Criteria
{
    public CriteriaRecord Entry;
    public CriteriaFlagsCu FlagsCu;
    public uint Id;
    public ModifierTreeNode Modifier;
}