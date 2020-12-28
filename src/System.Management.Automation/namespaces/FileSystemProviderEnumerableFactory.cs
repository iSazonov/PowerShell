// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Enumeration;
using System.Management.Automation;
using System.Management.Automation.Provider;

namespace Microsoft.PowerShell.Commands
{
    public sealed partial class FileSystemProvider : NavigationCmdletProvider,
                                                    IContentCmdletProvider,
                                                    IPropertyCmdletProvider,
                                                    ISecurityDescriptorCmdletProvider,
                                                    ICmdletProviderSupportsHelp
    {
        private static bool MatchesPattern(string expression, ReadOnlySpan<char> name, EnumerationOptions options)
        {
            bool ignoreCase = (options.MatchCasing == MatchCasing.PlatformDefault && !Platform.IsWindows)
                || options.MatchCasing == MatchCasing.CaseInsensitive;

            return options.MatchType switch
            {
                MatchType.Simple => FileSystemName.MatchesSimpleExpression(expression.AsSpan(), name, ignoreCase),
                MatchType.Win32 => FileSystemName.MatchesWin32Expression(expression.AsSpan(), name, ignoreCase),
                _ => throw new ArgumentOutOfRangeException(nameof(options)),
            };
        }

        private static int GetHRForWin32Error(int dwLastError)
        {
            if ((dwLastError & 0x80000000) == 0x80000000)
            {
                return dwLastError;
            }

            return (dwLastError & 0x0000FFFF) | unchecked((int)0x80070000);
        }

         private static IEnumerable<string> GetNames(
            CmdletProvider providerInstance,
            string directory,
            string expression,
            ReturnContainers returnContainers,
            Collection<WildcardPattern>? includeMatcher,
            Collection<WildcardPattern>? excludeMatcher,
            FlagsExpression<FileAttributes>? evaluator,
            FlagsExpression<FileAttributes>? switchEvaluator,
            bool filterHidden,
            InodeTracker? tracker,  // tracker will be non-null only if the user invoked the -FollowSymLinks and -Recurse switch parameters.
            uint depth,
            CmdletProviderContext context,
            EnumerationOptions options)
        {
            int errorCode = 0;

            // To calculate current depth we remove a root (start) directory from current directory
            // and count slashes in the tail.
            static uint GetCurrentDepth(ref FileSystemEntry entry)
            {
                var rootLength = entry.RootDirectory.Length;
                var tail = entry.Directory.Slice(rootLength);

                uint count = 0;
                int slashPosition = 0;

                while ((slashPosition = tail.IndexOf(StringLiterals.DefaultPathSeparator)) != -1)
                {
                    tail = tail.Slice(slashPosition + 1);
                    count++;
                }

                return count;
            }

            bool FileSystemEntryFilter(ref FileSystemEntry entry)
                {
                    if (errorCode != 0)
                {
                    // While enumerating we can get an error.
                    // In the case ShouldContinueOnErrorPredicate sets the error code.
                    // The error can be either 'access denied' or 'path not found'.
                    Exception? exc = System.Runtime.InteropServices.Marshal.GetExceptionForHR(GetHRForWin32Error(errorCode));
                    errorCode = 0;
                    if (exc is UnauthorizedAccessException)
                    {
                        context.WriteError(new ErrorRecord(exc, "DirUnauthorizedAccessError", ErrorCategory.PermissionDenied, entry.ToFullPath()));
                    }
                    else
                    {
                        context.WriteError(new ErrorRecord(exc, "DirIOError", ErrorCategory.ReadError, entry.ToFullPath()));
                    }
                }

                if ((returnContainers == ReturnContainers.ReturnAllContainers) && entry.Attributes.HasFlag(FileAttributes.Directory))
                {
                    return true;
                }

                bool attributeFilter = true;

                if (evaluator != null)
                {
                    // from expressions
                    attributeFilter = evaluator.Evaluate(entry.Attributes);
                }

                if (switchEvaluator != null)
                {
                    // from switch parameters
                    attributeFilter = attributeFilter && switchEvaluator.Evaluate(entry.Attributes);
                }

                bool hidden = entry.Attributes.HasFlag(FileAttributes.Hidden);

                // if "Hidden" is explicitly specified anywhere in the attribute filter, then override
                // default hidden attribute filter.
                if (!(attributeFilter && (filterHidden || context.Force || !hidden) && MatchesPattern(expression, entry.FileName, options)))
                {
                    return false;
                }

                bool isIncludeMatch =
                    includeMatcher == null ||
                    includeMatcher.Count == 0 ||
                    SessionStateUtilities.MatchesAnyWildcardPattern(
                        entry.FileName.ToString(),
                        includeMatcher,
                        true);

                if (isIncludeMatch)
                {
                    return
                        excludeMatcher == null ||
                        excludeMatcher.Count == 0 ||
                        !SessionStateUtilities.MatchesAnyWildcardPattern(
                            entry.FileName.ToString(),
                            excludeMatcher,
                            false);
                }

                return false;
            }

            // Here transformation lambda returns relative path based on OriginalRootDirectory
            return new FileSystemProviderEnumerable<string>(
                directory,
                (ref FileSystemEntry entry) => entry.OriginalRootDirectory.Length > entry.Directory.Length ? entry.FileName.ToString() : Path.Join(entry.Directory.Slice(entry.OriginalRootDirectory.Length), entry.FileName),
                options)
            {
                ShouldIncludePredicate = FileSystemEntryFilter,

                ShouldRecursePredicate = (ref FileSystemEntry entry) =>
                    {
                        // Making sure to obey the StopProcessing.
                        if (context.Stopping ||
                           (depth < uint.MaxValue && GetCurrentDepth(ref entry) >= depth))
                        {
                            return false;
                        }

                        bool hidden = false;
                        if (!context.Force)
                        {
                            hidden = entry.Attributes.HasFlag(FileAttributes.Hidden);
                        }

                        // if "Hidden" is explicitly specified anywhere in the attribute filter, then override
                        // default hidden attribute filter.
                        if (context.Force || !hidden || filterHidden)
                        {
                            // We only want to recurse into symlinks if
                            //  a) the user has asked to with the -FollowSymLinks switch parameter and
                            //  b) the directory pointed to by the symlink has not already been visited,
                            //     preventing symlink loops.
                            //  c) it is not a reparse point with a target (not OneDrive or an AppX link).
                            if (tracker == null)
                            {
                                if (entry.Attributes.HasFlag(FileAttributes.ReparsePoint) && InternalSymbolicLinkLinkCodeMethods.IsReparsePointWithTarget(entry.ToFileSystemInfo()))
                                {
                                    return false;
                                }
                            }
                            else if (!tracker.TryVisitPath(entry.ToFullPath()))
                            {
                                providerInstance.WriteWarning(
                                    System.Management.Automation.Internal.StringUtil.Format(
                                        FileSystemProviderStrings.AlreadyListedDirectory,
                                        entry.ToFullPath()));
                                return false;
                            }

                            return true;
                        }

                        return false;
                    },

                ShouldContinueOnErrorPredicate = (int error) =>
                    {
                        // We can not write user-friendly error message for current path because we can not get its value in the method.
                        // So we save the error code to process it later (in FileSystemEntryFilter() method) and suppress throw.
                        errorCode = error;
                        return true;
                    }
            };
        }

        /// <summary>
        /// Gets the child names of the item at the specified path by
        /// manually recursing through all the containers instead of
        /// allowing the provider to do the recursion.
        /// </summary>
        /// <param name="providerInstance">
        /// The provider instance to use.
        /// </param>
        /// <param name="newProviderPath">
        /// The path to the item if it was specified on the command line.
        /// </param>
        /// <param name="recurse">
        /// If true all names in the subtree should be returned.
        /// </param>
        /// <param name="depth">
        /// Current depth of recursion; special case uint.MaxValue performs full recursion.
        /// </param>
        /// <param name="returnContainers">
        /// Determines if all containers should be returned or only those containers that match the
        /// filter(s).
        /// </param>
        /// <param name="includeMatcher">
        /// A set of filters that the names must match to be returned.
        /// </param>
        /// <param name="excludeMatcher">
        /// A set of filters that the names cannot match to be returned.
        /// </param>
        /// <param name="context">
        /// The context which the core command is running.
        /// </param>
        internal void DoGetChildNamesFast(
            CmdletProvider providerInstance,
            string newProviderPath,
            ReturnContainers returnContainers,
            Collection<WildcardPattern>? includeMatcher,
            Collection<WildcardPattern>? excludeMatcher,
            CmdletProviderContext context,
            bool recurse,
            uint depth)
        {
            if (newProviderPath == null)
            {
                throw new ArgumentNullException(nameof(newProviderPath), nameof(DoGetChildNamesFast));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context), nameof(DoGetChildNamesFast));
            }

