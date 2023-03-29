// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.AuctionHouse;

public class AuctionSearchClassFilters
{
    public enum FilterType : uint
    {
        SkipClass = 0,
        SkipSubclass = 0xFFFFFFFF,
        SkipInvtype = 0xFFFFFFFF
    }

    public SubclassFilter[] Classes = new SubclassFilter[(int)ItemClass.Max];

    public AuctionSearchClassFilters()
    {
        for (var i = 0; i < (int)ItemClass.Max; ++i)
            Classes[i] = new SubclassFilter();
    }

    public class SubclassFilter
    {
        public FilterType SubclassMask;
        public ulong[] InvTypes = new ulong[ItemConst.MaxItemSubclassTotal];
    }
}