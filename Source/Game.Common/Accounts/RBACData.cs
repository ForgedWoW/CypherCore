// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;
using Game.Common.Accounts;

namespace Game.Common.Accounts;

public class RBACData
{
	readonly uint _id;                         // Account id
	readonly string _name;                     // Account name
	readonly int _realmId;                     // RealmId Affected
	readonly List<uint> _grantedPerms = new(); // Granted permissions
	readonly List<uint> _deniedPerms = new();  // Denied permissions
	byte _secLevel;                            // Account SecurityLevel
	List<uint> _globalPerms = new();           // Calculated permissions

	// Gets the Name of the Object

	public string Name => _name;
	// Gets the Id of the Object

	public uint Id => _id;

	// Returns all the granted permissions (after computation)

	public List<uint> Permissions => _globalPerms;
	// Returns all the granted permissions

	public List<uint> GrantedPermissions => _grantedPerms;
	// Returns all the denied permissions

	public List<uint> DeniedPermissions => _deniedPerms;

	public RBACData(uint id, string name, int realmId, byte secLevel = 255)
	{
		_id = id;
		_name = name;
		_realmId = realmId;
		_secLevel = secLevel;
	}

	public RBACCommandResult GrantPermission(uint permissionId, int realmId = 0)
	{
		// Check if permission Id exists
		var perm = Global.AccountMgr.GetRBACPermission(permissionId);

		if (perm == null)
		{
			Log.outDebug(LogFilter.Rbac,
						"RBACData.GrantPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Permission does not exists",
						Id,
						Name,
						permissionId,
						realmId);

			return RBACCommandResult.IdDoesNotExists;
		}

		// Check if already added in denied list
		if (HasDeniedPermission(permissionId))
		{
			Log.outDebug(LogFilter.Rbac,
						"RBACData.GrantPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Permission in deny list",
						Id,
						Name,
						permissionId,
						realmId);

			return RBACCommandResult.InDeniedList;
		}

		// Already added?
		if (HasGrantedPermission(permissionId))
		{
			Log.outDebug(LogFilter.Rbac,
						"RBACData.GrantPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Permission already granted",
						Id,
						Name,
						permissionId,
						realmId);

			return RBACCommandResult.CantAddAlreadyAdded;
		}

		AddGrantedPermission(permissionId);

		// Do not save to db when loading data from DB (realmId = 0)
		if (realmId != 0)
		{
			Log.outDebug(LogFilter.Rbac,
						"RBACData.GrantPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Ok and DB updated",
						Id,
						Name,
						permissionId,
						realmId);

			SavePermission(permissionId, true, realmId);
			CalculateNewPermissions();
		}
		else
		{
			Log.outDebug(LogFilter.Rbac,
						"RBACData.GrantPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Ok",
						Id,
						Name,
						permissionId,
						realmId);
		}

		return RBACCommandResult.OK;
	}

	public RBACCommandResult DenyPermission(uint permissionId, int realmId = 0)
	{
		// Check if permission Id exists
		var perm = Global.AccountMgr.GetRBACPermission(permissionId);

		if (perm == null)
		{
			Log.outDebug(LogFilter.Rbac,
						"RBACData.DenyPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Permission does not exists",
						Id,
						Name,
						permissionId,
						realmId);

			return RBACCommandResult.IdDoesNotExists;
		}

		// Check if already added in granted list
		if (HasGrantedPermission(permissionId))
		{
			Log.outDebug(LogFilter.Rbac,
						"RBACData.DenyPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Permission in grant list",
						Id,
						Name,
						permissionId,
						realmId);

			return RBACCommandResult.InGrantedList;
		}

		// Already added?
		if (HasDeniedPermission(permissionId))
		{
			Log.outDebug(LogFilter.Rbac,
						"RBACData.DenyPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Permission already denied",
						Id,
						Name,
						permissionId,
						realmId);

			return RBACCommandResult.CantAddAlreadyAdded;
		}

		AddDeniedPermission(permissionId);

		// Do not save to db when loading data from DB (realmId = 0)
		if (realmId != 0)
		{
			Log.outDebug(LogFilter.Rbac,
						"RBACData.DenyPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Ok and DB updated",
						Id,
						Name,
						permissionId,
						realmId);

			SavePermission(permissionId, false, realmId);
			CalculateNewPermissions();
		}
		else
		{
			Log.outDebug(LogFilter.Rbac,
						"RBACData.DenyPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Ok",
						Id,
						Name,
						permissionId,
						realmId);
		}

		return RBACCommandResult.OK;
	}

	public RBACCommandResult RevokePermission(uint permissionId, int realmId = 0)
	{
		// Check if it's present in any list
		if (!HasGrantedPermission(permissionId) && !HasDeniedPermission(permissionId))
		{
			Log.outDebug(LogFilter.Rbac,
						"RBACData.RevokePermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Not granted or revoked",
						Id,
						Name,
						permissionId,
						realmId);

			return RBACCommandResult.CantRevokeNotInList;
		}

		RemoveGrantedPermission(permissionId);
		RemoveDeniedPermission(permissionId);

		// Do not save to db when loading data from DB (realmId = 0)
		if (realmId != 0)
		{
			Log.outDebug(LogFilter.Rbac,
						"RBACData.RevokePermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Ok and DB updated",
						Id,
						Name,
						permissionId,
						realmId);

			var stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_RBAC_ACCOUNT_PERMISSION);
			stmt.AddValue(0, Id);
			stmt.AddValue(1, permissionId);
			stmt.AddValue(2, realmId);
			DB.Login.Execute(stmt);

			CalculateNewPermissions();
		}
		else
		{
			Log.outDebug(LogFilter.Rbac,
						"RBACData.RevokePermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Ok",
						Id,
						Name,
						permissionId,
						realmId);
		}

