// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(48439)]
public class NPCGoblinEngineer : ScriptedAI
{
    public InstanceScript Instance;

    public NPCGoblinEngineer(Creature creature) : base(creature)
    {
        Instance = creature.InstanceScript;
    }

    public override void Reset()
    {
        if (!Me)
            return;

        if (Me.FindNearestGameObject(DmGameObjects.GO_HEAVY_DOOR, 20.0f))
            Me.AddAura(78087, Me);
        else
            Me.AddAura(57626, Me);
    }
}