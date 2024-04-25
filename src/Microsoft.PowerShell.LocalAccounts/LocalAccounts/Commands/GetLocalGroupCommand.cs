// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
using System.DirectoryServices.AccountManagement;
using System.Management.Automation;
using System.Management.Automation.SecurityAccountsManager;
using System.Security.Principal;
#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// The Get-LocalGroup cmdlet gets local groups from the Windows Security
    /// Accounts manager.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "LocalGroup",
            DefaultParameterSetName = "Default",
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717974")]
    [Alias("glg")]
    public class GetLocalGroupCommand : Cmdlet, IDisposable
    {
        #region Instance Data
        private PrincipalContext _principalContext = new PrincipalContext(ContextType.Machine, LocalHelpers.GetFullComputerName());
        #endregion Instance Data

        #region Parameter Properties
        /// <summary>
        /// The following is the definition of the input parameter "Name".
        /// Specifies the local groups to get from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "Default")]
        [ValidateNotNull]
        public string[] Name { get; set; } = null!;

        /// <summary>
        /// The following is the definition of the input parameter "SID".
        /// Specifies a local group from the local Security Accounts Manager.
        /// </summary>
        [Parameter(Position = 0,
                   ValueFromPipeline = true,
                   ValueFromPipelineByPropertyName = true,
                   ParameterSetName = "SecurityIdentifier")]
        [ValidateNotNull]
        public SecurityIdentifier[] SID { get; set; } = null!;
        #endregion Parameter Properties

        #region Cmdlet Overrides
        /// <summary>
        /// ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (Name == null && SID == null)
            {
                try
                {
                    foreach (LocalGroup LocalGroup in LocalHelpers.GetAllLocalGroups(_principalContext))
                    {
                        WriteObject(LocalGroup);
                    }
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, "InvalidGetLocalGroupOperation", ErrorCategory.InvalidOperation, targetObject: null));
                }

                return;
            }

            ProcessNames();
            ProcessSids();
        }
        #endregion Cmdlet Overrides

        #region Private Methods
        /// <summary>
        /// Process groups requested by -Name.
        /// </summary>
        /// <remarks>
        /// All arguments to -Name will be treated as names,
        /// even if a name looks like a SID.
        /// Groups may be specified using wildcards.
        /// </remarks>
        private void ProcessNames()
        {
            if (Name != null)
            {
                foreach (string name in Name)
                {
                    try
                    {
                        if (WildcardPattern.ContainsWildcardCharacters(name))
                        {
                            var pattern = new WildcardPattern(name, WildcardOptions.Compiled | WildcardOptions.IgnoreCase);

                            foreach (LocalGroup localGroup in LocalHelpers.GetMatchingLocalGroups(userPrincipal => pattern.IsMatch(userPrincipal.Name), _principalContext))
                            {
                                WriteObject(localGroup);
                            }
                        }
                        else
                        {
                            WriteObject(LocalHelpers.GetMatchingLocalGroupsByName(name, _principalContext));
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(ex, "InvalidGetLocalGroupOperation", ErrorCategory.InvalidOperation, targetObject: new LocalUser(name)));
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
                    try
                    {
                        WriteObject(LocalHelpers.GetMatchingLocalGroupsBySID(sid, _principalContext));
                    }
                    catch (Exception ex)
                    {
                        WriteError(new ErrorRecord(ex, "InvalidGetLocalGroupOperation", ErrorCategory.InvalidOperation, targetObject: new LocalUser() { SID = sid}));
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
