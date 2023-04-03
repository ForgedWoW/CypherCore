// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Accounts;
using Framework.Constants;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("rbac")]
internal class RbacComands
{
    private static RBACCommandData GetRBACData(AccountIdentifier account)
    {
        if (account.IsConnected())
            return new RBACCommandData()
            {
                rbac = account.GetConnectedSession().RBACData,
                needDelete = false
            };

        RBACData rbac = new(account.GetID(), account.GetName(), (int)Global.WorldMgr.RealmId.Index, (byte)Global.AccountMgr.GetSecurity(account.GetID(), (int)Global.WorldMgr.RealmId.Index));
        rbac.LoadFromDB();

        return new RBACCommandData()
        {
            rbac = rbac,
            needDelete = true
        };
    }

    [Command("list", RBACPermissions.CommandRbacList, true)]
    private static bool HandleRBACListPermissionsCommand(CommandHandler handler, uint? permId)
    {
        if (!permId.HasValue)
        {
            var permissions = Global.AccountMgr.RBACPermissionList;
            handler.SendSysMessage(CypherStrings.RbacListPermissionsHeader);

            foreach (var (_, permission) in permissions)
                handler.SendSysMessage(CypherStrings.RbacListElement, permission.Id, permission.Name);
        }
        else
        {
            var permission = Global.AccountMgr.GetRBACPermission(permId.Value);

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
                var rbacPermission = Global.AccountMgr.GetRBACPermission(linkedPerm);

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
            if (account == null)
                account = AccountIdentifier.FromTarget(handler);

            if (account == null)
                return false;

            if (handler.HasLowerSecurityAccount(null, account.GetID(), true))
                return false;

            if (!realmId.HasValue)
                realmId = -1;

            var data = GetRBACData(account);

            var result = data.rbac.DenyPermission(permId, realmId.Value);
            var permission = Global.AccountMgr.GetRBACPermission(permId);

            switch (result)
            {
                case RBACCommandResult.CantAddAlreadyAdded:
                    handler.SendSysMessage(CypherStrings.RbacPermDeniedInList,
                                           permId,
                                           permission.Name,
                                           realmId.Value,
                                           data.rbac.Id,
                                           data.rbac.Name);

                    break;

                case RBACCommandResult.InGrantedList:
                    handler.SendSysMessage(CypherStrings.RbacPermDeniedInGrantedList,
                                           permId,
                                           permission.Name,
                                           realmId.Value,
                                           data.rbac.Id,
                                           data.rbac.Name);

                    break;

                case RBACCommandResult.Ok:
                    handler.SendSysMessage(CypherStrings.RbacPermDenied,
                                           permId,
                                           permission.Name,
                                           realmId.Value,
                                           data.rbac.Id,
                                           data.rbac.Name);

                    break;

                case RBACCommandResult.IdDoesNotExists:
                    handler.SendSysMessage(CypherStrings.RbacWrongParameterId, permId);

                    break;

                default:
                    break;
            }

            return true;
        }

        [Command("grant", RBACPermissions.CommandRbacAccPermGrant, true)]
        private static bool HandleRBACPermGrantCommand(CommandHandler handler, AccountIdentifier account, uint permId, int? realmId)
        {
            if (account == null)
                account = AccountIdentifier.FromTarget(handler);

            if (account == null)
                return false;

            if (handler.HasLowerSecurityAccount(null, account.GetID(), true))
                return false;

            if (!realmId.HasValue)
                realmId = -1;

            var data = GetRBACData(account);

            var result = data.rbac.GrantPermission(permId, realmId.Value);
            var permission = Global.AccountMgr.GetRBACPermission(permId);

            switch (result)
            {
                case RBACCommandResult.CantAddAlreadyAdded:
                    handler.SendSysMessage(CypherStrings.RbacPermGrantedInList,
                                           permId,
                                           permission.Name,
                                           realmId.Value,
                                           data.rbac.Id,
                                           data.rbac.Name);

                    break;

                case RBACCommandResult.InDeniedList:
                    handler.SendSysMessage(CypherStrings.RbacPermGrantedInDeniedList,
                                           permId,
                                           permission.Name,
                                           realmId.Value,
                                           data.rbac.Id,
                                           data.rbac.Name);

                    break;

                case RBACCommandResult.Ok:
                    handler.SendSysMessage(CypherStrings.RbacPermGranted,
                                           permId,
                                           permission.Name,
                                           realmId.Value,
                                           data.rbac.Id,
                                           data.rbac.Name);

                    break;

                case RBACCommandResult.IdDoesNotExists:
                    handler.SendSysMessage(CypherStrings.RbacWrongParameterId, permId);

                    break;

                default:
                    break;
            }

            return true;
        }

        [Command("list", RBACPermissions.CommandRbacAccPermList, true)]
        private static bool HandleRBACPermListCommand(CommandHandler handler, AccountIdentifier account)
        {
            if (account == null)
                account = AccountIdentifier.FromTarget(handler);

            if (account == null)
                return false;

            var data = GetRBACData(account);

            handler.SendSysMessage(CypherStrings.RbacListHeaderGranted, data.rbac.Id, data.rbac.Name);
            var granted = data.rbac.GrantedPermissions;

            if (granted.Empty())
                handler.SendSysMessage(CypherStrings.RbacListEmpty);
            else
                foreach (var id in granted)
                {
                    var permission = Global.AccountMgr.GetRBACPermission(id);
                    handler.SendSysMessage(CypherStrings.RbacListElement, permission.Id, permission.Name);
                }

            handler.SendSysMessage(CypherStrings.RbacListHeaderDenied, data.rbac.Id, data.rbac.Name);
            var denied = data.rbac.DeniedPermissions;

            if (denied.Empty())
                handler.SendSysMessage(CypherStrings.RbacListEmpty);
            else
                foreach (var id in denied)
                {
                    var permission = Global.AccountMgr.GetRBACPermission(id);
                    handler.SendSysMessage(CypherStrings.RbacListElement, permission.Id, permission.Name);
                }

            handler.SendSysMessage(CypherStrings.RbacListHeaderBySecLevel, data.rbac.Id, data.rbac.Name, data.rbac.GetSecurityLevel());
            var defaultPermissions = Global.AccountMgr.GetRBACDefaultPermissions(data.rbac.GetSecurityLevel());

            if (defaultPermissions.Empty())
                handler.SendSysMessage(CypherStrings.RbacListEmpty);
            else
                foreach (var id in defaultPermissions)
                {
                    var permission = Global.AccountMgr.GetRBACPermission(id);
                    handler.SendSysMessage(CypherStrings.RbacListElement, permission.Id, permission.Name);
                }

            return true;
        }

        [Command("revoke", RBACPermissions.CommandRbacAccPermRevoke, true)]
        private static bool HandleRBACPermRevokeCommand(CommandHandler handler, AccountIdentifier account, uint permId, int? realmId)
        {
            if (account == null)
                account = AccountIdentifier.FromTarget(handler);

            if (account == null)
                return false;

            if (handler.HasLowerSecurityAccount(null, account.GetID(), true))
                return false;

            if (!realmId.HasValue)
                realmId = -1;

            var data = GetRBACData(account);

            var result = data.rbac.RevokePermission(permId, realmId.Value);
            var permission = Global.AccountMgr.GetRBACPermission(permId);

            switch (result)
            {
                case RBACCommandResult.CantRevokeNotInList:
                    handler.SendSysMessage(CypherStrings.RbacPermRevokedNotInList,
                                           permId,
                                           permission.Name,
                                           realmId.Value,
                                           data.rbac.Id,
                                           data.rbac.Name);

                    break;

                case RBACCommandResult.Ok:
                    handler.SendSysMessage(CypherStrings.RbacPermRevoked,
                                           permId,
                                           permission.Name,
                                           realmId.Value,
                                           data.rbac.Id,
                                           data.rbac.Name);

                    break;

                case RBACCommandResult.IdDoesNotExists:
                    handler.SendSysMessage(CypherStrings.RbacWrongParameterId, permId);

                    break;

                default:
                    break;
            }

            return true;
        }
    }

    private class RBACCommandData
    {
        public bool needDelete;
        public RBACData rbac;
    }
}