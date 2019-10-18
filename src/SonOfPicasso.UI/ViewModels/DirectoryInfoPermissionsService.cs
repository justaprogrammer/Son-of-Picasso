using System;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Security.AccessControl;
using System.Security.Principal;

namespace SonOfPicasso.UI.ViewModels
{
    public class DirectoryInfoPermissionsService : IDirectoryInfoPermissionsService
    {
        // https://stackoverflow.com/a/31040692/104877

        public bool IsReadable(IDirectoryInfo di)
        {
            AuthorizationRuleCollection rules;
            WindowsIdentity identity;
            try
            {
                rules = di.GetAccessControl().GetAccessRules(true, true, typeof(SecurityIdentifier));
                identity = WindowsIdentity.GetCurrent();
            }
            catch (UnauthorizedAccessException uae)
            {
                return false;
            }

            var isAllow = false;
            var userSID = identity.User.Value;

            foreach (FileSystemAccessRule rule in rules)
                if (rule.IdentityReference.ToString() == userSID || identity.Groups.Contains(rule.IdentityReference))
                {
                    if ((rule.FileSystemRights.HasFlag(FileSystemRights.Read) ||
                         rule.FileSystemRights.HasFlag(FileSystemRights.ReadAttributes) ||
                         rule.FileSystemRights.HasFlag(FileSystemRights.ReadData)) &&
                        rule.AccessControlType == AccessControlType.Deny)
                        return false;
                    if (rule.FileSystemRights.HasFlag(FileSystemRights.Read) &&
                        rule.FileSystemRights.HasFlag(FileSystemRights.ReadAttributes) &&
                        rule.FileSystemRights.HasFlag(FileSystemRights.ReadData) &&
                        rule.AccessControlType == AccessControlType.Allow)
                        isAllow = true;
                }

            return isAllow;
        }

        public bool IsWriteable(IDirectoryInfo me)
        {
            AuthorizationRuleCollection rules;
            WindowsIdentity identity;
            try
            {
                rules = me.GetAccessControl().GetAccessRules(true, true, typeof(SecurityIdentifier));
                identity = WindowsIdentity.GetCurrent();
            }
            catch (UnauthorizedAccessException uae)
            {
                Debug.WriteLine(uae.ToString());
                return false;
            }

            var isAllow = false;
            var userSID = identity.User.Value;

            foreach (FileSystemAccessRule rule in rules)
                if (rule.IdentityReference.ToString() == userSID || identity.Groups.Contains(rule.IdentityReference))
                {
                    if ((rule.FileSystemRights.HasFlag(FileSystemRights.Write) ||
                         rule.FileSystemRights.HasFlag(FileSystemRights.WriteAttributes) ||
                         rule.FileSystemRights.HasFlag(FileSystemRights.WriteData) ||
                         rule.FileSystemRights.HasFlag(FileSystemRights.CreateDirectories) ||
                         rule.FileSystemRights.HasFlag(FileSystemRights.CreateFiles)) &&
                        rule.AccessControlType == AccessControlType.Deny)
                        return false;
                    if (rule.FileSystemRights.HasFlag(FileSystemRights.Write) &&
                        rule.FileSystemRights.HasFlag(FileSystemRights.WriteAttributes) &&
                        rule.FileSystemRights.HasFlag(FileSystemRights.WriteData) &&
                        rule.FileSystemRights.HasFlag(FileSystemRights.CreateDirectories) &&
                        rule.FileSystemRights.HasFlag(FileSystemRights.CreateFiles) &&
                        rule.AccessControlType == AccessControlType.Allow)
                        isAllow = true;
                }

            return isAllow;
        }
    }
}