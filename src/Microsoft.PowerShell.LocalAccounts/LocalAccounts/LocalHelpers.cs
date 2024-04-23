// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;

using Microsoft.PowerShell.Commands;
using Microsoft.PowerShell.LocalAccounts;

namespace System.Management.Automation.SecurityAccountsManager;

/// <summary>
/// Contains utility functions for user and group operations.
/// </summary>
internal static class LocalHelpers
{
    /// <summary>
    /// Get FQDN computer name.
    /// </summary>
    internal static string GetFullComputerName()
        => Net.Dns.GetHostEntry(Environment.MachineName).HostName;

    /// <summary>
    /// Get a domain name if the local computer.
    /// </summary>
    internal static string GetComputerDomainName()
        => System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().DomainName;

    /// <summary>
    /// Get all local users.
    /// </summary>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{LocalUser}"/> object containing LocalUser objects.
    /// </returns>
    internal static IEnumerable<LocalUser> GetAllLocalUsers(PrincipalContext principalContext)
        => GetMatchingLocalUsers(static _ => true, principalContext);

    /// <summary>
    /// Get local user whose a name satisfy the specified name.
    /// </summary>
    /// <param name="name">
    /// A user name.
    /// </param>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="LocalUser"/> object for a user with the specified name.
    /// </returns>
    internal static LocalUser GetMatchingLocalUsersByName(string name, PrincipalContext principalContext)
        => GetMatchingLocalUsers(userPrincipal => name.Equals(userPrincipal.Name, StringComparison.CurrentCultureIgnoreCase), principalContext).FirstOrDefault();

