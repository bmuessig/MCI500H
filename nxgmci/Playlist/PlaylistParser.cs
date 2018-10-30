using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace nxgmci.Playlist
{
    public static class PlaylistParser
    {
        private static readonly Regex infoRegex = new Regex(@"^\s*#EXTINF\s*:\s*(-?\d+)\s*,\s*(.*)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static List<PlaylistItem> Parse(string Input, bool SkipEverythingButURLs = false, bool SkipEverythingButHTTP = false)
        {
            // First, normalize the line endings to unix linefeed
            Input = Input.Replace("\r\n", "\n").Replace("\r", "\n");
            
            // Split the lines into an array of strings
            string[] lines = Input.Split('\n');
            
            // Keep a marker whether or not we use extended M3U, whether it's the first line or we have meta
            bool isExtended = false, firstLine = true, haveMeta = false;
            
            // We also keep the last title and duration encountered
            string lastTitle = string.Empty;
            long lastDuration = -1;

            // And we allocate a list for our playlist entries
            List<PlaylistItem> items = new List<PlaylistItem>();

            // Loop through the lines
            foreach (string line in lines)
            {
                // Skip the line if it is empty
                if (string.IsNullOrWhiteSpace(line))
                {
                    // And reset our meta marker on these blank lines
                    haveMeta = false;
                    continue;
                }

                // Check, if this was the first line
                if (firstLine)
                {
                    // If this is true, reset the first line marker
                    firstLine = false;

                    // Also, check if the line is the extended header
                    if (line.Trim().ToUpper() == "#EXTM3U")
                    {
                        // If so, set the marker
                        isExtended = true;
                        
                        // And proceed, since the line is handled
                        continue;
                    }
                }

                // Now, check if the line is a normal comment and if true, skip it
                if (line.TrimStart().StartsWith("#"))
                {
                    // If we don't use the extended syntax, every line starting with a # is a comment
                    if (!isExtended)
                        continue;

                    // Attempt to match the line using the info regex
                    Match infoMatch = infoRegex.Match(line);
                    
                    // We have just encountered extended info and attempt to parse it
                    // If parsing fails, it's probably just a regular comment, so we skip it
                    if (!infoMatch.Success)
                        continue;
                    if (infoMatch.Groups.Count != 3)
                        continue;

                    // If we succeed parsing it, store title and duration
                    if (!long.TryParse(infoMatch.Groups[1].Value, out lastDuration))
                        lastDuration = -1;
                    lastTitle = infoMatch.Groups[2].Value.Trim();
                    haveMeta = true;

                    // Since we have handled that line, proceed to the next one
                    continue;
                }

                // If we end up here, it's neither a comment, nor a directive, so it must be a playlist file
                if(SkipEverythingButURLs)
                    if (SkipEverythingButHTTP)
                    {
                        if (!line.TrimStart().ToLower().StartsWith("http://"))
                        {
                            // If we only allow HTTP URLs, skip everything else
                            haveMeta = false;
                            continue;
                        }
                    }
                    else
                    {
                        if (!line.Contains("://"))
                        {
                            // If we only allow URLs, but the URL characteristic is not present, skip the path
                            haveMeta = false;
                            continue;
                        }
                    }

                // If we end up here, we should be able to add the file to the playlist
                // Note, that this does not properly check paths or URLs
                // It will only do some crude checking to filter out most bad ones
                // So, it's advised to add some more checking code here later (TODO)

                // Finally, add the new playlist item
                // If we have additional info, add it
                if (haveMeta)
                {
                    items.Add(new PlaylistItem(lastTitle, line.Trim(), lastDuration));
                    haveMeta = false;
                }
                else
                    items.Add(new PlaylistItem(string.Empty, line.Trim()));
            }

            // And finally, return the completed list
            return items;
        }

        public static string Compile(IEnumerable<PlaylistItem> Playlist)
        {
            // Initialize the output string builder
            StringBuilder outputBuilder = new StringBuilder("#EXTM3U");
            outputBuilder.Append(Environment.NewLine);

            // Loop through the playlist items and write them
            foreach (PlaylistItem item in Playlist)
            {
                if (item == null)
                    continue;
                string line = item.ToString();
                if (line == null)
                    continue;
                outputBuilder.Append(line);
                outputBuilder.Append(Environment.NewLine);
            }

            // Return the final string
            return outputBuilder.ToString();
        }
    }
}
