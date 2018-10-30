using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Playlist
{
    public class PlaylistItem
    {
        public string Title;
        public string Path;
        public long Duration;

        public PlaylistItem(string Title, string Path, long Duration = -1)
        {
            this.Title = Title;
            this.Path = Path;
            this.Duration = Duration;
        }

        public override string ToString()
        {
            if (Title == null)
                Title = string.Empty;
            if (Path == string.Empty)
                return null;
            return string.Format("#EXTINF:{0},{1}{2}{3}{2}",
                (Duration < -1) ? -1 : Duration, Title.Trim(), Environment.NewLine, Path);
        }
    }
}
