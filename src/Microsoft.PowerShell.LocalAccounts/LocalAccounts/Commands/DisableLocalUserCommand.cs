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
    /// The Disable-LocalUser cmdlet disables local user accounts. When a user
    /// account is disabled, the user is not permitted to log on. When a user
    /// account is enabled, the user is permitted to log on normally.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Disable, "LocalUser",
            SupportsShouldProcess = true,
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717986")]
    [Alias("dlu")]
    public class DisableLocalUserCommand : Cmdlet, IDisposable
    {
        #region Instance Data
        private PrincipalContext _principalContext = new PrincipalContext(ContextType.Machine, LocalHelpers.GetFullComputerName());
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "InputObject".
        /// Specifies the of the local user accounts to disable in the local Security
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
        /// Specifies the names of the local user accounts to disable in the local
        /// Security Accounts Manager.
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
        /// Specifies the LocalUser accounts to disable by
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
            try
            {
                ProcessUsers();
                ProcessNames();
                ProcessSids();
            }
            catch (Exception ex)
            {
                WriteError(ex.MakeErrorRecord());
            }
        }
        #endregion Cmdlet Overrides

        #region Private Methods
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
                foreach (string name in Name)
                {
                    try
                    {
                        if (CheckShouldProcess(name))
                        {
                            UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(_principalContext, name);
                            if (userPrincipal is not null)
                            {
                                userPrincipal.Enabled = false;
                                userPrincipal.Save();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(ex.MakeErrorRecord());
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
                    try
                    {
                        var sidString = sid.ToString();
                        if (CheckShouldProcess(sidString))
                        {
                            UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(_principalContext, IdentityType.Sid, sidString);
                            if (userPrincipal is not null)
                            {
                                userPrincipal.Enabled = false;
                                userPrincipal.Save();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(ex.MakeErrorRecord());
                    }
                }
            }
        }

        /// <summary>
        /// Process users requested by -InputObject.
        /// </summary>
        private void ProcessUsers()
        {
            if (InputObject != null)
            {
                foreach (LocalUser user in InputObject)
                {
                    try
                    {
                        if (CheckShouldProcess(user.Name))
                        {
                            UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(_principalContext, user.Name);
                            if (userPrincipal is not null)
                            {
                                userPrincipal.Enabled = false;
                                userPrincipal.Save();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(ex.MakeErrorRecord());
                    }
                }
            }
        }

        private bool CheckShouldProcess(string target)
        {
            return ShouldProcess(target, Strings.ActionDisableUser);
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
