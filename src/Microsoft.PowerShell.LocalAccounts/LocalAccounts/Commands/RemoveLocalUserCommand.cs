// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.DirectoryServices.AccountManagement;
using System.Management.Automation;
using System.Management.Automation.SecurityAccountsManager;
using System.Security.Principal;

using Microsoft.PowerShell.LocalAccounts;
#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Remove-LocalUser cmdlet deletes a user account from the Windows Security
    /// Accounts manager.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "LocalUser",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717982")]
    [Alias("rlu")]
    public class RemoveLocalUserCommand : Cmdlet, IDisposable
    {
        #region Instance Data
        private PrincipalContext _principalContext = new PrincipalContext(ContextType.Machine, LocalHelpers.GetFullComputerName());
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "InputObject".
        /// Specifies the of the local user accounts to remove in the local Security
        /// Accounts Manager.
        /// </summary>
        [Parameter(Mandatory = true,
                   Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "InputObject")]
        [ValidateNotNullOrEmpty]
        public LocalUser[] InputObject { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// Specifies the user accounts to be deleted from the local Security Accounts
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
        /// Specifies the local user accounts to remove by
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
            ProcessUsers();
            ProcessNames();
            ProcessSids();
        }
        #endregion Cmdlet Overrides

        #region Private Methods
        private static LocalUser GetTargetUserObject(UserPrincipal user)
            => new LocalUser()
            {
                Description = user.Description,
                Name = user.Name,
                PrincipalSource = Sam.GetPrincipalSource(user.Sid),
                SID = user.Sid,
            };

        /// <summary>
        /// Process users requested by -Name.
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
                            using UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(_principalContext, IdentityType.SamAccountName, name);
                            if (userPrincipal is null)
                            {
                                WriteError(new ErrorRecord(new UserNotFoundException(name, new LocalUser(name)), "UserNotFound", ErrorCategory.ObjectNotFound, name));
                            }
                            else
                            {
                                try
                                {
                                    userPrincipal.Delete();
                                }
                                catch (UnauthorizedAccessException)
                                {
                                    var exc = new AccessDeniedException(Strings.AccessDenied);

                                    ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: GetTargetUserObject(userPrincipal)));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(ex, "InvalidRemoveLocalUserOperation", ErrorCategory.InvalidOperation, targetObject: new LocalUser(name)));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Process users requested by -SID.
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
                            UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(_principalContext, IdentityType.Sid, sid.Value);
                            if (userPrincipal is null)
                            {
                                WriteError(new ErrorRecord(new UserNotFoundException(sid.Value, sid.Value), "UserNotFound", ErrorCategory.ObjectNotFound, sid.Value));
                            }
                            else
                            {
                                try
                                {
                                    userPrincipal.Delete();
                                }
                                catch (UnauthorizedAccessException)
                                {
                                    var exc = new AccessDeniedException(Strings.AccessDenied);

                                    ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: GetTargetUserObject(userPrincipal)));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(ex, "InvalidRemoveLocalUserOperation", ErrorCategory.InvalidOperation, targetObject: new LocalUser() { SID = sid }));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Process users given through -InputObject.
        /// </summary>
        private void ProcessUsers()
        {
            if (InputObject != null)
            {
                foreach (LocalUser user in InputObject)
                {
                    if (CheckShouldProcess(user.Name ?? user.SID?.Value))
                    {
                        try
                        {
                            using UserPrincipal userPrincipal = user.SID is not null
                                ? UserPrincipal.FindByIdentity(_principalContext, IdentityType.Sid, user.SID.Value)
                                : UserPrincipal.FindByIdentity(_principalContext, IdentityType.SamAccountName, user.Name);
                            if (userPrincipal is null)
                            {
                                WriteError(new ErrorRecord(new UserNotFoundException(user.Name ?? user.SID.Value, user), "GroupNotFound", ErrorCategory.ObjectNotFound, user));
                            }
                            else
                            {
                                try
                                {
                                    userPrincipal.Delete();
                                }
                                catch (UnauthorizedAccessException)
                                {
                                    var exc = new AccessDeniedException(Strings.AccessDenied);

                                    ThrowTerminatingError(new ErrorRecord(exc, "AccessDenied", ErrorCategory.PermissionDenied, targetObject: GetTargetUserObject(userPrincipal)));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            WriteError(new ErrorRecord(ex, "InvalidRemoveLocalGroupOperation", ErrorCategory.InvalidOperation, targetObject: new LocalGroup(user.Name) { SID = user.SID }));
                        }
                    }
                }
            }
        }

        private bool CheckShouldProcess(string target)
        {
            return ShouldProcess(target, Strings.ActionRemoveUser);
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
