// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.A;

public class AreaPOIStateRecord
{
    public uint AreaPoiID;
    public LocalizedString Description;
    public byte IconEnumValue;
    public uint ID;
    public uint UiTextureAtlasMemberID;
    public byte WorldStateValue;
}