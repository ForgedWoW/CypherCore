// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.GameObjects;

internal class SetTransportAutoCycleBetweenStopFrames : GameObjectTypeBase.CustomCommand
{
    private readonly bool _on;

    public SetTransportAutoCycleBetweenStopFrames(bool on)
    {
        _on = on;
    }

    public override void Execute(GameObjectTypeBase type)
    {
        var transport = (Transport)type;

        if (transport != null)
            transport.SetAutoCycleBetweenStopFrames(_on);
    }
}