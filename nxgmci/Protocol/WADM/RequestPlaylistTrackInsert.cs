using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// This API call allows adding tracks to playlists, as well as changing the order of tracks in playlists.
    /// </summary>
    public static class RequestPlaylistTrackInsert
    {
        // RequestPlaylistTrackInsert Parser
        private readonly static WADMParser parser = new WADMParser("requestplaylisttrackinsert", "responseparameters", false);

        /// <summary>
        /// Assembles a RequestPlaylistTrackInsert request for adding tracks to playlists to be passed to the stereo.
        /// </summary>
        /// <param name="UpdateID">The modification update ID passed as a token.</param>
        /// <param name="TargetIndex">The index of the playlist to insert the track into.</param>
        /// <param name="SourceIndex">The index of the track to insert into the playlist.</param>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string BuildAdd(uint UpdateID, uint TargetIndex, uint SourceIndex)
        {
            // Sanity check the input
            if (SourceIndex == 0 || SourceIndex == TargetIndex)
                return null;

            // And build the request
            return string.Format(
                "<requestplaylistdelete><requestparameters>" +
                "<updateid>{0}</updateid>" +
                "<targetindex>{1}</targetindex>" + // The target ID uses Playlist child nodes of Playlist - as expected
                "<sourceindex>{2}</sourceindex>" + // The source ID uses the All-track -> Track namespace. TODO: Check, if others may be used.
                "<offset>-1</offset>" + // Perhaps, offset will allow you to define an order rightaway - it's usually always set to -1 though.
                "<movetrack>0</movetrack>" +
                "</requestparameters></requestplaylistdelete>",
                UpdateID,
                TargetIndex,
                SourceIndex);
        }

        /// <summary>
        /// Assembles a RequestPlaylistTrackInsert request for moving playlist items to be passed to the stereo.
        /// </summary>
        /// <param name="UpdateID">The modification update ID passed as a token.</param>
        /// <param name="TargetIndex">The the parent playlist ID of the item to be moved.</param>
        /// <param name="SourceIndex">The ID of the item to be moved inside the parent's namespace.</param>
        /// <param name="Offset">Unknown offset. Might be offset from the top.</param>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string BuildMove(uint UpdateID, uint TargetIndex, uint SourceIndex, uint Offset)
        {
            // Sanity check the input
            if (SourceIndex == 0 || SourceIndex == TargetIndex)
                return null;

            // And build the request
            return string.Format(
                "<requestplaylistdelete><requestparameters>" +
                "<updateid>{0}</updateid>" +
                "<targetindex>{1}</targetindex>" + // The target ID uses Playlist child nodes of Playlist. The parent of the item to be moved.
                "<sourceindex>{2}</sourceindex>" + // The source ID uses Track child-nodes of Playlist node - TODO: Check, if others may be used.
                "<offset>{3}</offset>" + // TOOD: Figure out how this works
                "<movetrack>1</movetrack>" +
                "</requestparameters></requestplaylistdelete>",
                UpdateID,
                TargetIndex,
                SourceIndex,
                Offset); // TODO: Check, if offset can be negative and how it works
        }
    }
}
