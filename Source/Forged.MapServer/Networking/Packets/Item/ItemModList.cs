// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

public class ItemModList
{
    public Array<ItemMod> Values = new((int)ItemModifier.Max);

    public void Read(WorldPacket data)
    {
        var itemModListCount = data.ReadBits<uint>(6);
        data.ResetBitPos();

        for (var i = 0; i < itemModListCount; ++i)
        {
            var itemMod = new ItemMod();
            itemMod.Read(data);
            Values[i] = itemMod;
        }
    }

    public void Write(WorldPacket data)
    {
        data.WriteBits(Values.Count, 6);
        data.FlushBits();

        foreach (var itemMod in Values)
            itemMod.Write(data);
    }

    public override int GetHashCode()
    {
        return Values.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (obj is ItemModList)
            return (ItemModList)obj == this;

        return false;
    }

    public static bool operator ==(ItemModList left, ItemModList right)
    {
        if (left.Values.Count != right.Values.Count)
            return false;

        return !left.Values.Except(right.Values).Any();
    }

    public static bool operator !=(ItemModList left, ItemModList right)
    {
        return !(left == right);
    }
}