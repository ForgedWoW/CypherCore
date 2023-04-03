// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Accounts;

public class RBACData
{
    private readonly AccountManager _accountManager;
    private readonly LoginDatabase _loginDatabase;
    private readonly int _realmId; // RealmId Affected
    private byte _secLevel;                            // Account SecurityLevel

    // Gets the Name of the Object

    public RBACData(uint id, string name, int realmId, AccountManager accountManager, LoginDatabase loginDatabase, byte secLevel = 255)
    {
        Id = id;
        Name = name;
        _realmId = realmId;
        _accountManager = accountManager;
        _loginDatabase = loginDatabase;
        _secLevel = secLevel;
    }

    public List<uint> DeniedPermissions { get; } = new();
    public List<uint> GrantedPermissions { get; } = new();
    public uint Id { get; }
    public string Name { get; }
    // Gets the Id of the Object
    // Returns all the granted permissions (after computation)

    public List<uint> Permissions { get; private set; } = new();

    public void AddPermissions(List<uint> permsFrom, List<uint> permsTo)
    {
        permsTo.AddRange(permsFrom);
    }

    public RBACCommandResult DenyPermission(uint permissionId, int realmId = 0)
    {
        // Check if permission Id exists
        var perm = _accountManager.GetRBACPermission(permissionId);

        if (perm == null)
        {
            Log.Logger.Debug("RBACData.DenyPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Permission does not exists",
                             Id,
                             Name,
                             permissionId,
                             realmId);

            return RBACCommandResult.IdDoesNotExists;
        }

        // Check if already added in granted list
        if (HasGrantedPermission(permissionId))
        {
            Log.Logger.Debug("RBACData.DenyPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Permission in grant list",
                             Id,
                             Name,
                             permissionId,
                             realmId);

            return RBACCommandResult.InGrantedList;
        }

        // Already added?
        if (HasDeniedPermission(permissionId))
        {
            Log.Logger.Debug("RBACData.DenyPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Permission already denied",
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
            Log.Logger.Debug("RBACData.DenyPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Ok and DB updated",
                             Id,
                             Name,
                             permissionId,
                             realmId);

            SavePermission(permissionId, false, realmId);
            CalculateNewPermissions();
        }
        else
        {
            Log.Logger.Debug("RBACData.DenyPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Ok",
                             Id,
                             Name,
                             permissionId,
                             realmId);
        }

        return RBACCommandResult.Ok;
    }

    public byte GetSecurityLevel()
    {
        return _secLevel;
    }

    // Returns all the granted permissions
    // Returns all the denied permissions
    public RBACCommandResult GrantPermission(uint permissionId, int realmId = 0)
    {
        // Check if permission Id exists
        var perm = _accountManager.GetRBACPermission(permissionId);

        if (perm == null)
        {
            Log.Logger.Debug("RBACData.GrantPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Permission does not exists",
                             Id,
                             Name,
                             permissionId,
                             realmId);

            return RBACCommandResult.IdDoesNotExists;
        }

        // Check if already added in denied list
        if (HasDeniedPermission(permissionId))
        {
            Log.Logger.Debug("RBACData.GrantPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Permission in deny list",
                             Id,
                             Name,
                             permissionId,
                             realmId);

            return RBACCommandResult.InDeniedList;
        }

        // Already added?
        if (HasGrantedPermission(permissionId))
        {
            Log.Logger.Debug("RBACData.GrantPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Permission already granted",
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
            Log.Logger.Debug("RBACData.GrantPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Ok and DB updated",
                             Id,
                             Name,
                             permissionId,
                             realmId);

            SavePermission(permissionId, true, realmId);
            CalculateNewPermissions();
        }
        else
        {
            Log.Logger.Debug("RBACData.GrantPermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Ok",
                             Id,
                             Name,
                             permissionId,
                             realmId);
        }

        return RBACCommandResult.Ok;
    }

    public bool HasPermission(RBACPermissions permission)
    {
        return Permissions.Contains((uint)permission);
    }

    public void LoadFromDB()
    {
        ClearData();

        Log.Logger.Debug("RBACData.LoadFromDB [Id: {0} Name: {1}]: Loading permissions", Id, Name);
        // Load account permissions (granted and denied) that affect current realm
        var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_RBAC_ACCOUNT_PERMISSIONS);
        stmt.AddValue(0, Id);
        stmt.AddValue(1, GetRealmId());

        LoadFromDBCallback(_loginDatabase.Query(stmt));
    }

    public QueryCallback LoadFromDBAsync()
    {
        ClearData();

        Log.Logger.Debug("RBACData.LoadFromDB [Id: {0} Name: {1}]: Loading permissions", Id, Name);
        // Load account permissions (granted and denied) that affect current realm
        var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_RBAC_ACCOUNT_PERMISSIONS);
        stmt.AddValue(0, Id);
        stmt.AddValue(1, GetRealmId());

        return _loginDatabase.AsyncQuery(stmt);
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
        var permissions = _accountManager.GetRBACDefaultPermissions(_secLevel);

        foreach (var id in permissions)
            GrantPermission(id);

        // Force calculation of permissions
        CalculateNewPermissions();
    }

