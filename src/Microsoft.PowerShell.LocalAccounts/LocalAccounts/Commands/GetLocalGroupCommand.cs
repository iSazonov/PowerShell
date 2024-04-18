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
    /// The Get-LocalGroup cmdlet gets local groups from the Windows Security
    /// Accounts manager.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "LocalGroup",
            DefaultParameterSetName = "Default",
            HelpUri = "https://go.microsoft.com/fwlink/?LinkId=717974")]
    [Alias("glg")]
    public class GetLocalGroupCommand : Cmdlet
    {
        #region Instance Data
        private Sam sam = null;
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
        public string[] Name { get; set; }

        /// <summary>
        /// The following is the definition of the input parameter "SID".
        /// Specifies a local group from the local Security Accounts Manager.
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
                foreach (LocalGroup group in sam.GetAllLocalGroups())
                    WriteObject(group);

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

                            foreach (LocalGroup group in sam.GetMatchingLocalGroups(n => pattern.IsMatch(n)))
                                WriteObject(group);
                        }
                        else
                        {
                            WriteObject(sam.GetLocalGroup(name));
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
                        WriteObject(sam.GetLocalGroup(sid));
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
