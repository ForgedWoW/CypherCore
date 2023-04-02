﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.L;

public sealed class LanguagesRecord
{
    public int Flags;
    public uint Id;
    public LocalizedString Name;
    public int UiTextureKitElementCount;
    public int UiTextureKitID;
}