            CmdletProviderContext childNamesContext =
                new CmdletProviderContext(context);

            try
            {
                FlagsExpression<FileAttributes>? evaluator = null;
                FlagsExpression<FileAttributes>? switchEvaluator = null;
                InodeTracker? tracker = null;

                if (DynamicParameters is GetChildDynamicParameters fspDynamicParam)
                {
                    evaluator = fspDynamicParam.Attributes;
                    switchEvaluator = FormatAttributeSwitchParameters();

                    if (recurse && fspDynamicParam.FollowSymlink)
                    {
                        tracker = new InodeTracker(newProviderPath);
                    }
                }

                bool filterHidden = false;

                if (evaluator != null)
                {
                    // "Hidden" is specified somewhere in the expression
                    filterHidden = evaluator.ExistsInExpression(FileAttributes.Hidden);
                }

                if (switchEvaluator != null)
                {
                    // "Hidden" is specified somewhere in the parameters
                    filterHidden = filterHidden || switchEvaluator.ExistsInExpression(FileAttributes.Hidden);
                }

                foreach (var result in
                    GetNames(
                        this,
                        newProviderPath,
                        Filter ?? "*",
                        returnContainers,
                        includeMatcher,
                        excludeMatcher,
                        evaluator,
                        switchEvaluator,
                        filterHidden,
                        tracker,
                        depth,
                        context,
                        new System.IO.EnumerationOptions { RecurseSubdirectories = recurse, MatchType = MatchType.Win32, AttributesToSkip = 0, IgnoreInaccessible = false }))
                {
                    context.WriteObject(result);
                }
            }
            finally
            {
                childNamesContext.RemoveStopReferral();
            }
        }
    }
}