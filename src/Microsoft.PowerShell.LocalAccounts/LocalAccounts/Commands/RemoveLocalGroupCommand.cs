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
    /// The Remove-LocalGroup cmdlet deletes a security group from the Windows
    /// Security Accounts manager.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "LocalGroup",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717975")]
    [Alias("rlg")]
    public class RemoveLocalGroupCommand : Cmdlet, IDisposable
    {
        #region Instance Data
        private PrincipalContext _principalContext = new PrincipalContext(ContextType.Machine, LocalHelpers.GetFullComputerName());
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "InputObject".
        /// Specifies security groups from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "InputObject")]
        [ValidateNotNullOrEmpty]
        public LocalGroup[] InputObject { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// Specifies the local groups to be deleted from the local Security Accounts
        /// Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Default")]
        [ValidateNotNullOrEmpty]
        public string[] Name { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "SID".
        /// Specifies the LocalGroup accounts to remove by
        /// System.Security.Principal.SecurityIdentifier.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "SecurityIdentifier")]
        [ValidateNotNullOrEmpty]
        public SecurityIdentifier[] SID { get; set; }
        #endregion Parameter Properties

        #region Cmdlet Overrides
        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            ProcessGroups();
            ProcessNames();
            ProcessSids();
        }
        #endregion Cmdlet Overrides

        #region Private Methods
        private static LocalGroup GetTargetGroupObject(GroupPrincipal group)
            => new LocalGroup()
            {
                Description = group.Description,
                Name = group.Name,
                PrincipalSource = Sam.GetPrincipalSource(group.Sid),
                SID = group.Sid,
            };

        /// <summary>
        /// Process groups requested by -Name.
        /// </summary>
        /// <remarks>
        /// All arguments to -Name will be treated as names,
        /// even if a name looks like a SID.
        /// </remarks>
        private void ProcessNames()
        {
            if (Name != null)
            {
                foreach (var name in Name)
                {
                    if (CheckShouldProcess(name))
                    {
                        try
                        {
                            using GroupPrincipal group = LocalHelpers.GetMatchingGroupPrincipalsByName(name, _principalContext);
                            if (group is null)
                            {
                                WriteError(new ErrorRecord(new GroupNotFoundException(name, new LocalGroup(name)), "GroupNotFound", ErrorCategory.ObjectNotFound, name));
                            }
                            else
                            {
                                try
                                {
                                    group.Delete();
                                }
                                catch (UnauthorizedAccessException)
                                {
                                    var exc = new AccessDeniedException(Strings.AccessDenied);

                                    ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: GetTargetGroupObject(group)));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(ex, "InvalidRemoveLocalGroupOperation", ErrorCategory.InvalidOperation, targetObject: new LocalGroup(name)));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Process groups requested by -SID.
        /// </summary>
        private void ProcessSids()
        {
            if (SID != null)
            {
                foreach (SecurityIdentifier sid in SID)
                {
                    if (CheckShouldProcess(sid.Value))
                    {
                        try
                        {
                            GroupPrincipal group = LocalHelpers.GetMatchingGroupPrincipalsBySID(sid, _principalContext);
                            if (group is null)
                            {
                                WriteError(new ErrorRecord(new GroupNotFoundException(sid.Value, sid.Value), "GroupNotFound", ErrorCategory.ObjectNotFound, sid.Value));
                            }
                            else
                            {
                                try
                                {
                                    group.Delete();
                                }
                                catch (UnauthorizedAccessException)
                                {
                                    var exc = new AccessDeniedException(Strings.AccessDenied);

                                    ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: GetTargetGroupObject(group)));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(ex, "InvalidRemoveLocalGroupOperation", ErrorCategory.InvalidOperation, targetObject: new LocalGroup() { SID = sid }));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Process groups given through -InputObject.
        /// </summary>
        private void ProcessGroups()
        {
            if (InputObject != null)
            {
                foreach (LocalGroup group in InputObject)
                {
                    if (CheckShouldProcess(group.Name ?? group.SID?.Value))
                    {
                        try
                        {
                            using GroupPrincipal groupPrincipal = group.SID is not null
                                ? GroupPrincipal.FindByIdentity(_principalContext, IdentityType.Sid, group.SID.Value)
                                : GroupPrincipal.FindByIdentity(_principalContext, IdentityType.Name, group.Name);
                            if (groupPrincipal is null)
                            {
                                WriteError(new ErrorRecord(new GroupNotFoundException(group.Name ?? group.SID.Value, group), "GroupNotFound", ErrorCategory.ObjectNotFound, group));
                            }
                            else
                            {
                                try
                                {
                                    groupPrincipal.Delete();
                                }
                                catch (UnauthorizedAccessException)
                                {
                                    var exc = new AccessDeniedException(Strings.AccessDenied);

                                    ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: GetTargetGroupObject(groupPrincipal)));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(ex, "InvalidRemoveLocalGroupOperation", ErrorCategory.InvalidOperation, targetObject: new LocalGroup(group.Name) { SID = group.SID }));
                        }
                    }
                }
            }
        }

        private bool CheckShouldProcess(string target)
        {
            return ShouldProcess(target, Strings.ActionRemoveGroup);
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
