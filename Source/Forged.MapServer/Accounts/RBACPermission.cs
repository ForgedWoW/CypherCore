// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Accounts;

public class RBACPermission
{
	readonly uint _id;                  // id of the object
	readonly string _name;              // name of the object
	readonly List<uint> _perms = new(); // Set of permissions

	// Gets the Name of the Object

	public string Name => _name;

	// Gets the Id of the Object

	public uint Id => _id;

	// Gets the Permissions linked to this permission

	public List<uint> LinkedPermissions => _perms;

	public RBACPermission(uint id = 0, string name = "")
	{
		_id = id;
		_name = name;
	}

	// Adds a new linked Permission
	public void AddLinkedPermission(uint id)
	{
		_perms.Add(id);
	}

	// Removes a linked Permission
	public void RemoveLinkedPermission(uint id)
	{
		_perms.Remove(id);
	}
}