// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;

using Microsoft.PowerShell.Commands;

namespace System.Management.Automation.SecurityAccountsManager;

/// <summary>
/// Contains utility functions for user and group operations.
/// </summary>
internal static class LocalHelpers
{
    /// <summary>
    /// Get all local users whose properties satisfy the specified predicate.
    /// </summary>
    /// <param name="principalFilter">
    /// Predicate that determines whether a user satisfies the conditions.
    /// </param>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{LocalUser}"/> object containing LocalUser
    /// objects that satisfy the predicate condition.
    /// </returns>
    internal static IEnumerable<LocalUser> GetMatchingLocalUsers(Predicate<UserPrincipal> principalFilter, PrincipalContext principalContext)
    {
        using var queryFolter = new UserPrincipal(principalContext);
        using var searcher = new PrincipalSearcher(queryFolter);
        foreach (UserPrincipal user in searcher.FindAll().Cast<UserPrincipal>())
        {
            using (user)
            {
                if (!principalFilter(user))
                {
                    continue;
                }

                DateTime? lastPasswordSet = null;
                DateTime? passwordChangeableDate = null;
                DateTime? passwordExpires = null;

                if (user.LastPasswordSet is DateTime lastPasswordSetValue)
                {
                    DirectoryEntry entry = (DirectoryEntry)user.GetUnderlyingObject();
                    int minPasswordAge = Convert.ToInt32(entry.Properties["MinPasswordAge"].Value);
                    int maxPasswordAge = Convert.ToInt32(entry.Properties["MaxPasswordAge"].Value);

                    lastPasswordSetValue = lastPasswordSetValue.ToLocalTime();

                    lastPasswordSet = lastPasswordSetValue;
                    passwordChangeableDate = lastPasswordSetValue.AddSeconds(minPasswordAge);
                    passwordExpires = user.PasswordNeverExpires ? null : lastPasswordSetValue.AddSeconds(maxPasswordAge);
                }

                var localUser = new LocalUser()
                {
                    AccountExpires = user.AccountExpirationDate,
                    Description = user.Description,
                    Enabled = user.Enabled ?? false,
                    FullName = user.DisplayName,
                    LastLogon = user.LastLogon,
                    Name = user.Name,
                    PasswordChangeableDate = passwordChangeableDate,
                    PasswordExpires = passwordExpires,
                    PasswordLastSet = lastPasswordSet,
                    PasswordRequired = !user.PasswordNotRequired,
                    PrincipalSource = Sam.GetPrincipalSource(user.Sid),
                    SID = user.Sid,
                    UserMayChangePassword = !user.UserCannotChangePassword,
                };

                yield return localUser;
            }
        }
    }
}
