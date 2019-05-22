using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace nxgmci.Metadata.Playlist
{
    public static class PlaylistParser
    {
        private static readonly Regex infoRegex = new Regex(@"^\s*#EXTINF\s*:\s*(-?\d+)\s*,\s*(.*)(?:\s*-\s*(.*))?\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex infoNoArtistRegex = new Regex(@"^\s*#EXTINF\s*:\s*(-?\d+)\s*,\s*(.*)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static List<PlaylistItem> Parse(string Input,
            bool IgnoreBlankLines = false, bool DisableArtistInfo = false,
            bool SkipEverythingButURLs = false, bool SkipEverythingButHTTP = false)
        {
            // First, normalize the line endings to unix linefeed
            Input = Input.Replace("\r\n", "\n").Replace("\r", "\n");
            
            // Split the lines into an array of strings
            string[] lines = Input.Split('\n');
            
            // Keep a marker whether or not we use extended M3U, whether it's the first line or we have meta
            bool isExtended = false, firstLine = true, haveMeta = false;
            
            // We also keep the last title and duration encountered
            string lastTitle = string.Empty, lastArtist = string.Empty;
            long lastDuration = -1;

            // And we allocate a list for our playlist entries
            List<PlaylistItem> items = new List<PlaylistItem>();

            // Loop through the lines
            foreach (string line in lines)
            {
                // Skip the line if it is empty
                if (string.IsNullOrWhiteSpace(line))
                {
                    // And, if desired, reset our meta marker on these blank lines
                    // Windows Media Player prefers the full artist + title format
                    // VLC ignores blank lines and does both formats well
                    // Both support header-less entries
                    if (!IgnoreBlankLines)
                    {
                        haveMeta = false;
                        lastTitle = string.Empty;
                        lastArtist = string.Empty;
                        lastDuration = -1;
                    }
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

                    // Attempt to match the line using the info Regex
                    Match infoMatch;
                    if (DisableArtistInfo)
                        infoMatch = infoNoArtistRegex.Match(line);
                    else
                        infoMatch = infoRegex.Match(line);
                    
                    // We have just encountered extended info and attempt to parse it
                    // If parsing fails, it's probably just a regular comment, so we skip it
                    if (!infoMatch.Success)
                        continue;
                    if (infoMatch.Groups.Count < 3 || infoMatch.Groups.Count > 4)
                        continue;

                    // If we succeed parsing it, store title, artist and duration
                    if (!long.TryParse(infoMatch.Groups[1].Value, out lastDuration))
                        lastDuration = -1;

                    // Check, if the artist info is disabled and the Regex unified
                    if (DisableArtistInfo || string.IsNullOrWhiteSpace(infoMatch.Groups[4].Value))
                        lastTitle = infoMatch.Groups[2].Value.Trim();
                    else
                    {
                        // We have both, artist and title
                        lastTitle = infoMatch.Groups[3].Value.Trim();
                        lastArtist = infoMatch.Groups[2].Value.Trim();
                    }

                    // Set the meta marker
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
                            lastTitle = string.Empty;
                            lastArtist = string.Empty;
                            lastDuration = -1;
                            continue;
                        }
                    }
                    else
                    {
                        if (!line.Contains("://"))
                        {
                            // If we only allow URLs, but the URL characteristic is not present, skip the path
                            haveMeta = false;
                            lastTitle = string.Empty;
                            lastArtist = string.Empty;
                            lastDuration = -1;
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
                    items.Add(new PlaylistItem(lastTitle, lastArtist, line.Trim(), lastDuration));
                    haveMeta = false;
                    lastTitle = string.Empty;
                    lastArtist = string.Empty;
                    lastDuration = -1;
                }
                else
                    items.Add(new PlaylistItem(string.Empty, string.Empty, line.Trim()));
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
