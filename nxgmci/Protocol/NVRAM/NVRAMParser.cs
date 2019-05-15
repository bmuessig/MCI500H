using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace nxgmci.Protocol.NVRAM
{
    /// <summary>
    /// Thread-safe and static class for parsing NVRAM responses.
    /// </summary>
    public static class NVRAMParser
    {
        // The Regex strings
        private const string WRAP_REGEX = @"^\s*<html.*?>.*<title.*?>\s*nvramd\s*<\/title>.*<body.*?>\s*\n([\s\S]*)\n\s*<\/body>.*<\/html>\s*$";
        private const string ITEM_REGEX = @"^(.*) = '(.*)'\s*<br\s*\/?\s*>$";

        // The precompiled Regexes
        private static readonly Regex wrapRegex;
        private static readonly Regex itemRegex;

        /// <summary>
        /// Static constructor.
        /// </summary>
        static NVRAMParser()
        {
            // Precompile the Regexes
            wrapRegex = new Regex(WRAP_REGEX, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
            itemRegex = new Regex(ITEM_REGEX, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
        }

        /// <summary>
        /// Parses a single NVRAM parameter or response and returns the result.
        /// </summary>
        /// <param name="Input">The input string received from the stereo.</param>
        /// <returns>A result object. If the parsing succeeded, the resulting value is also returned.</returns>
        public static Result<string> ParseSingle(string Input)
        {
            // Allocate the result
            Result<string> result = new Result<string>();

            // Input sanity check
            if (string.IsNullOrWhiteSpace(Input))
                return Result<string>.FailMessage(result, "The input may not be null, empty or white-space only!");

            // Parse the input
            Match match = wrapRegex.Match(Input);

            // Check, if the match succeeded and if requried number of matches exists
            if (!match.Success)
                return Result<string>.FailMessage(result, "The input structure did not match!");
            if (match.Groups.Count != 2)
                return Result<string>.FailMessage(result, "The root group count was incorrect (was {0}, should be 2)!", match.Groups.Count);

            // Return the result
            return Result<string>.SucceedProduct(result, match.Groups[1].Value);
        }

        /// <summary>
        /// Parses the index list of all NVRAM parameters and returns the result.
        /// </summary>
        /// <param name="Input">The input string received from the stereo.</param>
        /// <param name="FailOnError">Indicates whether to fail after encountering an invalid item.</param>
        /// <returns>A result object. If the parsing succeeded, the resulting collection is also returned.</returns>
        public static Result<Dictionary<string, string>> ParseList(string Input, bool FailOnError = false)
        {
            // Allocate the result
            Result<Dictionary<string, string>> result = new Result<Dictionary<string, string>>();

            // Input sanity check
            if (string.IsNullOrWhiteSpace(Input))
                return Result<Dictionary<string, string>>.FailMessage(result, "The input may not be null, empty or white-space only!");

            // Parse the input to unwrap it
            Match rootMatch = wrapRegex.Match(Input);

            // Check, if the match succeeded and if requried number of matches exists
            if (!rootMatch.Success)
                return Result<Dictionary<string, string>>.FailMessage(result, "The input's root structure did not match!");
            if (rootMatch.Groups.Count != 2)
                return Result<Dictionary<string, string>>.FailMessage(result, "The root group count was incorrect (was {0}, should be 2)!", rootMatch.Groups.Count);

            // Next, parse the input again to get the individual values
            MatchCollection itemMatches = itemRegex.Matches(Input);

            // Allocate the resulting dictionary and an item counter
            Dictionary<string, string> resultDict = new Dictionary<string, string>();
            uint itemCount = 0;

            // Iterate through all items to parse them
            foreach (Match itemMatch in itemMatches)
            {
                // Check, if the item failed
                if (!itemMatch.Success)
                {
                    if (FailOnError)
                        return Result<Dictionary<string, string>>.FailMessage(result, "Item #{0} did not match!", itemCount);
                    itemCount++;
                    continue;
                }

                // Check, if the number of groups matches
                if (itemMatch.Groups.Count != 3)
                {
                    if (FailOnError)
                        return Result<Dictionary<string, string>>.FailMessage(result, "The root group count was incorrect (was {0}, should be 3)!", itemMatch.Groups.Count);
                    itemCount++;
                    continue;
                }

                // Add the item to the collection
                resultDict.Add(itemMatch.Groups[1].Value, itemMatch.Groups[2].Value);
                itemCount++;
            }

            // Return the result
            return Result<Dictionary<string, string>>.SucceedProduct(result, resultDict);
        }
    }
}
