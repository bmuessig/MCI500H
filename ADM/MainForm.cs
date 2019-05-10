using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using nxgmci;
using System.Net;
using System.IO;
using nxgmci.Network;
using nxgmci.Protocol;
using nxgmci.Cover;
using nxgmci.Metadata;
using nxgmci.Protocol.WADM;
using nxgmci.Device;

namespace ADM
{
    public partial class MainForm : Form
    {
        private static IPAddress ip = new IPAddress(new byte[] { 10, 0, 0, 10 });
        //private static IPAddress ip = new IPAddress(new byte[] { 192, 168, 10, 3 });

        private MCI500H stereo = new MCI500H(ip);
        int lastid = -1;
        string lastResponse = "";
        bool lastSuccess = false;

        WADMClient client = new WADMClient(new EndpointDescriptor(ip));
        WebClient web = new WebClient();

        public MainForm()
        {
            InitializeComponent();
            CoverCrypt.CalculateCryptoKey();
        }

        private void speedTestButton_Click(object sender, EventArgs e)
        {
            string url = string.Format("http://{0}:8081/", ip.ToString());
            string query = "<requesturimetadata></requesturimetadata>";
            string internalResponse, externalResponse;

            DateTime internalStart = DateTime.Now;

            WebClient client = new WebClient();
            try
            {
                internalResponse = client.UploadString(url, query);
            }
            catch (Exception ex)
            {
                internalResponse = "An error occured: " + ex.Message;
            }

            DateTime externalStart = DateTime.Now;

            Postmaster.QueryResponse response = Postmaster.PostXML(new Uri(url), query);
            if (response.Success)
                externalResponse = response.TextualResponse;
            else
                externalResponse = "An error occured: " + response.Message;

            DateTime testEnd = DateTime.Now;

            MessageBox.Show(string.Format("Test results:\n\nMicrosoft Implementation: {0} ms\nOwn Implementation: {1} ms",
                (externalStart - internalStart).TotalMilliseconds,
                (testEnd - externalStart).TotalMilliseconds));

            MessageBox.Show(string.Format("Microsoft Implementation Reply:\n\n{0}", internalResponse));

            MessageBox.Show(string.Format("Own Implementation Reply:\n\n{0}", externalResponse));
        }

        private void mediaPlayButton_Click(object sender, EventArgs e)
        {
            if (mediaView.SelectedIndices.Count == 1)
                if (lastid != mediaView.SelectedIndices[0])
                {
                    lastid = mediaView.SelectedIndices[0];
                    selectLast();
                }

            if (!stereo.Play())
                MessageBox.Show("Fail!");
        }

        private string TMP_media_from_id(RequestRawData.ContentData entry)
        {
            string tid = (entry.NodeID & currentUriMetaData.IDMask).ToString();
            string ext;
            if (!currentUriMetaData.MediaTypeKey.ContainsKey(entry.MediaType))
                return null;
            string container = tid.Substring(0, ((int)Math.Log10(currentUriMetaData.ContainerSize)) - 1);
            ext = currentUriMetaData.MediaTypeKey[entry.MediaType];
            return string.Format("{0}/{1}/{2}.{3}", currentUriMetaData.URIPath, container, tid, ext);
        }

        private void transmitButton_Click(object sender, EventArgs e)
        {
            receiveTextBox.Clear();
            transmitTextBox.ReadOnly = true;
            Postmaster.QueryResponse response = Postmaster.PostXML(new Uri(string.Format("http://{0}:8081/", ip.ToString())), transmitTextBox.Text);
            receiveTextBox.Text = response.Success ? string.Format("HTTP {0} {1}:\n\n{2}", response.StatusCode, response.Message, response.TextualResponse).Replace("\n", "\r\n")
                : "An error occured: " + response.Message;
            lastSuccess = response.Success;
            lastResponse = response.TextualResponse;
            transmitTextBox.ReadOnly = false;
        }

        private void transmitClearButton_Click(object sender, EventArgs e)
        {
            transmitTextBox.Clear();
            transmitTextBox.Focus();
        }

        private void topMostCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = topMostCheckBox.Checked;
        }