    /// <summary>
    /// Get local user whose a security identifier (SID) satisfy the specified SID.
    /// </summary>
    /// <param name="sid">
    /// A user a security identifier (SID).
    /// </param>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="LocalUser"/> object for a user with the specified security identifier (SID).
    /// </returns>
    internal static LocalUser GetMatchingLocalUsersBySID(SecurityIdentifier sid, PrincipalContext principalContext)
        => GetMatchingLocalUsers(userPrincipal => sid.Equals(userPrincipal.Sid), principalContext).FirstOrDefault();

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
        using var queryFilter = new UserPrincipal(principalContext);
        using var searcher = new PrincipalSearcher(queryFilter);
        foreach (UserPrincipal user in searcher.FindAll().Cast<UserPrincipal>())
        {
            using (user)
            {
                if (!principalFilter(user))
                {
                    continue;
                }

                yield return GetLocalUser(user);
            }
        }
    }

    internal static LocalUser GetLocalUser(UserPrincipal user)
    {
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

        return localUser;
    }

    /// <summary>
    /// Get all local groups.
    /// </summary>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{LocalGroup}"/> object containing LocalGroup objects.
    /// </returns>
    internal static IEnumerable<LocalGroup> GetAllLocalGroups(PrincipalContext principalContext)
        => GetMatchingLocalGroups(static _ => true, principalContext);

    /// <summary>
    /// Get local group whose a name satisfy the specified name.
    /// </summary>
    /// <param name="name">
    /// A group name.
    /// </param>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="LocalGroup"/> object for a group with the specified name.
    /// </returns>
    internal static LocalGroup GetMatchingLocalGroupsByName(string name, PrincipalContext principalContext)
        => GetMatchingLocalGroups(userPrincipal => name.Equals(userPrincipal.Name, StringComparison.CurrentCultureIgnoreCase), principalContext).FirstOrDefault();

    /// <summary>
    /// Get local group whose a security identifier (SID) satisfy the specified SID.
    /// </summary>
    /// <param name="sid">
    /// A group a security identifier (SID).
    /// </param>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="LocalGroup"/> object for a group with the specified security identifier (SID).
    /// </returns>
    internal static LocalGroup GetMatchingLocalGroupsBySID(SecurityIdentifier sid, PrincipalContext principalContext)
        => GetMatchingLocalGroups(userPrincipal => sid.Equals(userPrincipal.Sid), principalContext).FirstOrDefault();

    /// <summary>
    /// Get all local groups whose properties satisfy the specified predicate.
    /// </summary>
    /// <param name="principalFilter">
    /// Predicate that determines whether a group satisfies the conditions.
    /// </param>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{LocalGroup}"/> object containing LocalGroup
    /// objects that satisfy the predicate condition.
    /// </returns>
    internal static IEnumerable<LocalGroup> GetMatchingLocalGroups(Predicate<GroupPrincipal> principalFilter, PrincipalContext principalContext)
    {
        foreach (GroupPrincipal group in GetMatchingGroupPrincipals(principalFilter, principalContext))
        {
            using (group)
            {
                var localGroup = new LocalGroup()
                {
                    Description = group.Description,
                    Name = group.Name,
                    PrincipalSource = Sam.GetPrincipalSource(group.Sid),
                    SID = group.Sid,
                };

                yield return localGroup;
            }
        }
    }

    /// <summary>
    /// Get all local groups.
    /// </summary>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{GroupPrincipal}"/> object containing GroupPrincipal objects.
    /// </returns>
    internal static IEnumerable<GroupPrincipal> GetAllGroupPrincipals(PrincipalContext principalContext)
        => GetMatchingGroupPrincipals(static _ => true, principalContext);

    /// <summary>
    /// Get local group whose a name satisfy the specified name.
    /// </summary>
    /// <param name="name">
    /// A group name.
    /// </param>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="GroupPrincipal"/> object for a group with the specified name.
    /// </returns>
    internal static GroupPrincipal GetMatchingGroupPrincipalsByName(string name, PrincipalContext principalContext)
        => GetMatchingGroupPrincipals(userPrincipal => name.Equals(userPrincipal.Name, StringComparison.CurrentCultureIgnoreCase), principalContext).FirstOrDefault();

    /// <summary>
    /// Get local group whose a security identifier (SID) satisfy the specified SID.
    /// </summary>
    /// <param name="sid">
    /// A group a security identifier (SID).
    /// </param>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="GroupPrincipal"/> object for a group with the specified security identifier (SID).
    /// </returns>
    internal static GroupPrincipal GetMatchingGroupPrincipalsBySID(SecurityIdentifier sid, PrincipalContext principalContext)
        => GetMatchingGroupPrincipals(userPrincipal => sid.Equals(userPrincipal.Sid), principalContext).FirstOrDefault();

    /// <summary>
    /// Get all local groups whose properties satisfy the specified predicate.
    /// </summary>
    /// <param name="principalFilter">
    /// Predicate that determines whether a group satisfies the conditions.
    /// </param>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{GroupPrincipal}"/> object containing GroupPrincipal
    /// objects that satisfy the predicate condition.
    /// </returns>
    internal static IEnumerable<GroupPrincipal> GetMatchingGroupPrincipals(Predicate<GroupPrincipal> principalFilter, PrincipalContext principalContext)
    {
        using var queryFilter = new GroupPrincipal(principalContext);
        using var searcher = new PrincipalSearcher(queryFilter);
        foreach (GroupPrincipal group in searcher.FindAll().Cast<GroupPrincipal>())
        {
            if (principalFilter(group))
            {
                yield return group;
            }
        }
    }

    /// <summary>
    /// Get all local group members of a group whose a name satisfy the specified name.
    /// </summary>
    /// <param name="name">
    /// A group name.
    /// </param>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{LocalPrincipal}"/> object containing members of a group with the specified name.
    /// </returns>
    internal static IEnumerable<LocalPrincipal> GetMatchingLocalGroupMembersByName(string name, PrincipalContext principalContext)
        => GetMatchingLocalGroupMembers(userPrincipal => name.Equals(userPrincipal.Name, StringComparison.CurrentCultureIgnoreCase), principalContext);

    /// <summary>
    /// Get all local group members of a group whose a security identifier (SID) satisfy the specified SID.
    /// </summary>
    /// <param name="sid">
    /// A group a security identifier (SID).
    /// </param>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{LocalPrincipal}"/> containing members of a group with the specified security identifier (SID).
    /// </returns>
    internal static IEnumerable<LocalPrincipal> GetMatchingLocalGroupMemebersBySID(SecurityIdentifier sid, PrincipalContext principalContext)
        => GetMatchingLocalGroupMembers(userPrincipal => sid.Equals(userPrincipal.Sid), principalContext);

    /// <summary>
    /// Get all local group members for a group whose properties satisfy the specified predicate.
    /// </summary>
    /// <param name="principalFilter">
    /// Predicate that determines whether a group satisfies the conditions.
    /// </param>
    /// <param name="principalContext">
    /// Encapsulates the server or domain against which all operations are performed.
    /// </param>
    /// <returns>
    /// An <see cref="IEnumerable{LocalPrincipal}"/> containing members of a group that satisfy the predicate condition.
    /// </returns>
    internal static IEnumerable<LocalPrincipal> GetMatchingLocalGroupMembers(Predicate<GroupPrincipal> principalFilter, PrincipalContext principalContext)
    {
        using var queryFolter = new GroupPrincipal(principalContext);
        using var searcher = new PrincipalSearcher(queryFolter);
        foreach (GroupPrincipal group in searcher.FindAll().Cast<GroupPrincipal>())
        {
            static string GetObjectClass(Principal p) => p switch
            {
                GroupPrincipal => Strings.ObjectClassGroup,
                UserPrincipal => Strings.ObjectClassUser,
                _ => Strings.ObjectClassOther
            };

            using (group)
            {
                if (!principalFilter(group))
                {
                    continue;
                }

                IEnumerator<Principal> members = group.GetMembers().GetEnumerator();
                bool hasItem = false;
                do
                {
                    hasItem = false;
                    LocalPrincipal localGroup = null;

                    try
                    {
                        // Try to move on to next member.
                        // `GroupPrincipal.GetMembers()` and `GroupPrincipal.Members` throw if an group member account was removed.
                        // It is a reason why we don't use `foreach (Principal principal in group.GetMembers()) { ... }`
                        // and we are forced to deconstruct the foreach in order to silently ignore such error and continue.
                        hasItem = members.MoveNext();

                        if (hasItem)
                        {
                            Principal principal = members.Current;
                            localGroup = new LocalPrincipal()
                            {
                                // Get name as 'Domain\user'
                                Name = principal.Sid.Translate(typeof(NTAccount)).ToString(),
                                PrincipalSource = Sam.GetPrincipalSource(principal.Sid),
                                SID = principal.Sid,
                                ObjectClass = GetObjectClass(principal),
                            };

                            /*
                            // Follow code is more useful but
                            //    1. it is a breaking change (output UserPrincipal and GoupPrincipal types instead of LocalPrincipal type)
                            //    2. it breaks a table output.
                            if (principal is GroupPrincipal)
                            {
                                localGroup = new LocalPrincipal()
                                {
                                    Name = principal.Name,
                                    PrincipalSource = Sam.GetPrincipalSource(principal.Sid),
                                    SID = principal.Sid,
                                    ObjectClass = GetObjectClass(principal),
                                };
                            }
                            else if (principal is UserPrincipal userPrincipal)
                            {
                               localGroup = GetLocalUser(userPrincipal);
                            }
                            */
                        }
                    }
                    catch (PrincipalOperationException)
                    {
                        // An error (1332) occurred while enumerating the group membership.  The member's SID could not be resolved.
                        hasItem = true;
                    }

                     if (localGroup is not null)
                    {
                        // `yield` can not be in try with catch block.
                        yield return localGroup;
                    }
                } while (hasItem);
            }
        }
    }
}
