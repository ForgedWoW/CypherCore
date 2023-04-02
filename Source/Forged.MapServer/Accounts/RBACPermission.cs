// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Accounts;

public class RBACPermission
{
    // Gets the Name of the Object

    public RBACPermission(uint id = 0, string name = "")
    {
        Id = id;
        Name = name;
    }

    public uint Id { get; }
    public List<uint> LinkedPermissions { get; } = new();
    public string Name { get; }

    // Gets the Id of the Object
    // Gets the Permissions linked to this permission
    // Adds a new linked Permission
    public void AddLinkedPermission(uint id)
    {
        LinkedPermissions.Add(id);
    }

    // Removes a linked Permission
    public void RemoveLinkedPermission(uint id)
    {
        LinkedPermissions.Remove(id);
    }
}