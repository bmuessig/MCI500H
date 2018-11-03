using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace nxgmci.Parsers
{
    public class WADMParser
    {
        readonly string RootName;
        readonly string WrapOrListName;
        readonly bool IsList;

        private const string ROOT_WRAP_REGEX = @"^\s*<{0}>\s*<{1}>\s*([\s\S]*)\s*<\/{1}>\s*<\/{0}>\s*$";
        private const string ROOT_LIST_REGEX = @"^\s*<{0}>\s*([\s\S]*)\s*<\/{0}>\s*$";
        private const string LIST_REGEX = @"<\s*{0}\s*>\s*([\s\S]*?)\s*<\s*\/\s*{0}\s*>";
        private const string ELEM_REGEX = @"<\s*([\s\S]*?)\s*(?:\/\s*>|>([\s\S]*?)<\s*\/\1\s*>)";

        private readonly Regex rootWrapOrListRegex;
        private readonly Regex listRegex;
        private readonly Regex elementRegex;

        public WADMParser(string RootName, string WrapOrListName, bool IsList)
        {
            // Sanity check input
            if(string.IsNullOrWhiteSpace(RootName))
                throw new ArgumentNullException("RootName");
            if (string.IsNullOrWhiteSpace(WrapOrListName))
                throw new ArgumentNullException("WrapOrListName");

            // Initialize the state
            this.RootName = RootName.Trim();
            this.WrapOrListName = WrapOrListName.Trim();
            this.IsList = IsList;

            // Initialize the Regexes
            // First, the element regex used to match each XML value element
            elementRegex = new Regex(ELEM_REGEX, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            
            // Then, initialize either Element-wrap or the List Regexes
            if (IsList)
            {
                // This Regex matches only the root element of our list
                rootWrapOrListRegex = new Regex(string.Format(ROOT_LIST_REGEX, this.RootName),
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);
                // This Regex matches each list item
                listRegex = new Regex(string.Format(LIST_REGEX, this.WrapOrListName),
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            else // This Regex matches the root and wrap of our elements
                rootWrapOrListRegex = new Regex(string.Format(ROOT_WRAP_REGEX, this.RootName, this.WrapOrListName),
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);

        }

        public WADMResult Parse(string Input, bool LooseSyntax = false)
        {
            // Input sanity check
            if (string.IsNullOrWhiteSpace(Input))
                return new WADMResult("The input may not be null, empty or white-space only!");
            
            // Match the input against our root level Regex
            // This is used to verify that the reply is correct and it will strip away the root wrapper
            Match rootMatch = rootWrapOrListRegex.Match(Input);

            // Make sure we've got success and two match groups
            if (!rootMatch.Success)
                return new WADMResult("The root structure did not match!");
            if (rootMatch.Groups.Count != 2)
                return new WADMResult("The root group count was incorrect!");
            if (!rootMatch.Groups[1].Success)
                return new WADMResult("The root content group did not succeed!");

            // Allocate space for our successful reply
            WADMResult result = new WADMResult(RootName, WrapOrListName, IsList);

            // Now, match the inner elements
            string innerText = rootMatch.Groups[1].Value;
            // Allocate a string for potentional parser error handling
            string parserError;

            // Inner text should later contain only non-list, elements
            if (IsList)
            {
                // Allocate space for our list
                List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();

                // Match all list elements
                MatchCollection listMatches = listRegex.Matches(innerText);

                // Loop through the list elements
                foreach (Match listMatch in listMatches)
                {
                    // Make sure our match succeeded
                    if (!listMatch.Success)
                        if (LooseSyntax)
                            continue;
                        else
                            return new WADMResult("The list item did not match successfully in strict mode!");

                    // Also make sure we've got the right number of groups
                    if (listMatch.Groups.Count != 2)
                        if (LooseSyntax)
                            continue;
                        else
                            return new WADMResult("The number of matched list item groups was incorrect in strict mode!");

                    if(listMatch.Groups[1].Value == null)
                        if (LooseSyntax)
                            continue;
                        else
                            return new WADMResult("The value of the matched list item group was null in strict mode!");

                    // If everything's right, we will parse the inner elements
                    Dictionary<string, string> innerElements = ParseElements(listMatch.Groups[1].Value, out parserError, LooseSyntax);
                    if (innerElements == null)
                        return new WADMResult(parserError);

                    // Finally, we will add the element to our list
                    // Yes, we will even add empty elements, as they were potentionally intentionally left blank
                    list.Add(innerElements);
                }

                // Now, we remove the successful elements from further processing
                innerText = listRegex.Replace(innerText, string.Empty);

                // And finally, we will add the list to the result
                result.List = list;
            }

            // Parse the root level key-value elements
            Dictionary<string, string> elements = ParseElements(innerText, out parserError, LooseSyntax);
            if (elements == null)
                return new WADMResult(parserError);

            // And store them in the result
            result.Elements = elements;

            // Finally, successfully return the result
            return result;
        }

        private Dictionary<string, string> ParseElements(string Input, out string Error, bool LooseSyntax = false)
        {
            // Check input
            if (string.IsNullOrWhiteSpace(Input))
            {
                Error = "The input may not be null or white-space!";
                return null;
            }

            // Allocate room for our elements
            Dictionary<string, string> elements = new Dictionary<string, string>();

            // Match our elements
            MatchCollection matches = elementRegex.Matches(Input);
            if (matches == null)
            {
                Error = "The match-collection was null!";
                return null;
            }

            // Loop through our matches
            // Also, note that we don't support nesting even though we could
            // It's just not a requirement here, so it will not be implemented
            foreach (Match match in matches)
            {
                // Check, if that match was a success
                if (!match.Success)
                    if (LooseSyntax)
                        continue;
                    else
                    {
                        Error = "The element did not match successfully in strict mode!";
                        return null;
                    }
                
                // Now, make sure we've got three elements in our group
                if (match.Groups.Count != 3)
                    if (LooseSyntax)
                        continue;
                    else
                    {
                        Error = "The number of matched element groups was incorrect in strict mode!";
                        return null;
                    }
                
                // Next, check the key result group
                if (string.IsNullOrWhiteSpace(match.Groups[1].Value))
                    if (LooseSyntax)
                        continue;
                    else
                    {
                        Error = "The element's key contained no content in strict mode!";
                        return null;
                    }
                
                // Trim the key
                string key = match.Groups[1].Value.Trim(), value = match.Groups[2].Value.Trim();

                // Replace some escape sequences in the value
                value = value.Replace("&apos;", "'");
                value = value.Replace("&quot;", "\"");
                value = value.Replace("&lt;", "<");
                value = value.Replace("&gt;", ">");
                value = value.Replace("&amp;", "&");

                // After that, determine if the entry already exists and add it
                if (elements.ContainsKey(key))
                    if (LooseSyntax)
                        elements[key] = value;
                    else
                    {
                        Error = "The element had already been encountered in strict mode!";
                        return null;
                    }
                else
                    elements.Add(key, value);
            }

            // Finally, return the elements
            Error = null;
            return elements;
        }
    }
}
