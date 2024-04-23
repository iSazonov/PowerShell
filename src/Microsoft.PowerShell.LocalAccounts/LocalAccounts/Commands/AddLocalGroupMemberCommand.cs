// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
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
    /// The Add-LocalGroupMember cmdlet adds one or more users or groups to a local
    /// group.
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "LocalGroupMember",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717987")]
    [Alias("algm")]
    public class AddLocalGroupMemberCommand : PSCmdlet, IDisposable
    {
        #region Instance Data
        // Explicitly point a domain name of the computer otherwise a domain name of current user would be used by default.
        private PrincipalContext _principalContext = new PrincipalContext(ContextType.Domain, LocalHelpers.GetComputerDomainName());
        private GroupPrincipal _groupPrincipal;

        // Explicitly point DNS computer name to avoid very slow NetBIOS name resolutions.
        private PrincipalContext _groupPrincipalContext = new PrincipalContext(ContextType.Machine, LocalHelpers.GetFullComputerName());
        //private PrincipalContext _groupPrincipalContext = new PrincipalContext(ContextType.Machine, LocalHelpers.GetFullComputerName());
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "Group".
        /// Specifies a security group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ParameterSetName = "Group")]
        [ValidateNotNull]
        public LocalGroup Group { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "Member".
        /// Specifies one or more users or groups to add to this local group. You can
        /// identify users or groups by specifying their names or SIDs, or by passing
        /// Microsoft.PowerShell.Commands.LocalPrincipal objects.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 1,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public LocalPrincipal[] Member { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// Specifies a security group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ParameterSetName = "Default")]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "SID".
        /// Specifies a security group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ParameterSetName = "SecurityIdentifier")]
        [ValidateNotNull]
        public SecurityIdentifier SID { get; set; }
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
                        : GroupPrincipal.FindByIdentity(_groupPrincipalContext, IdentityType.Name, Group.Name);
                }
                else if (Name is not null)
                {
                    _groupPrincipal = GroupPrincipal.FindByIdentity(_groupPrincipalContext, IdentityType.Name, Name);

                }
                else if (SID is not null)
                {
                    _groupPrincipal = GroupPrincipal.FindByIdentity(_groupPrincipalContext, IdentityType.Sid, SID.Value);
                }
            }
            catch (Exception ex)
            {
                ThrowTerminatingError(new ErrorRecord(ex, "GroupNotFound", ErrorCategory.ObjectNotFound, GetTargetObject()));
            }

            if (_groupPrincipal is null)
            {
                ThrowTerminatingError(new ErrorRecord(new GroupNotFoundException(Group.Name, GetTargetObject()), "GroupNotFound", ErrorCategory.ObjectNotFound, GetTargetObject()));
            }
        }

        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            foreach (LocalPrincipal member in Member)
            {
                try
                {
                    using Principal principal = MakePrincipal(_groupPrincipal.Name, member);
                    if (principal is not null)
                    {
                        _groupPrincipal.Members.Add(principal);
                        _groupPrincipal.Save();
                    }
                }
                catch (UnauthorizedAccessException)
                {

                    var exc = new AccessDeniedException(member);
                    ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: GetTargetObject()));
                }
                catch (PrincipalExistsException)
                {
                    var exc = new MemberExistsException(member.Name, _groupPrincipal.Name, GetTargetObject());
                    WriteError(new ErrorRecord(exc, "MemberExists", ErrorCategory.ResourceExists, targetObject: GetTargetObject()));
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "InvalidAddOperation", ErrorCategory.InvalidOperation, targetObject: GetTargetObject()));

                }
            }
        }
        #endregion Cmdlet Overrides

        #region Private Methods

        private LocalGroup GetTargetObject()
            => new LocalGroup()
               {
                    Description = _groupPrincipal.Description,
                    Name = _groupPrincipal.Name,
                    PrincipalSource = Sam.GetPrincipalSource(_groupPrincipal.Sid),
                    SID = _groupPrincipal.Sid,
               };

        /// <summary>
        /// Creates a <see cref="Principal"/> object
        /// ready to be processed by the cmdlet.
        /// </summary>
        /// <param name="groupId">
        /// Name or SID (as a string) of the group we'll be adding to.
        /// This string is used only for specifying the target
        /// in WhatIf scenarios.
        /// </param>
        /// <param name="member">
        /// <see cref="LocalPrincipal"/> object to be processed.
        /// </param>
        /// <returns>
        /// A <see cref="Principal"/> object to be added to the group.
        /// </returns>
        /// <remarks>
        /// <para>
        /// <see cref="LocalPrincipal"/> objects in the Member parameter may not be complete,
        /// particularly those created from a name or a SID string given to the
        /// Member cmdlet parameter. The object returned from this method contains
        /// , at the very least, a valid SID.
        /// </para>
        /// <para>
        /// Any Member objects provided by name or SID string will be looked up
        /// to ensure that such an object exists. If an object is not found,
        /// null will be returned.
        /// </para>
        /// <para>
        /// This method also handles the WhatIf scenario. If the Cmdlet's
        /// ShouldProcess method returns false on any Member object,
        /// that object will not be included in the returned List.
        /// </para>
        /// </remarks>
        private Principal MakePrincipal(string groupId, LocalPrincipal member)
        {
            Principal principal;

            // If the member has a SID, we can use it directly.
            if (member.SID is not null)
            {
                principal = Principal.FindByIdentity(_principalContext, IdentityType.Sid, member.SID.Value);
            }
            else
            {
                // Otherwise it must have been constructed by name.
                SecurityIdentifier sid = this.TrySid(member.Name);

                if (sid is not null)
                {
                    member.SID = sid;
                    principal = Principal.FindByIdentity(_principalContext, IdentityType.Sid, member.SID.Value);
                }
                else
                {
                    principal = Principal.FindByIdentity(_principalContext, IdentityType.SamAccountName, member.Name);
                }
            }

            if (principal is null)
            {
                // It is a breaking change. AccountManagement API can not add a member by a fake SID, Windows PowerShell can do.
                WriteError(new ErrorRecord(new PrincipalNotFoundException(member.Name ?? member.SID.Value, member), "PrincipalNotFound", ErrorCategory.ObjectNotFound, member));

                return null;
            }

            return CheckShouldProcess(principal, groupId) ? principal : null;
        }

        /// <summary>
        /// Determine if a principal should be processed.
        /// Just a wrapper around Cmdlet.ShouldProcess, with localized string
        /// formatting.
        /// </summary>
        /// <param name="principal">Name of the principal to be added.</param>
        /// <param name="groupName">
        /// Name of the group to which the members will be added.
        /// </param>
        /// <returns>
        /// True if the principal should be processed, false otherwise.
        /// </returns>
        private bool CheckShouldProcess(Principal principal, string groupName)
        {
            if (principal == null)
            {
                return false;
            }

            string msg = StringUtil.Format(Strings.ActionAddGroupMember, principal.Name);

            return ShouldProcess(groupName, msg);
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
                    _groupPrincipal.Dispose();
                    _groupPrincipalContext.Dispose();
                    _principalContext?.Dispose();
                }

                _disposed = true;
            }
        }
        #endregion
    }
}
