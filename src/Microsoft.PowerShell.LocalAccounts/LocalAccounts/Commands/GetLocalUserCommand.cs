// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.DirectoryServices.AccountManagement;
using System.Management.Automation;
using System.Management.Automation.SecurityAccountsManager;
using System.Management.Automation.SecurityAccountsManager.Extensions;
using System.Security.Principal;
#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Get-LocalUser cmdlet gets local user accounts from the Windows Security
    /// Accounts Manager. This includes local accounts that have been connected to a
    /// Microsoft account.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "LocalUser",
            DefaultParameterSetName = "Default",
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717980")]
    [Alias("glu")]
    public class GetLocalUserCommand : Cmdlet, IDisposable
    {
        #region Instance Data
        private PrincipalContext _principalContext = new PrincipalContext(ContextType.Machine);
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// Specifies the local user accounts to get from the local Security Accounts
        /// Manager. This accepts a name or wildcard string.
        /// </summary>
        [Parameter(Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Default")]
        [ValidateNotNull]
        public string[] Name { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "SID".
        /// Specifies a user from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "SecurityIdentifier")]
        [ValidateNotNull]
        public SecurityIdentifier[] SID { get; set; }
        #endregion Parameter Properties

        #region Cmdlet Overrides
        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (Name == null && SID == null)
            {
                foreach (LocalUser localUser in LocalHelpers.GetMatchingLocalUsers(static _ => true, _principalContext))
                {
                    WriteObject(localUser);
                }

                return;
            }

            ProcessNames();
            ProcessSids();
        }
        #endregion Cmdlet Overrides

        #region Private Methods
        /// <summary>
        /// Process users requested by -Name.
        /// </summary>
        /// <remarks>
        /// All arguments to -Name will be treated as names,
        /// even if a name looks like a SID.
        /// Users may be specified using wildcards.
        /// </remarks>
        private void ProcessNames()
        {
            if (Name != null)
            {
                foreach (string nm in Name)
                {
                    try
                    {
                        if (WildcardPattern.ContainsWildcardCharacters(nm))
                        {
                            var pattern = new WildcardPattern(nm, WildcardOptions.Compiled | WildcardOptions.IgnoreCase);
                            foreach (LocalUser localUser in LocalHelpers.GetMatchingLocalUsers(userPrincipal => pattern.IsMatch(userPrincipal.Name), _principalContext))
                            {
                                WriteObject(localUser);
                            }
                        }
                        else
                        {
                            foreach (LocalUser localUser in LocalHelpers.GetMatchingLocalUsers(userPrincipal => nm.Equals(userPrincipal.Name, StringComparison.CurrentCultureIgnoreCase), _principalContext))
                            {
                                WriteObject(localUser);
                                break;
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
                foreach (SecurityIdentifier s in SID)
                {
                    try
                    {
                        foreach (LocalUser localUser in LocalHelpers.GetMatchingLocalUsers(userPrincipal => s.Equals(userPrincipal.Sid), _principalContext))
                        {
                            WriteObject(localUser);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(ex.MakeErrorRecord());
                    }
                }
            }
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
