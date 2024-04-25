// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Management.Automation;
using System.Management.Automation.SecurityAccountsManager;
using System.Management.Automation.SecurityAccountsManager.Extensions;
using System.Security.Principal;

using Microsoft.PowerShell.LocalAccounts;
#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Get-LocalGroupMember cmdlet gets the members of a local group.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "LocalGroupMember",
            DefaultParameterSetName = "Default",
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717988")]
    [Alias("glgm")]
    public class GetLocalGroupMemberCommand : Cmdlet, IDisposable
    {
        #region Instance Data
        // Explicitly point a domain name of the computer otherwise a domain name of current user would be used by default.
        private PrincipalContext _principalContext = new PrincipalContext(ContextType.Domain, LocalHelpers.GetComputerDomainName());
        private GroupPrincipal? _groupPrincipal;

        // Explicitly point DNS computer name to avoid very slow NetBIOS name resolutions.
        private PrincipalContext _groupPrincipalContext = new PrincipalContext(ContextType.Machine, LocalHelpers.GetFullComputerName());
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "Group".
        /// The security group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Group")]
        [ValidateNotNull]
        public LocalGroup Group { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "Member".
        /// Specifies the name of the user or group that is a member of this group. If
        /// this parameter is not specified, all members of the specified group are
        /// returned. This accepts a name, SID, or wildcard string.
        /// </summary>
        [Parameter(Position = 1)]
        [ValidateNotNullOrEmpty]
        public string Member { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// The security group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Default")]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "SID".
        /// The security group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "SecurityIdentifier")]
        [ValidateNotNullOrEmpty]
        public SecurityIdentifier SID { get; set; } = null!;
        #endregion Parameter Properties

        #region Cmdlet Overrides
        /// <summary>
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            try
            {
                if (Group is not null)
                {
                    _groupPrincipal = Group.SID is not null
                        ? GroupPrincipal.FindByIdentity(_groupPrincipalContext, IdentityType.Sid, Group.SID.Value)
                        : GroupPrincipal.FindByIdentity(_groupPrincipalContext, IdentityType.SamAccountName, Group.Name);
                }
                else if (Name is not null)
                {
                    _groupPrincipal = GroupPrincipal.FindByIdentity(_groupPrincipalContext, IdentityType.SamAccountName, Name);

                }
                else if (SID is not null)
                {
                    _groupPrincipal = GroupPrincipal.FindByIdentity(_groupPrincipalContext, IdentityType.Sid, SID.Value);
                }
            }
            catch (Exception ex)
            {
                ThrowTerminatingError(new ErrorRecord(ex, "GroupNotFound", ErrorCategory.ObjectNotFound, Group ?? new LocalGroup(Name) { SID = SID }));
            }

            if (_groupPrincipal is null)
            {
                LocalGroup target = Group ?? new LocalGroup(Name) { SID = SID };
                ThrowTerminatingError(new ErrorRecord(new GroupNotFoundException(target.ToString(), target), "GroupNotFound", ErrorCategory.ObjectNotFound, target));
            }
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                IEnumerable<LocalPrincipal>? principals = ProcessesMembership(MakeLocalPrincipals(_groupPrincipal!));

                if (principals is not null)
                {
                    WriteObject(principals, enumerateCollection: true);
                }
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "InvalidGetLocalGroupMemberOperation", ErrorCategory.InvalidOperation, targetObject: null));
            }
        }
        #endregion Cmdlet Overrides

        #region Private Methods
        private static IEnumerable<LocalPrincipal> MakeLocalPrincipals(GroupPrincipal groupPrincipal)
        {
            static string GetObjectClass(Principal p) => p switch
            {
                GroupPrincipal => Strings.ObjectClassGroup,
                UserPrincipal => Strings.ObjectClassUser,
                _ => Strings.ObjectClassOther
            };

            IEnumerator<Principal> members = groupPrincipal.GetMembers().GetEnumerator();
            bool hasItem = false;
            do
            {
                hasItem = false;
                LocalPrincipal? localPrincipal = null;

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
                        localPrincipal = new LocalPrincipal()
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
                            localPrincipal = new LocalPrincipal()
                            {
                                Name = principal.Name,
                                PrincipalSource = Sam.GetPrincipalSource(principal.Sid),
                                SID = principal.Sid,
                                ObjectClass = GetObjectClass(principal),
                            };
                        }
                        else if (principal is UserPrincipal userPrincipal)
                        {
                           localPrincipal = GetLocalUser(userPrincipal);
                        }
                        */
                    }
                }
                catch (PrincipalOperationException)
                {
                    // An error (1332) occurred while enumerating the group membership.  The member's SID could not be resolved.
                    hasItem = true;
                }

                if (localPrincipal is not null)
                {
                    // `yield` can not be in try with catch block.
                    yield return localPrincipal;
                }
            } while (hasItem);
        }

        private IEnumerable<LocalPrincipal> ProcessesMembership(IEnumerable<LocalPrincipal> membership)
        {
            List<LocalPrincipal> rv;

            // if no member filters are specified, return all
            if (Member is null)
            {
                rv = new List<LocalPrincipal>(membership);
            }
            else
            {
                rv = new List<LocalPrincipal>();

                if (WildcardPattern.ContainsWildcardCharacters(Member))
                {
                    var pattern = new WildcardPattern(Member, WildcardOptions.Compiled | WildcardOptions.IgnoreCase);

                    foreach (LocalPrincipal m in membership)
                    {
                        if (pattern.IsMatch(m.Name))
                        {
                            rv.Add(m);
                        }
                    }
                }
                else
                {
                    SecurityIdentifier? sid = this.TrySid(Member);

                    if (sid is not null)
                    {
                        foreach (LocalPrincipal m in membership)
                        {
                            if (m.SID == sid)
                            {
                                rv.Add(m);
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (LocalPrincipal m in membership)
                        {
                            if (Member.Equals(m.Name, StringComparison.CurrentCultureIgnoreCase))
                            {
                                rv.Add(m);
                                break;
                            }
                        }
                    }

                    if (rv.Count == 0)
                    {
                        var ex = new PrincipalNotFoundException(Member, Member);
                        WriteError(ex.MakeErrorRecord());
                    }
                }
            }

            // sort the resulting principals by name
            rv.Sort(static (p1, p2) => string.Compare(p1.Name, p2.Name, StringComparison.CurrentCultureIgnoreCase));

            return rv;
        }
        #endregion Private Methods

        #region IDisposable interface
        private bool _disposed;

        /// <summary>
        /// Dispose the DisableLocalUserCommand.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implementation of IDisposable for both manual Dispose() and finalizer-called disposal of resources.
        /// </summary>
        /// <param name="disposing">
        /// Specified as true when Dispose() was called, false if this is called from the finalizer.
        /// </param>
        protected void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _groupPrincipal?.Dispose();
                    _groupPrincipalContext.Dispose();
                    _principalContext?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion
    }
}
