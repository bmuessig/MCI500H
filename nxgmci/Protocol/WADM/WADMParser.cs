using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// Universal parser for parsing WADM API responses into result objects.
    /// </summary>
    public class WADMParser
    {
        /// <summary>
        /// The maximum length of an XML field.
        /// </summary>
        public const uint MaximumFieldLength = 128;

        /// <summary>
        /// The name of the root wrapper node.
        /// </summary>
        public readonly string RootName;

        /// <summary>
        /// The name of the second wrapper node.
        /// </summary>
        public readonly string WrapName;

        /// <summary>
        /// The name of the list items.
        /// </summary>
        public readonly string ListItemName;

        /// <summary>
        /// The name of the list wrapper node.
        /// </summary>
        public readonly string ListWrapName;

        /// <summary>
        /// Indicates whether the input is parsed as a list.
        /// </summary>
        public readonly bool HasList;

        /// <summary>
        /// Indicates whether the input is parsed with a wrapper.
        /// </summary>
        public readonly bool HasWrap;

        /// <summary>
        /// Indicates, whether the list is wrapped in another node.
        /// </summary>
        public readonly bool HasWrappedList;

        private const string WRAP_REGEX = @"^\s*<{0}>\s*<{1}>\s*([\s\S]*)\s*<\/{1}>\s*<\/{0}>\s*$";
        private const string LIST_REGEX = @"^\s*<{0}>\s*([\s\S]*)\s*<\/{0}>\s*$";
        private const string LIST_WRAP_REGEX = @"^\s*<{0}>\s*<{1}>\s*([\s\S]*)\s*<{2}>\s*([\s\S]*)\s*<\/{2}>\s*([\s\S]*)\s*<\/{1}>\s*<\/{0}>\s*$";
        private const string LIST_ELEM_REGEX = @"<\s*{0}\s*>\s*([\s\S]*?)\s*<\s*\/\s*{0}\s*>";
        private const string ROOT_ELEM_REGEX = @"<\s*([\s\S]*?)\s*(?:\/\s*>|>([\s\S]*?)<\s*\/\1\s*>)";

        // The precompiled Regexes
        private readonly Regex mainRegex;
        private readonly Regex listElementRegex;
        private readonly Regex nodeElementRegex;

        // TODO!
        // Redo the parser regexes to:
        // 1) Allow either a wrapper or a list or both
        // 2) If both, a wrapper and a list are used, parse the wrapper first, as usual, then do the list, then the inner items
        // 3) If a list is parsed, always concatenate the input before and after the list to get the non-list root items (change regex to do this!)
        // 4) Fix all the errors
        // 5) Test the new parser

        /// <summary>
        /// Default public constructor.
        /// </summary>
        /// <param name="RootName">The name of the root wrapper node.</param>
        /// <param name="WrapOrListName">The name of the second wrapper node or the list wrapper node.</param>
        /// <param name="IsList">Indicates whether the input is parsed as a list.</param>
        public WADMParser(string RootName, string WrapOrListName, bool IsList)
        {
            // Sanity check input
            if(string.IsNullOrWhiteSpace(RootName))
                throw new ArgumentNullException("RootName");
            if (string.IsNullOrWhiteSpace(WrapOrListName))
                throw new ArgumentNullException("WrapOrListName");

            // Initialize the state
            this.RootName = RootName.Trim();
            if (this.HasList = IsList)
                this.ListItemName = WrapOrListName.Trim();
            else
                this.WrapName = WrapOrListName.Trim();
            this.HasWrap = !IsList;
            this.HasWrappedList = false;

            // Initialize the Regexes
            // First, the element regex used to match each XML value element
            nodeElementRegex = new Regex(ROOT_ELEM_REGEX, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            
            // Then, initialize either Element-wrap or the List Regexes
            if (IsList)
            {
                // This Regex matches only the root element of our list
                mainRegex = new Regex(string.Format(LIST_REGEX, this.RootName),
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);
                // This Regex matches each list item
                listElementRegex = new Regex(string.Format(LIST_ELEM_REGEX, this.ListItemName),
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }
            else // This Regex matches the root and wrap of our elements
                mainRegex = new Regex(string.Format(WRAP_REGEX, this.RootName, this.WrapName),
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Public constructor for double-wrapped lists.
        /// </summary>
        /// <param name="RootName">The name of the root wrapper node.</param>
        /// <param name="WrapName">The name of the second wrapper node.</param>
        /// <param name="ListWrapName">The name of the list wrapper node.</param>
        /// <param name="ListItemName">The name of the list items.</param>
        public WADMParser(string RootName, string WrapName, string ListWrapName, string ListItemName)
        {
            // Sanity check input
            if (string.IsNullOrWhiteSpace(RootName))
                throw new ArgumentNullException("RootName");
            if (string.IsNullOrWhiteSpace(WrapName))
                throw new ArgumentNullException("WrapName");
            if (string.IsNullOrWhiteSpace(ListWrapName))
                throw new ArgumentNullException("ListWrapName");
            if (string.IsNullOrWhiteSpace(ListItemName))
                throw new ArgumentNullException("ListItemName");

            // Initialize the state
            this.RootName = RootName.Trim();
            this.WrapName = WrapName.Trim();
            this.ListWrapName = ListWrapName.Trim();
            this.ListItemName = ListItemName.Trim();
            this.HasList = true;
            this.HasWrap = true;
            this.HasWrappedList = true;

            // Initialize the Regexes
            // First, the element regex used to match each XML value element
            nodeElementRegex = new Regex(ROOT_ELEM_REGEX, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            
            // Then, initialize the list element regex
            listElementRegex = new Regex(string.Format(LIST_ELEM_REGEX, this.ListItemName),
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);

            // Finally, initialize the wrapped list regex
            mainRegex = new Regex(string.Format(LIST_WRAP_REGEX, RootName, WrapName, ListWrapName),
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Parses an input string and returns the result of the process.
        /// </summary>
        /// <param name="Input">The input string received from the stereo.</param>
        /// <param name="LooseSyntax">Indicates whether to ignore certain minor syntax errors. Setting this to false will abort on every error.</param>
        /// <returns>A result object. If the parsing succeeded, the resulting object is also returned.</returns>
        public Result<WADMProduct> Parse(string Input, bool LooseSyntax = false)
        {
            // Allocate the result
            Result<WADMProduct> result = new Result<WADMProduct>();

            // Input sanity check
            if (string.IsNullOrWhiteSpace(Input))
                return result.FailMessage("The input may not be null, empty or white-space only!");

            // Check, if the input perhaps signals an error
            if (Input.ToLower().Trim() == "<pclinkinvalidcommand/>")
                return result.FailMessage("The request failed with an invalid command message!");
            
            // Match the input against our root level Regex
            // This is used to verify that the reply is correct and it will strip away the root wrapper
            Match rootMatch = mainRegex.Match(Input);

            // Allocate space for our product
            WADMProduct product;

            // And allocate the inner text variables
            string innerNodes = string.Empty, innerList = string.Empty;

            // Make sure we've got success and the correct number of matching groups
            if (!rootMatch.Success)
                return result.FailMessage("The root structure did not match!");
            if (HasList && HasWrap && HasWrappedList)
            {
                if (rootMatch.Groups.Count != 4)
                    return result.FailMessage("The root group count was incorrect (should be 4)!");
                if (!rootMatch.Groups[1].Success || !rootMatch.Groups[2].Success || !rootMatch.Groups[3].Success)
                    return result.FailMessage("One of the root content groups did not succeed!");
                // Create the product
                product = new WADMProduct(RootName, WrapName, ListWrapName, ListItemName);

                // Concatenate the upper and lower half of the root node elements
                innerNodes = rootMatch.Groups[1].Value.Trim() + rootMatch.Groups[3].Value.Trim();
                innerList = rootMatch.Groups[2].Value.Trim();
            }
            else
            {
                if (rootMatch.Groups.Count != 2)
                    return result.FailMessage("The root group count was incorrect (should be 2)!");
                if (!rootMatch.Groups[1].Success)
                    return result.FailMessage("The root content group did not succeed!");
                if (HasList && !HasWrap && !HasWrappedList)
                {
                    // Create the product
                    product = new WADMProduct(RootName, ListItemName, HasList);
                    
                    // Store the inner list data
                    innerList = rootMatch.Groups[1].Value.Trim();
                }
                else if (!HasList && HasWrap && !HasWrappedList)
                {
                    // Create the product
                    product = new WADMProduct(RootName, WrapName, HasList);

                    // Store the inner element data
                    innerNodes = rootMatch.Groups[1].Value.Trim();
                }
                else
                    return result.FailMessage("The type flags were invalid (not list, not wrap)!");
            }

            // Allocate a string for potentional parser error handling
            string parserError;

            // Inner text should later contain only non-list, elements
            if (HasList)
            {
                // Allocate space for our list
                List<Dictionary<string, string>> list = new List<Dictionary<string, string>>();

                // Match all list elements
                MatchCollection listMatches = listElementRegex.Matches(innerList);

                // Loop through the list elements
                foreach (Match listMatch in listMatches)
                {
                    // Make sure our match succeeded
                    if (!listMatch.Success)
                        if (LooseSyntax)
                            continue;
                        else
                            return result.FailMessage("The list item did not match successfully in strict mode!");

                    // Also make sure we've got the right number of groups
                    if (listMatch.Groups.Count != 2)
                        if (LooseSyntax)
                            continue;
                        else
                            return result.FailMessage("The number of matched list item groups was incorrect in strict mode!");

                    if(listMatch.Groups[1].Value == null)
                        if (LooseSyntax)
                            continue;
                        else
                            return result.FailMessage("The value of the matched list item group was null in strict mode!");

                    // If everything's right, we will parse the inner elements
                    Dictionary<string, string> innerElements = ParseElements(listMatch.Groups[1].Value, out parserError, LooseSyntax);
                    if (innerElements == null)
                        return result.FailMessage(parserError);

                    // Finally, we will add the element to our list
                    // Yes, we will even add empty elements, as they were potentionally intentionally left blank
                    list.Add(innerElements);
                }

                // Now, we remove the successful elements from further processing (if not already done)
                if (HasList && !HasWrap && !HasWrappedList)
                    innerNodes = listElementRegex.Replace(innerList, string.Empty);

                // And finally, we will add the list to the result
                product.List = list;
            }

            // Parse the root level key-value elements
            Dictionary<string, string> elements = ParseElements(innerNodes, out parserError, LooseSyntax);
            if (elements == null)
                return result.FailMessage(parserError);

            // And store them in the product
            product.Elements = elements;

            // Finally, successfully return the result
            return result.Succeed(product);
        }

        /// <summary>
        /// Internal function for parsing the elements.
        /// </summary>
        /// <param name="Input">The input to be parsed.</param>
        /// <param name="Error">Outputs an error string if the process failed.</param>
        /// <param name="LooseSyntax">Indicates whether to ignore minor syntax errors.</param>
        /// <returns>A dictionary of nodes found. Key: XML node name. Value: XML node value.</returns>
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
            MatchCollection matches = nodeElementRegex.Matches(Input);
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

                // Replace some escape sequences in the fields
                key = DecodeValue(key);
                value = DecodeValue(value);

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

        /// <summary>
        /// This function sanitizes and encodes all required special XML characters in a string.
        /// </summary>
        /// <param name="Value">The literal string.</param>
        /// <returns>The XML escaped string.</returns>
        public static string EncodeValue(string Value)
        {
            // Sanity check
            if (Value == null)
                return null;
            
            // Escape the ampersand first!
            Value = Value.Replace("&", "&amp;");

            // Replace the other literals with the escapes
            Value = Value.Replace("'", "&apos;");
            Value = Value.Replace("\"", "&quot;");
            Value = Value.Replace("<", "&lt;");
            Value = Value.Replace(">", "&gt;");

            // Return the result
            return Value;
        }

        /// <summary>
        /// This function decodes an XML escaped string to it's literal form.
        /// </summary>
        /// <param name="Value">The XML escaped string to decode.</param>
        /// <returns>The literal string.</returns>
        public static string DecodeValue(string Value)
        {
            // Sanity check
            if (Value == null)
                return null;

            // Replace the other escapes with the literals
            Value = Value.Replace("&apos;", "'");
            Value = Value.Replace("&quot;", "\"");
            Value = Value.Replace("&lt;", "<");
            Value = Value.Replace("&gt;", ">");

            // Replace the ampersand last!
            Value = Value.Replace("&amp;", "&");

            // Return the result
            return Value;
        }

        /// <summary>
        /// This function trims a value to the maximum supported length and puts an ellipsis at the end.
        /// XML encoded string will be trimmed encoding aware (i.e. no XML entities are butchered).
        /// </summary>
        /// <param name="Value">The value to trim.</param>
        /// <param name="IsEncoded">Set to true, if the value is XML encoded.</param>
        /// <returns>The trimmed value.</returns>
        public static string TrimValue(string Value, bool IsEncoded)
        {
            // Sanity check
            if (Value == null)
                return null;

            // Trim any whitespace
            Value = Value.Trim();

            // Check, if the maximum length is exceeded
            if (Value.Length <= MaximumFieldLength)
                return Value;

            // If the value is encoded, decode it before shortening
            if (IsEncoded)
                Value = DecodeValue(Value);

            // Trim the string
            Value = Value.Substring(0, MaximumFieldLength > 3 ? (int)MaximumFieldLength - 3 : 0);
            Value += "...";

            // Return the result
            return (IsEncoded ? EncodeValue(Value) : Value);
        }
    }
}
