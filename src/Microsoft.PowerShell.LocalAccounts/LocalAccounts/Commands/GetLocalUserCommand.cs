// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives
using System;
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
    public class GetLocalUserCommand : Cmdlet
    {
        #region Instance Data
        private Sam sam = null;
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
            if (Name == null && SID == null)
            {
                foreach (LocalUser user in sam.GetAllLocalUsers())
                    WriteObject(user);

                return;
            }

            ProcessNames();
            ProcessSids();
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

                            foreach (LocalUser user in sam.GetMatchingLocalUsers(n => pattern.IsMatch(n)))
                                WriteObject(user);
                        }
                        else
                        {
                            WriteObject(sam.GetLocalUser(nm));
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
                        WriteObject(sam.GetLocalUser(s));
                    }
                    catch (Exception ex)
                    {
                        WriteError(ex.MakeErrorRecord());
                    }
                }
            }
        }
        #endregion Private Methods
    }
}
