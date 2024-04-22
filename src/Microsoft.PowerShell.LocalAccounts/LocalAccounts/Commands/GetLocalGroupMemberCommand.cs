// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.SecurityAccountsManager;
using System.Management.Automation.SecurityAccountsManager.Extensions;
using System.Security.Principal;
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
        private PrincipalContext _principalContext = new PrincipalContext(ContextType.Machine, LocalHelpers.GetFullComputerName());
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
        public LocalGroup Group { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "Member".
        /// Specifies the name of the user or group that is a member of this group. If
        /// this parameter is not specified, all members of the specified group are
        /// returned. This accepts a name, SID, or wildcard string.
        /// </summary>
        [Parameter(Position = 1)]
        [ValidateNotNullOrEmpty]
        public string Member { get; set; }

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
        public string Name { get; set; }

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
        public SecurityIdentifier SID { get; set; }
        #endregion Parameter Properties

        #region Cmdlet Overrides
        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            try
            {
                IEnumerable<LocalPrincipal> principals = null;

                if (Group != null)
                {
                    principals = ProcessGroup(Group);
                }
                else if (Name != null)
                {
                    principals = ProcessName(Name);
                }
                else if (SID != null)
                {
                    principals = ProcessSid(SID);
                }

                if (principals != null)
                {
                    WriteObject(principals, enumerateCollection: true);
                }
            }
            catch (Exception ex)
            {
                WriteError(ex.MakeErrorRecord());
            }
        }
        #endregion Cmdlet Overrides

        #region Private Methods
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
                    SecurityIdentifier sid = this.TrySid(Member);

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
                            if (m.Name.Equals(Member, StringComparison.CurrentCultureIgnoreCase))
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

        private IEnumerable<LocalPrincipal> ProcessGroup(LocalGroup group)
        {
            return ProcessesMembership(LocalHelpers.GetMatchingLocalGroupMemebersBySID(group.SID, _principalContext));
        }

        private IEnumerable<LocalPrincipal> ProcessName(string name)
        {
            return ProcessesMembership(LocalHelpers.GetMatchingLocalGroupMembersByName(name, _principalContext));
        }

        private IEnumerable<LocalPrincipal> ProcessSid(SecurityIdentifier groupSid)
        {
            return ProcessesMembership(LocalHelpers.GetMatchingLocalGroupMemebersBySID(groupSid, _principalContext));
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
                    _principalContext?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion
    }
}
