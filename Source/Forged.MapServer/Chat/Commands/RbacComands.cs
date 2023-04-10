// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Accounts;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("rbac")]
internal class RbacComands
{
    private static RBACCommandData GetRBACData(AccountIdentifier account, CommandHandler handler)
    {
        if (account.IsConnected())
            return new RBACCommandData()
            {
                RBAC = account.GetConnectedSession().RBACData,
                NeedDelete = false
            };

        RBACData rbac = new(account.GetID(), account.GetName(), (int)WorldManager.Realm.Id.Index, handler.AccountManager, handler.ClassFactory.Resolve<LoginDatabase>(), (byte)handler.AccountManager.GetSecurity(account.GetID(), (int)WorldManager.Realm.Id.Index));
        rbac.LoadFromDB();

        return new RBACCommandData()
        {
            RBAC = rbac,
            NeedDelete = true
        };
    }

    [Command("list", RBACPermissions.CommandRbacList, true)]
    private static bool HandleRBACListPermissionsCommand(CommandHandler handler, uint? permId)
    {
        if (!permId.HasValue)
        {
            var permissions = handler.AccountManager.RBACPermissionList;
            handler.SendSysMessage(CypherStrings.RbacListPermissionsHeader);

            foreach (var (_, permission) in permissions)
                handler.SendSysMessage(CypherStrings.RbacListElement, permission.Id, permission.Name);
        }
        else
        {
            var permission = handler.AccountManager.GetRBACPermission(permId.Value);

            if (permission == null)
            {
                handler.SendSysMessage(CypherStrings.RbacWrongParameterId, permId.Value);

                return false;
            }

            handler.SendSysMessage(CypherStrings.RbacListPermissionsHeader);
            handler.SendSysMessage(CypherStrings.RbacListElement, permission.Id, permission.Name);
            handler.SendSysMessage(CypherStrings.RbacListPermsLinkedHeader);

            foreach (var linkedPerm in permission.LinkedPermissions)
            {
                var rbacPermission = handler.AccountManager.GetRBACPermission(linkedPerm);

                if (rbacPermission != null)
                    handler.SendSysMessage(CypherStrings.RbacListElement, rbacPermission.Id, rbacPermission.Name);
            }
        }

        return true;
    }

    [CommandGroup("account")]
    private class RbacAccountCommands
    {
        [Command("deny", RBACPermissions.CommandRbacAccPermDeny, true)]
        private static bool HandleRBACPermDenyCommand(CommandHandler handler, AccountIdentifier account, uint permId, int? realmId)
        {
            account = account switch
            {
                null => AccountIdentifier.FromTarget(handler),
                _    => account
            };

            if (account == null)
                return false;

            if (handler.HasLowerSecurityAccount(null, account.GetID(), true))
                return false;

            realmId = realmId switch
            {
                null => -1,
                _    => realmId
            };

            var data = GetRBACData(account, handler);

            var result = data.RBAC.DenyPermission(permId, realmId.Value);
            var permission = handler.AccountManager.GetRBACPermission(permId);

            switch (result)
            {
                case RBACCommandResult.CantAddAlreadyAdded:
                    handler.SendSysMessage(CypherStrings.RbacPermDeniedInList,
                                           permId,
                                           permission.Name,
                                           realmId.Value,
                                           data.RBAC.Id,
                                           data.RBAC.Name);

                    break;

                case RBACCommandResult.InGrantedList:
                    handler.SendSysMessage(CypherStrings.RbacPermDeniedInGrantedList,
                                           permId,
                                           permission.Name,
                                           realmId.Value,
                                           data.RBAC.Id,
                                           data.RBAC.Name);

                    break;

                case RBACCommandResult.Ok:
                    handler.SendSysMessage(CypherStrings.RbacPermDenied,
                                           permId,
                                           permission.Name,
                                           realmId.Value,
                                           data.RBAC.Id,
                                           data.RBAC.Name);

                    break;

                case RBACCommandResult.IdDoesNotExists:
                    handler.SendSysMessage(CypherStrings.RbacWrongParameterId, permId);

                    break;
            }

            return true;
        }

