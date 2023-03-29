// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum PacketProcessing
{
    Inplace = 0,  //process packet whenever we receive it - mostly for non-handled or non-implemented packets
    ThreadUnsafe, //packet is not thread-safe - process it in World.UpdateSessions()
    ThreadSafe    //packet is thread-safe - process it in Map.Update()
}