		return RBACCommandResult.OK;
	}

	public void LoadFromDB()
	{
		ClearData();

		Log.outDebug(LogFilter.Rbac, "RBACData.LoadFromDB [Id: {0} Name: {1}]: Loading permissions", Id, Name);
		// Load account permissions (granted and denied) that affect current realm
		var stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_RBAC_ACCOUNT_PERMISSIONS);
		stmt.AddValue(0, Id);
		stmt.AddValue(1, GetRealmId());

		LoadFromDBCallback(DB.Login.Query(stmt));
	}

	public QueryCallback LoadFromDBAsync()
	{
		ClearData();

		Log.outDebug(LogFilter.Rbac, "RBACData.LoadFromDB [Id: {0} Name: {1}]: Loading permissions", Id, Name);
		// Load account permissions (granted and denied) that affect current realm
		var stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_RBAC_ACCOUNT_PERMISSIONS);
		stmt.AddValue(0, Id);
		stmt.AddValue(1, GetRealmId());

		return DB.Login.AsyncQuery(stmt);
	}

	public void LoadFromDBCallback(SQLResult result)
	{
		if (!result.IsEmpty())
			do
			{
				if (result.Read<bool>(1))
					GrantPermission(result.Read<uint>(0));
				else
					DenyPermission(result.Read<uint>(0));
			} while (result.NextRow());

		// Add default permissions
		var permissions = Global.AccountMgr.GetRBACDefaultPermissions(_secLevel);

		foreach (var id in permissions)
			GrantPermission(id);

		// Force calculation of permissions
		CalculateNewPermissions();
	}

	public void AddPermissions(List<uint> permsFrom, List<uint> permsTo)
	{
		foreach (var id in permsFrom)
			permsTo.Add(id);
	}

	public bool HasPermission(RBACPermissions permission)
	{
		return _globalPerms.Contains((uint)permission);
	}

	public void SetSecurityLevel(byte id)
	{
		_secLevel = id;
		LoadFromDB();
	}

	public byte GetSecurityLevel()
	{
		return _secLevel;
	}

	void SavePermission(uint permission, bool granted, int realmId)
	{
		var stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_RBAC_ACCOUNT_PERMISSION);
		stmt.AddValue(0, Id);
		stmt.AddValue(1, permission);
		stmt.AddValue(2, granted);
		stmt.AddValue(3, realmId);
		DB.Login.Execute(stmt);
	}

	void CalculateNewPermissions()
	{
		Log.outDebug(LogFilter.Rbac, "RBACData.CalculateNewPermissions [Id: {0} Name: {1}]", Id, Name);

		// Get the list of granted permissions
		_globalPerms = GrantedPermissions;
		ExpandPermissions(_globalPerms);
		var revoked = DeniedPermissions;
		ExpandPermissions(revoked);
		RemovePermissions(_globalPerms, revoked);
	}

	/// <summary>
	///  Removes a list of permissions from another list
	/// </summary>
	/// <param name="permsFrom"> </param>
	/// <param name="permsToRemove"> </param>
	void RemovePermissions(List<uint> permsFrom, List<uint> permsToRemove)
	{
		foreach (var id in permsToRemove)
			permsFrom.Remove(id);
	}

	void ExpandPermissions(List<uint> permissions)
	{
		List<uint> toCheck = new(permissions);
		permissions.Clear();

		while (!toCheck.Empty())
		{
			// remove the permission from original list
			var permissionId = toCheck.FirstOrDefault();
			toCheck.RemoveAt(0);

			var permission = Global.AccountMgr.GetRBACPermission(permissionId);

			if (permission == null)
				continue;

			// insert into the final list (expanded list)
			permissions.Add(permissionId);

			// add all linked permissions (that are not already expanded) to the list of permissions to be checked
			var linkedPerms = permission.LinkedPermissions;

			foreach (var id in linkedPerms)
				if (!permissions.Contains(id))
					toCheck.Add(id);
		}

		//Log.outDebug(LogFilter.General, "RBACData:ExpandPermissions: Expanded: {0}", GetDebugPermissionString(permissions));
	}

	void ClearData()
	{
		_grantedPerms.Clear();
		_deniedPerms.Clear();
		_globalPerms.Clear();
	}

	int GetRealmId()
	{
		return _realmId;
	}

	// Checks if a permission is granted
	bool HasGrantedPermission(uint permissionId)
	{
		return _grantedPerms.Contains(permissionId);
	}

	// Checks if a permission is denied
	bool HasDeniedPermission(uint permissionId)
	{
		return _deniedPerms.Contains(permissionId);
	}

	// Adds a new granted permission
	void AddGrantedPermission(uint permissionId)
	{
		_grantedPerms.Add(permissionId);
	}

	// Removes a granted permission
	void RemoveGrantedPermission(uint permissionId)
	{
		_grantedPerms.Remove(permissionId);
	}

	// Adds a new denied permission
	void AddDeniedPermission(uint permissionId)
	{
		_deniedPerms.Add(permissionId);
	}

	// Removes a denied permission
	void RemoveDeniedPermission(uint permissionId)
	{
		_deniedPerms.Remove(permissionId);
	}
}