        [Command("grant", RBACPermissions.CommandRbacAccPermGrant, true)]
        private static bool HandleRBACPermGrantCommand(CommandHandler handler, AccountIdentifier account, uint permId, int? realmId)
        {
            account = account switch
            {
                null => AccountIdentifier.FromTarget(handler),
                _    => account
            };

            if (account == null)
                return false;

            if (handler.HasLowerSecurityAccount(null, account.GetID(), true))
                return false;

            realmId = realmId switch
            {
                null => -1,
                _    => realmId
            };

            var data = GetRBACData(account, handler);

            var result = data.RBAC.GrantPermission(permId, realmId.Value);
            var permission = handler.AccountManager.GetRBACPermission(permId);

            switch (result)
            {
                case RBACCommandResult.CantAddAlreadyAdded:
                    handler.SendSysMessage(CypherStrings.RbacPermGrantedInList,
                                           permId,
                                           permission.Name,
                                           realmId.Value,
                                           data.RBAC.Id,
                                           data.RBAC.Name);

                    break;

                case RBACCommandResult.InDeniedList:
                    handler.SendSysMessage(CypherStrings.RbacPermGrantedInDeniedList,
                                           permId,
                                           permission.Name,
                                           realmId.Value,
                                           data.RBAC.Id,
                                           data.RBAC.Name);

                    break;

                case RBACCommandResult.Ok:
                    handler.SendSysMessage(CypherStrings.RbacPermGranted,
                                           permId,
                                           permission.Name,
                                           realmId.Value,
                                           data.RBAC.Id,
                                           data.RBAC.Name);

                    break;

                case RBACCommandResult.IdDoesNotExists:
                    handler.SendSysMessage(CypherStrings.RbacWrongParameterId, permId);

                    break;
            }

            return true;
        }

        [Command("list", RBACPermissions.CommandRbacAccPermList, true)]
        private static bool HandleRBACPermListCommand(CommandHandler handler, AccountIdentifier account)
        {
            account = account switch
            {
                null => AccountIdentifier.FromTarget(handler),
                _    => account
            };

            if (account == null)
                return false;

            var data = GetRBACData(account, handler);

            handler.SendSysMessage(CypherStrings.RbacListHeaderGranted, data.RBAC.Id, data.RBAC.Name);
            var granted = data.RBAC.GrantedPermissions;

            if (granted.Empty())
                handler.SendSysMessage(CypherStrings.RbacListEmpty);
            else
                foreach (var id in granted)
                {
                    var permission = handler.AccountManager.GetRBACPermission(id);
                    handler.SendSysMessage(CypherStrings.RbacListElement, permission.Id, permission.Name);
                }

            handler.SendSysMessage(CypherStrings.RbacListHeaderDenied, data.RBAC.Id, data.RBAC.Name);
            var denied = data.RBAC.DeniedPermissions;

            if (denied.Empty())
                handler.SendSysMessage(CypherStrings.RbacListEmpty);
            else
                foreach (var id in denied)
                {
                    var permission = handler.AccountManager.GetRBACPermission(id);
                    handler.SendSysMessage(CypherStrings.RbacListElement, permission.Id, permission.Name);
                }

            handler.SendSysMessage(CypherStrings.RbacListHeaderBySecLevel, data.RBAC.Id, data.RBAC.Name, data.RBAC.GetSecurityLevel());
            var defaultPermissions = handler.AccountManager.GetRBACDefaultPermissions(data.RBAC.GetSecurityLevel());

            if (defaultPermissions.Empty())
                handler.SendSysMessage(CypherStrings.RbacListEmpty);
            else
                foreach (var id in defaultPermissions)
                {
                    var permission = handler.AccountManager.GetRBACPermission(id);
                    handler.SendSysMessage(CypherStrings.RbacListElement, permission.Id, permission.Name);
                }

            return true;
        }

        [Command("revoke", RBACPermissions.CommandRbacAccPermRevoke, true)]
        private static bool HandleRBACPermRevokeCommand(CommandHandler handler, AccountIdentifier account, uint permId, int? realmId)
        {
            account = account switch
            {
                null => AccountIdentifier.FromTarget(handler),
                _    => account
            };

            if (account == null)
                return false;

            if (handler.HasLowerSecurityAccount(null, account.GetID(), true))
                return false;

            realmId = realmId switch
            {
                null => -1,
                _    => realmId
            };

            var data = GetRBACData(account, handler);

            var result = data.RBAC.RevokePermission(permId, realmId.Value);
            var permission = handler.AccountManager.GetRBACPermission(permId);

            switch (result)
            {
                case RBACCommandResult.CantRevokeNotInList:
                    handler.SendSysMessage(CypherStrings.RbacPermRevokedNotInList,
                                           permId,
                                           permission.Name,
                                           realmId.Value,
                                           data.RBAC.Id,
                                           data.RBAC.Name);

                    break;

                case RBACCommandResult.Ok:
                    handler.SendSysMessage(CypherStrings.RbacPermRevoked,
                                           permId,
                                           permission.Name,
                                           realmId.Value,
                                           data.RBAC.Id,
                                           data.RBAC.Name);

                    break;

                case RBACCommandResult.IdDoesNotExists:
                    handler.SendSysMessage(CypherStrings.RbacWrongParameterId, permId);

                    break;
            }

            return true;
        }
    }

    private class RBACCommandData
    {
        public bool NeedDelete;
        public RBACData RBAC;
    }
}