    public RBACCommandResult RevokePermission(uint permissionId, int realmId = 0)
    {
        // Check if it's present in any list
        if (!HasGrantedPermission(permissionId) && !HasDeniedPermission(permissionId))
        {
            Log.Logger.Debug("RBACData.RevokePermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Not granted or revoked",
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
            Log.Logger.Debug("RBACData.RevokePermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Ok and DB updated",
                             Id,
                             Name,
                             permissionId,
                             realmId);

            var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.DEL_RBAC_ACCOUNT_PERMISSION);
            stmt.AddValue(0, Id);
            stmt.AddValue(1, permissionId);
            stmt.AddValue(2, realmId);
            _loginDatabase.Execute(stmt);

            CalculateNewPermissions();
        }
        else
        {
            Log.Logger.Debug("RBACData.RevokePermission [Id: {0} Name: {1}] (Permission {2}, RealmId {3}). Ok",
                             Id,
                             Name,
                             permissionId,
                             realmId);
        }

        return RBACCommandResult.Ok;
    }

    public void SetSecurityLevel(byte id)
    {
        _secLevel = id;
        LoadFromDB();
    }

    // Adds a new denied permission
    private void AddDeniedPermission(uint permissionId)
    {
        DeniedPermissions.Add(permissionId);
    }

    // Adds a new granted permission
    private void AddGrantedPermission(uint permissionId)
    {
        GrantedPermissions.Add(permissionId);
    }

    private void CalculateNewPermissions()
    {
        Log.Logger.Debug("RBACData.CalculateNewPermissions [Id: {0} Name: {1}]", Id, Name);

        // Get the list of granted permissions
        Permissions = GrantedPermissions;
        ExpandPermissions(Permissions);
        var revoked = DeniedPermissions;
        ExpandPermissions(revoked);
        RemovePermissions(Permissions, revoked);
    }

    private void ClearData()
    {
        GrantedPermissions.Clear();
        DeniedPermissions.Clear();
        Permissions.Clear();
    }

    private void ExpandPermissions(List<uint> permissions)
    {
        List<uint> toCheck = new(permissions);
        permissions.Clear();

        while (!toCheck.Empty())
        {
            // remove the permission from original list
            var permissionId = toCheck.FirstOrDefault();
            toCheck.RemoveAt(0);

            var permission = _accountManager.GetRBACPermission(permissionId);

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

        //Log.Logger.Debug("RBACData:ExpandPermissions: Expanded: {0}", GetDebugPermissionString(permissions));
    }

    private int GetRealmId()
    {
        return _realmId;
    }

    // Checks if a permission is denied
    private bool HasDeniedPermission(uint permissionId)
    {
        return DeniedPermissions.Contains(permissionId);
    }

    // Checks if a permission is granted
    private bool HasGrantedPermission(uint permissionId)
    {
        return GrantedPermissions.Contains(permissionId);
    }

    // Removes a denied permission
    private void RemoveDeniedPermission(uint permissionId)
    {
        DeniedPermissions.Remove(permissionId);
    }

    // Removes a granted permission
    private void RemoveGrantedPermission(uint permissionId)
    {
        GrantedPermissions.Remove(permissionId);
    }

    /// <summary>
    ///     Removes a list of permissions from another list
    /// </summary>
    /// <param name="permsFrom"> </param>
    /// <param name="permsToRemove"> </param>
    private void RemovePermissions(List<uint> permsFrom, List<uint> permsToRemove)
    {
        foreach (var id in permsToRemove)
            permsFrom.Remove(id);
    }

    private void SavePermission(uint permission, bool granted, int realmId)
    {
        var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.INS_RBAC_ACCOUNT_PERMISSION);
        stmt.AddValue(0, Id);
        stmt.AddValue(1, permission);
        stmt.AddValue(2, granted);
        stmt.AddValue(3, realmId);
        _loginDatabase.Execute(stmt);
    }
}