        RequestRawData.ContentDataSet currentMediaLib;
        RequestAlbumIndexTable.ContentDataSet currentAlbumIndex;
        RequestArtistIndexTable.ContentDataSet currentArtistIndex;
        RequestGenreIndexTable.ContentDataSet currentGenreIndex;
        RequestUriMetaData.ResponseParameters currentUriMetaData;

        private void mediaFetchButton_Click(object sender, EventArgs e)
        {
            // Fetch media
            Result<RequestRawData.ContentDataSet> contentResp = client.RequestRawData(0, 0);
            if (!contentResp.Success)
            {
                MessageBox.Show(contentResp.ToString());
                return;
            }

            // Fetch artists
            Result<RequestArtistIndexTable.ContentDataSet> artistResp = client.RequestArtistIndexTable();
            if (!artistResp.Success)
            {
                MessageBox.Show(artistResp.ToString());
                return;
            }

            // Fetch albums
            Result<RequestAlbumIndexTable.ContentDataSet> albumResp = client.RequestAlbumIndexTable();
            if (!albumResp.Success)
            {
                MessageBox.Show(albumResp.ToString());
                return;
            }

            // Fetch genres
            Result<RequestGenreIndexTable.ContentDataSet> genreResp = client.RequestGenreIndexTable();
            if (!genreResp.Success)
            {
                MessageBox.Show(genreResp.ToString());
                return;
            }

            // Fetch urimetadata
            Result<RequestUriMetaData.ResponseParameters> metaDataResp = client.RequestUriMetaData();
            if (!metaDataResp.Success)
            {
                MessageBox.Show(metaDataResp.ToString());
                return;
            }

            currentMediaLib = contentResp.Product;
            currentArtistIndex = artistResp.Product;
            currentAlbumIndex = albumResp.Product;
            currentGenreIndex = genreResp.Product;
            currentUriMetaData = metaDataResp.Product;
            updateLib();
        }

        private void selectLast(bool startPlay = false)
        {
            stereo.Stop();
            if (!stereo.SelectMedia(new Uri(TMP_media_from_id(currentMediaLib.ContentData[lastid]))))
                MessageBox.Show("Fail!");
            if (!startPlay)
                return;
            if (!stereo.Play())
                MessageBox.Show("Fail!");
        }

        private void updateLib()
        {
            if (currentMediaLib == null)
                return;
            mediaView.Items.Clear();
            foreach (var contentData in currentMediaLib.ContentData)
            {
                string title = contentData.Name.Trim(), artist, album;

                RequestArtistIndexTable.ContentData artistEntry =
                    currentArtistIndex.GetEntry(contentData.Artist);
                if (artistEntry == null)
                    artist = contentData.Artist.ToString();
                else
                    artist = artistEntry.Name;

                RequestAlbumIndexTable.ContentData albumEntry =
                    currentAlbumIndex.GetEntry(contentData.Album);
                if (albumEntry == null)
                    album = contentData.Album.ToString();
                else
                    album = albumEntry.Name;

                mediaView.Items.Add(new ListViewItem(new string[] {
                    title, artist, album
                }));
            }
        }

        private void queryDiskSpaceButton_Click(object sender, EventArgs e)
        {
            Result<QueryDiskSpace.ResponseParameters> result = client.QueryDiskSpace();

            if (!result.Success)
            {
                MessageBox.Show(result.ToString());
                return;
            }

            MessageBox.Show(string.Format("Free disk space: {0} GB / {1} GB ({2}%)\n\nTook {3}ms for request, response and parsing.",
                result.Product.Size / 1024 / 1024 / 1024,
                result.Product.TotalSize / 1024 / 1024 / 1024,
                (result.Product.Size * 100) / result.Product.TotalSize,
                result.TimeDelta.TotalMilliseconds));
        }

        private void mediaPauseButton_Click(object sender, EventArgs e)
        {
            if (!stereo.Pause())
                MessageBox.Show("Fail!");
        }

        private void mediaStopButton_Click(object sender, EventArgs e)
        {
            if (!stereo.Stop())
                MessageBox.Show("Fail!");
        }

        private void mediaSkipBackButton_Click(object sender, EventArgs e)
        {
            if (currentMediaLib == null)
                return;
            if (lastid > 0)
                lastid--;
            else
                return;
            selectLast(true);
        }

