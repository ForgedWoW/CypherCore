// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.Objects;

public struct CreateObjectBits
{
    public bool ActivePlayer;
    public bool AnimKit;
    public bool AreaTrigger;
    public bool CombatVictim;
    public bool Conversation;
    public bool EnablePortals;
    public bool GameObject;
    public bool MovementTransport;
    public bool MovementUpdate;
    public bool NoBirthAnim;
    public bool PlayHoverAnim;
    public bool Rotation;
    public bool SceneObject;
    public bool ServerTime;
    public bool SmoothPhasing;
    public bool Stationary;
    public bool ThisIsYou;
    public bool Vehicle;
    public void Clear()
    {
        NoBirthAnim = false;
        EnablePortals = false;
        PlayHoverAnim = false;
        MovementUpdate = false;
        MovementTransport = false;
        Stationary = false;
        CombatVictim = false;
        ServerTime = false;
        Vehicle = false;
        AnimKit = false;
        Rotation = false;
        AreaTrigger = false;
        GameObject = false;
        SmoothPhasing = false;
        ThisIsYou = false;
        SceneObject = false;
        ActivePlayer = false;
        Conversation = false;
    }
}