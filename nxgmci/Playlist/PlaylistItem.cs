using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Playlist
{
    public class PlaylistItem
    {
        public string Title;
        public string Artist;
        public string Path;
        public long Duration;

        public PlaylistItem(string Title, string Artist, string Path, long Duration = -1)
        {
            this.Title = Title;
            this.Artist = Artist;
            this.Path = Path;
            this.Duration = Duration;
        }

        public override string ToString()
        {
            if (Title == null)
                Title = string.Empty;
            if (Artist == null)
                Artist = string.Empty;
            if (Path == string.Empty)
                return null;
            return string.Format("#EXTINF:{0},{1}{2}{3}{4}{5}{4}",
                (Duration < -1) ? -1 : Duration, Artist.Trim(),
                string.IsNullOrWhiteSpace(Artist) ? string.Empty : " - ",
                Title.Trim(), Environment.NewLine, Path);
        }
    }
}