        private void mediaSkipAheadButton_Click(object sender, EventArgs e)
        {
            if (currentMediaLib == null)
                return;
            if (lastid + 1 < currentMediaLib.ContentData.Length)
                lastid++;
            else
                return;
            selectLast(true);
        }

        private void playUriButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(playUriTextBox.Text))
                return;
            
            string url = playUriTextBox.Text.Trim();

            if (url.ToLower().EndsWith(".m3u"))
            {
                string playlistConts;
                if(url.ToLower().StartsWith("http://") || url.ToLower().StartsWith("https://"))
                    using (WebClient web = new WebClient())
                    {
                        try
                        {
                            playlistConts = web.DownloadString(url);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Failed downloading:\n" + ex.Message);
                            return;
                        }
                    }
                else if (File.Exists(url))
                    try
                    {
                        playlistConts = File.ReadAllText(url);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed reading the file:\n" + ex.Message);
                        return;
                    }
                else
                {
                    MessageBox.Show("Can't make heads or tails of the entered string!");
                    return;
                }

                List<PlaylistItem> listItems = PlaylistParser.Parse(playlistConts, true, false, true, true);
                if (listItems == null)
                {
                    MessageBox.Show("Fail parsing list!");
                    return;
                }

                if (listItems.Count < 1)
                {
                    MessageBox.Show("The playlist was empty!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(listItems[0].Path))
                {
                    MessageBox.Show("First playlist item does not contain a path!");
                    return;
                }

                // We only play the first element if there is one
                url = listItems[0].Path;
            }
            /*else if (!url.ToLower().EndsWith(".mp3"))
            {
                MessageBox.Show("Invalid format!");
                return;
            }*/

            stereo.Stop();
            if (!stereo.SelectMedia(new Uri(url)))
            {
                MessageBox.Show("Fail!");
                return;
            }

            if (!stereo.Play())
                MessageBox.Show("Fail!");
        }

        private void viewIDButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show(string.Format("Current Update ID: {0}", client.UpdateID));
        }

        private void transmitTryParseButton_Click(object sender, EventArgs e)
        {
            if(!lastSuccess)
            {
                MessageBox.Show("The previous request did not complete successfully. Parsing cannot be performed!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }


            WADMParser parser = new WADMParser("contentdataset", "contentdata", true);
            // WADMParser parser = new WADMParser("requestlikenodes", "responseparameters", "hiddenplaylistnodes", "hiddenplaylist");
            // <requestlikenodes><requestparameters><index>402683145</index></requestparameters></requestlikenodes>
            Result<WADMProduct> result = parser.Parse(lastResponse, true);
            if (result.Success)
            {
                if (MessageBox.Show(string.Format("The parsing succeeded after {0}ms!\n{1} root elements and {2} list elements were found.\n\nDo you want to break the program to see the list?",
                    (int)result.TimeDelta.TotalMilliseconds, result.Product.Elements.Count, result.Product.HadList ? result.Product.List.Count : 0), "Success!",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                    throw null; // Break the program
            }
            else
                MessageBox.Show(string.Format("The parsing FAILED after {0}ms!\n\n{1}", (int)result.TimeDelta.TotalMilliseconds, result.ToString(true, false, true)), "Error!",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        uint currentNodeID = 0;
        string currentNodeName = "Root";
        Stack<KeyValuePair<uint, string>> nodeStack = new Stack<KeyValuePair<uint, string>>();
        RequestPlayableData.ContentDataSet currentDataSet;
        Bitmap coverImage;

        private void treeUpButton_Click(object sender, EventArgs e)
        {
            treeUpButton.Enabled = false;
            GoUp();
            treeUpButton.Enabled = true;
        }

        private void treeResetButton_Click(object sender, EventArgs e)
        {
            treeResetButton.Enabled = false;
            currentNodeID = 0;
            currentNodeName = "Root";
            nodeStack.Clear();
            treeStackListBox.Items.Clear();
            treeItemsListBox.Items.Clear();
            treeInfoTextBox.Clear();
            UpdateBrowser();
            treeResetButton.Enabled = true;
        }

        private void GoUp()
        {
            if (nodeStack.Count < 1)
                return;
            KeyValuePair<uint, string> currentLevel = nodeStack.Pop();
            currentNodeID = currentLevel.Key;
            currentNodeName = currentLevel.Value;
            UpdateStack();
            UpdateBrowser();
        }

        private void GoNodeID(uint NodeID, string NodeName)
        {
            if (currentNodeID == NodeID)
                return;
            nodeStack.Push(new KeyValuePair<uint, string>(currentNodeID, currentNodeName));
            UpdateStack();
            currentNodeID = NodeID;
            currentNodeName = NodeName;
            UpdateBrowser();
        }

        private void UpdateStack()
        {
            treeStackListBox.Items.Clear();
            foreach (KeyValuePair<uint, string> nodePair in nodeStack)
                treeStackListBox.Items.Add(string.Format("{0} (${1})", nodePair.Value, nodePair.Key.ToString("X8")));
        }

        private void UpdateBrowser()
        {
            Result<RequestPlayableData.ContentDataSet> result = client.RequestPlayableData(currentNodeID);
            if (!result.Success)
            {
                MessageBox.Show(string.Format("The process failed:\n\n{0}", result.ToString()), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (result.Product.Failed)
            {
                MessageBox.Show("The process failed, as the ID was invalid!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            currentDataSet = result.Product;
            UpdateBrowserList();
        }

        private void UpdateBrowserList()
        {
            if (currentDataSet == null)
                return;

            treeItemsListBox.Items.Clear();

            foreach (RequestPlayableData.ContentData data in currentDataSet.ContentData)
            {
                treeItemsListBox.Items.Add(string.Format("{0} (${1}, {2})",
                    data.Name, data.NodeID.ToString("X8"), data.NodeType));
            }
        }

        private void PlayNode(RequestPlayableData.ContentDataPlayable Node)
        {
            if (Node == null)
                return;
            stereo.Stop();
            stereo.SelectMedia(new Uri(Node.URL));
            stereo.Play();

            titleLabel.Text = Node.Title;
            artistLabel.Text = Node.Artist;
            albumLabel.Text = Node.Album;
            genreLabel.Text = Node.Genre;
            idLabel.Text = string.Format("${0}", Node.NodeID.ToString("X4"));

            if (Node is RequestPlayableData.ContentDataPlayableArt)
            {
                RequestPlayableData.ContentDataPlayableArt artNode = (RequestPlayableData.ContentDataPlayableArt)Node;
                string coverUrl = artNode.AlbumArtURL;

                try
                {
                    byte[] cover = web.DownloadData(coverUrl);
                    CoverCrypt.EncryptBuffer(ref cover, (uint)cover.Length);
                    MemoryStream ms = new MemoryStream(cover);
                    ms.Position = 0;
                    coverImage = new Bitmap(ms);
                    ms.Close();
                    coverPictureBox.Image = coverImage;
                }
                catch (Exception) { }
            }
        }

        private void treeGoButton_Click(object sender, EventArgs e)
        {
            if (treeItemsListBox.SelectedItems.Count != 1)
                return;
            treeGoButton.Enabled = false;
            RequestPlayableData.ContentData data = currentDataSet.ContentData[treeItemsListBox.SelectedIndex];
            if (data.NodeType != RequestPlayableData.NodeType.Branch)
            {
                treeGoButton.Enabled = true;
                MessageBox.Show("The process failed, as the node is not a branch!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            GoNodeID(data.NodeID, data.Name);
            treeGoButton.Enabled = true;
        }

        private void treePlayButton_Click(object sender, EventArgs e)
        {
            if (treeItemsListBox.SelectedItems.Count != 1)
                return;
            treePlayButton.Enabled = false;
            RequestPlayableData.ContentData data = currentDataSet.ContentData[treeItemsListBox.SelectedIndex];
            if (data.NodeType != RequestPlayableData.NodeType.Playable)
            {
                treePlayButton.Enabled = true;
                MessageBox.Show("The process failed, as the node is not playable!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (data is RequestPlayableData.ContentDataPlayable)
                PlayNode((RequestPlayableData.ContentDataPlayable)data);
            treePlayButton.Enabled = true;
        }
    }
}
