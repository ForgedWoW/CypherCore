// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Entities.GameObjects;

internal class GameObjectTypeBase
{
    protected readonly GameObject Owner;

    public GameObjectTypeBase(GameObject owner)
    {
        Owner = owner;
    }

    public virtual void OnRelocated() { }

    public virtual void OnStateChanged(GameObjectState oldState, GameObjectState newState) { }

    public virtual void Update(uint diff) { }
    public class CustomCommand
    {
        public virtual void Execute(GameObjectTypeBase type) { }
    }
}