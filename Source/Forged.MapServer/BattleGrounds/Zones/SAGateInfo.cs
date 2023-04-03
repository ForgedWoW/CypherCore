// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.BattleGrounds.Zones;

internal class SAGateInfo
{
    public uint DamagedText;
    public uint DestroyedText;
    public uint GameObjectId;
    public uint GateId;
    public uint WorldState;

    public SAGateInfo(uint gateId, uint gameObjectId, uint worldState, uint damagedText, uint destroyedText)
    {
        GateId = gateId;
        GameObjectId = gameObjectId;
        WorldState = worldState;
        DamagedText = damagedText;
        DestroyedText = destroyedText;
    }
}