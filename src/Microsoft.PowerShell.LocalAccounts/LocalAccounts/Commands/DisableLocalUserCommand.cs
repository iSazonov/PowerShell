// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
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
    public class DisableLocalUserCommand : Cmdlet
    {
        #region Constants
        private const Enabling enabling = Enabling.Disable;
        #endregion Constants

        #region Instance Data
        private Sam sam = null;
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
        /// BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            sam = new Sam();
        }

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

        /// <summary>
        /// EndProcessing method.
        /// </summary>
        protected override void EndProcessing()
        {
            if (sam != null)
            {
                sam.Dispose();
                sam = null;
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
                            sam.EnableLocalUser(sam.GetLocalUser(name), enabling);
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
                        if (CheckShouldProcess(sid.ToString()))
                            sam.EnableLocalUser(sid, enabling);
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
                            sam.EnableLocalUser(user, enabling);
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
    }
}
