using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using nxgmci;
using nxgmci.Cover;
using nxgmci.Device;
using nxgmci.Metadata.Playlist;
using nxgmci.Network;
using nxgmci.Protocol.Delivery;
using nxgmci.Protocol.WADM;

namespace ADM
{
    public partial class MainForm : Form
    {
        private static IPAddress ip = new IPAddress(new byte[] { 10, 0, 0, 10 });
        //private static IPAddress ip = new IPAddress(new byte[] { 192, 168, 10, 3 });
        //private static IPAddress ip = new IPAddress(new byte[] { 172, 16, 1, 19 });

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

        private void mediaPanelPlayButton_Click(object sender, EventArgs e)
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

        private void mediaPlayButton_Click(object sender, EventArgs e)
        {
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
        RequestIndexTable.ContentDataSet currentAlbumIndex, currentArtistIndex, currentGenreIndex;
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
            Result<RequestIndexTable.ContentDataSet> artistResp = client.RequestArtistIndexTable();
            if (!artistResp.Success)
            {
                MessageBox.Show(artistResp.ToString());
                return;
            }

            // Fetch albums
            Result<RequestIndexTable.ContentDataSet> albumResp = client.RequestAlbumIndexTable();
            if (!albumResp.Success)
            {
                MessageBox.Show(albumResp.ToString());
                return;
            }

            // Fetch genres
            Result<RequestIndexTable.ContentDataSet> genreResp = client.RequestGenreIndexTable();
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

                RequestIndexTable.ContentData artistEntry =
                    currentArtistIndex.FindIndex(contentData.Artist);
                if (artistEntry == null)
                    artist = contentData.Artist.ToString();
                else
                    artist = artistEntry.Name;

                RequestIndexTable.ContentData albumEntry =
                    currentAlbumIndex.FindIndex(contentData.Album);
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
            if (string.IsNullOrWhiteSpace(playUriComboBox.Text))
                return;
            
            string url = playUriComboBox.Text.Trim();

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
        RequestPlayableNavData.ContentDataSet currentDataSet;
        Bitmap coverImage;

        private void treeUpButton_Click(object sender, EventArgs e)
        {
            treeUpButton.Enabled = false;
            Application.DoEvents();
            GoUp();
            treeUpButton.Enabled = true;
        }

        private void treeResetButton_Click(object sender, EventArgs e)
        {
            treeResetButton.Enabled = false;
            Application.DoEvents();
            currentNodeID = 0;
            currentNodeName = "Root";
            nodeStack.Clear();
            treeStackListBox.Items.Clear();
            treeItemsListBox.Items.Clear();
            treeInfoTextBox.Clear();
            UpdateBrowser();
            treeAddPlaylistNameTextBox.Clear();
            highlightedNode = null;
            ClearMark();
            SetCurrentNodeInfo(null);
            SetSelectedNodeInfo(null, true);
            treeResetButton.Enabled = true;

            if (treeResetButton.Text == "Start")
            {
                treeUpButton.Enabled = true;
                treeGoButton.Enabled = true;
                treeResetButton.Text = "Reset";
            }
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
            Result<RequestPlayableNavData.ContentDataSet> result = client.RequestPlayableData(currentNodeID);
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
            SetSelectedNodeInfo(null, true);
        }

        private void UpdateBrowserList()
        {
            if (currentDataSet == null)
                return;

            treeItemsListBox.Items.Clear();

            foreach (RequestPlayableNavData.ContentData data in currentDataSet.ContentData)
            {
                treeItemsListBox.Items.Add(string.Format("{0} (${1}, {2})",
                    data.Name, data.NodeID.ToString("X8"), data.NodeType));
            }
        }

        private void PlayNode(RequestPlayableNavData.ContentDataPlayable Node)
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

            if (Node is RequestPlayableNavData.ContentDataPlayableArt)
            {
                RequestPlayableNavData.ContentDataPlayableArt artNode = (RequestPlayableNavData.ContentDataPlayableArt)Node;
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

        string currentNodeText = "", selectedNodeText = "";

        private void SetCurrentNodeInfo(RequestPlayableNavData.ContentDataBranch Node, bool GeneratePanel = false)
        {
            if (Node == null)
                currentNodeText = "";
            else
                currentNodeText = string.Format(
                    "Current Node:\r\nName: {0} \tID: ${1}\tType: {2}",
                    Node.Name,
                    Node.NodeID.ToString("X4"),
                    Node.IconType.ToString());

            if (GeneratePanel)
                GenerateTreeInfoPanel();
        }

        private void SetSelectedNodeInfo(RequestPlayableNavData.ContentDataBranch Node, bool GeneratePanel = false)
        {
            if (Node == null)
                selectedNodeText = "";
            else
                selectedNodeText = string.Format(
                    "Selected Node:\r\nName: {0} \tID: ${1}\tType: {2}",
                    Node.Name,
                    Node.NodeID.ToString("X4"),
                    Node.IconType.ToString());

            if (GeneratePanel)
                GenerateTreeInfoPanel();
        }

        private void SetSelectedPlayableInfo(RequestPlayableNavData.ContentDataPlayable Node, bool GeneratePanel = false)
        {
            if (Node == null)
                selectedNodeText = "";
            else
                selectedNodeText = string.Format(
                    "Selected Track:\r\nName: {0} \tID: ${1}\r\nArtist: {2} \tAlbum: {3}\r\nGenre: {4} \tTrack#: {5} \tYear: {6}",
                    Node.Name,
                    Node.NodeID.ToString("X4"),
                    Node.Artist,
                    Node.Album,
                    Node.Genre,
                    Node.TrackNo,
                    Node.Year);

            if (GeneratePanel)
                GenerateTreeInfoPanel();
        }

        private void GenerateTreeInfoPanel()
        {
            if (string.IsNullOrWhiteSpace(selectedNodeText) && !string.IsNullOrWhiteSpace(currentNodeText))
                treeInfoTextBox.Text = currentNodeText;
            else if (!string.IsNullOrWhiteSpace(selectedNodeText) && string.IsNullOrWhiteSpace(currentNodeText))
                treeInfoTextBox.Text = selectedNodeText;
            else if (!string.IsNullOrWhiteSpace(selectedNodeText) && !string.IsNullOrWhiteSpace(currentNodeText))
                treeInfoTextBox.Text = string.Format("{0}\r\n\r\n{1}", currentNodeText, selectedNodeText);
            else
                treeInfoTextBox.Text = "";
            treeInfoTextBox.Select(0, 0);
        }

        private void treeGoButton_Click(object sender, EventArgs e)
        {
            if (treeItemsListBox.SelectedItems.Count != 1)
                return;
            treeGoButton.Enabled = false;
            Application.DoEvents();
            RequestPlayableNavData.ContentData data = currentDataSet.ContentData[treeItemsListBox.SelectedIndex];

            if (data.NodeType == RequestPlayableNavData.NodeType.Branch)
            {
                SetCurrentNodeInfo((RequestPlayableNavData.ContentDataBranch)currentDataSet.ContentData[treeItemsListBox.SelectedIndex]);
                SetSelectedNodeInfo(null, true);
                GoNodeID(data.NodeID, data.Name);
            }
            else if (data.NodeType == RequestPlayableNavData.NodeType.Playable)
                if (data is RequestPlayableNavData.ContentDataPlayable)
                    PlayNode((RequestPlayableNavData.ContentDataPlayable)data);

            treeGoButton.Enabled = true;
        }

        RequestPlayableNavData.ContentDataPlayable highlightedNode;

        private void treeItemsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (treeItemsListBox.SelectedItems.Count != 1)
            {
                highlightedNode = null;
                SetSelectedNodeInfo(null, true);
                return;
            }

            if (currentDataSet.ContentData[treeItemsListBox.SelectedIndex].NodeType == RequestPlayableNavData.NodeType.Playable)
            {
                highlightedNode = (RequestPlayableNavData.ContentDataPlayable)currentDataSet.ContentData[treeItemsListBox.SelectedIndex];
                SetSelectedPlayableInfo((RequestPlayableNavData.ContentDataPlayable)currentDataSet.ContentData[treeItemsListBox.SelectedIndex], true);
                playUriComboBox.Text = highlightedNode.URL;
                return;
            }
            else
            {
                SetSelectedNodeInfo((RequestPlayableNavData.ContentDataBranch)currentDataSet.ContentData[treeItemsListBox.SelectedIndex], true);
                highlightedNode = null;
            }
        }

        private void uploadGoButton_Click(object sender, EventArgs e)
        {
            uploadGoButton.Enabled = false;
            uploadProgressBar.Value = 0;
            Application.DoEvents();

            if (string.IsNullOrWhiteSpace(uploadTitleTextBox.Text))
            {
                MessageBox.Show("The title is empty!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                uploadGoButton.Enabled = true;
                uploadProgressBar.Value = 0;
                return;
            }

            bool askForReview = false;
            if (string.IsNullOrWhiteSpace(uploadArtistTextBox.Text))
            {
                uploadArtistTextBox.Text = "No Artist";
                askForReview = true;
            }
            if (string.IsNullOrWhiteSpace(uploadAlbumTextBox.Text))
            {
                uploadAlbumTextBox.Text = "No Album";
                askForReview = true;
            }
            if (string.IsNullOrWhiteSpace(uploadGenreTextBox.Text))
            {
                uploadGenreTextBox.Text = "No Genre";
                askForReview = true;
            }

            uint trackno = 0;
            if (string.IsNullOrWhiteSpace(uploadTracknoTextBox.Text))
            {
                uploadTracknoTextBox.Text = "0";
                askForReview = true;
            }
            else if (!uint.TryParse(uploadTracknoTextBox.Text, out trackno))
            {
                MessageBox.Show("Invalid trackno!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                uploadGoButton.Enabled = true;
                uploadProgressBar.Value = 0;
                return;
            }

            uint year = 0;
            if (string.IsNullOrWhiteSpace(uploadYearTextBox.Text))
            {
                uploadYearTextBox.Text = "0";
                askForReview = true;
            }
            else if (!uint.TryParse(uploadYearTextBox.Text, out year))
            {
                MessageBox.Show("Invalid year!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                uploadGoButton.Enabled = true;
                uploadProgressBar.Value = 0;
                return;
            }

            if (askForReview)
            {
                MessageBox.Show("You have left some fields blank. These have been automatically changed. Please review the changes!", "Notice!",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                uploadGoButton.Enabled = true;
                uploadProgressBar.Value = 0;
                return;
            }

            if (openUploadFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                uploadGoButton.Enabled = true;
                uploadProgressBar.Value = 0;
                return;
            }

            if (!File.Exists(openUploadFileDialog.FileName))
            {
                MessageBox.Show("The specified file does not exist!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                uploadGoButton.Enabled = true;
                uploadProgressBar.Value = 0;
                return;
            }

            string type = Path.GetExtension(openUploadFileDialog.FileName).Replace(".", "").ToLower();

            if (type != "mp3" && type != "wma" && type != "aac" && type != "wav")
            {
                MessageBox.Show("The specified file type is not supported!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                uploadGoButton.Enabled = true;
                uploadProgressBar.Value = 0;
                return;
            }

            byte[] mediaBytes;
            try
            {
                mediaBytes = File.ReadAllBytes(openUploadFileDialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error reading the media file:\n{0}", ex.Message), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                uploadGoButton.Enabled = true;
                uploadProgressBar.Value = 0;
                return;
            }

            if (!client.GetUpdateID().Success)
            {
                MessageBox.Show("Could not fetch the current update ID!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                uploadGoButton.Enabled = true;
                uploadProgressBar.Value = 0;
                return;
            }

            uploadProgressBar.Value = 25;
            Application.DoEvents();

            Result<RequestObjectCreate.ResponseParameters> result = client.RequestObjectCreate(
                uploadArtistTextBox.Text.Trim(), uploadAlbumTextBox.Text.Trim(), uploadGenreTextBox.Text.Trim(), uploadTitleTextBox.Text.Trim(),
                trackno, year, type, 0, 10);

            if (!result.Success)
            {
                MessageBox.Show(string.Format("The meta upload process failed:\n\n{0}", result.ToString()), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                uploadGoButton.Enabled = true;
                uploadProgressBar.Value = 0;
                return;
            }

            if (result.Product.Status.Status != WADMStatus.StatusCode.Success)
            {
                MessageBox.Show(string.Format("The meta upload process failed:\n\n{0}", result.ToString()), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                uploadGoButton.Enabled = true;
                uploadProgressBar.Value = 0;
                return;
            }

            uploadProgressBar.Value = 50;
            Application.DoEvents();

            Result putResult = Delivery.PutMediaWithoutArt(result.Product.ImportResource.EndPoint, result.Product.ImportResource.Path, mediaBytes);

            if (!putResult.Success)
            {
                MessageBox.Show(string.Format("The upload process failed:\n\n{0}", putResult.ToString()), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                uploadGoButton.Enabled = true;
                uploadProgressBar.Value = 0;
                return;
            }

            DateTime start = DateTime.Now;
            while ((DateTime.Now - start).TotalSeconds < 25)
            {
                Application.DoEvents();
                Thread.Sleep(1000);
                Application.DoEvents();

                Result<RequestTransferComplete.ResponseParameters> finalizeResult = client.RequestTransferComplete(result.Product.Index, uploadTitleTextBox.Text.Trim());

                if (finalizeResult.Success)
                {
                    if (finalizeResult.Product.Status.Status == WADMStatus.StatusCode.Busy)
                        continue;
                    else if (finalizeResult.Product.Status.Status == WADMStatus.StatusCode.Success)
                        break;

                    MessageBox.Show("The stereo replied that the upload failed!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    uploadGoButton.Enabled = true;
                    uploadProgressBar.Value = 0;
                    return;
                }
            }

            uploadProgressBar.Value = 75;
            Application.DoEvents();

            if (!client.SvcDbDump().Success)
            {
                MessageBox.Show("Could not synchronize the database!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                uploadGoButton.Enabled = true;
                uploadProgressBar.Value = 0;
                return;
            }

            uploadProgressBar.Value = 100;
            Application.DoEvents();

            MessageBox.Show("The file was successfully uploaded!", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (treeResetButton.Text != "Start")
                UpdateBrowser();
            uploadGoButton.Enabled = true;
            uploadProgressBar.Value = 0;
        }

        private void treePlaylistAddButton_Click(object sender, EventArgs e)
        {
            treePlaylistAddButton.Enabled = false;
            Application.DoEvents();

            if (string.IsNullOrWhiteSpace(treeAddPlaylistNameTextBox.Text))
            {
                MessageBox.Show("The playlist name is empty!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                treePlaylistAddButton.Enabled = true;
                return;
            }

            if (!client.GetUpdateID().Success)
            {
                MessageBox.Show("Could not fetch the current update ID!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                treePlaylistAddButton.Enabled = true;
                return;
            }

            Result<RequestPlaylistCreate.ResponseParameters> result = client.RequestPlaylistCreate(treeAddPlaylistNameTextBox.Text.Trim());
            if (!result.Success)
            {
                MessageBox.Show(string.Format("The process failed:\n\n{0}", result.ToString()), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                treePlaylistAddButton.Enabled = true;
                return;
            }

            if (result.Product.Status.Status != WADMStatus.StatusCode.Success)
            {
                MessageBox.Show(string.Format("The process failed:\n\n{0}", result.ToString()), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                treePlaylistAddButton.Enabled = true;
                return;
            }

            MessageBox.Show("The process succeeded!", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            treeAddPlaylistNameTextBox.Clear();
            if (treeResetButton.Text != "Start")
                if (((currentNodeID >> 24) & 0xFF) == 0x1b) // 0x1b = 0b11011 = Playlist root node
                    UpdateBrowser();

            treePlaylistAddButton.Enabled = true;
        }

        private void treePlaylistOrTrackDeleteButton_Click(object sender, EventArgs e)
        {
            treePlaylistOrTrackDeleteButton.Enabled = false;
            Application.DoEvents();

            if (treeItemsListBox.SelectedItems.Count != 1)
            {
                MessageBox.Show("No playlist or child track selected!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                treePlaylistOrTrackDeleteButton.Enabled = true;
                return;
            }

            RequestPlayableNavData.ContentData data = currentDataSet.ContentData[treeItemsListBox.SelectedIndex];
            if (data.NodeType == RequestPlayableNavData.NodeType.Playable)
            {
                if (((data.ParentID >> 24) & 0xFF) != 0x1c) // 0x1c = 0b00011100 = Playlist sub item
                {
                    MessageBox.Show("The selected track is not selected from a playlist!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    treePlaylistOrTrackDeleteButton.Enabled = true;
                    return;
                }
            }
            else if (data.NodeType == RequestPlayableNavData.NodeType.Branch)
            {
                if (((data.NodeID >> 24) & 0xFF) != 0x1c) // 0x1c = 0b00011100 = Playlist sub item
                {
                    MessageBox.Show("The selected item is not a playlist!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    treePlaylistOrTrackDeleteButton.Enabled = true;
                    return;
                }
                if (MessageBox.Show(string.Format("Do you really want to delete the following playlist:\n{0}", data.Name), "Attention!",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    treePlaylistOrTrackDeleteButton.Enabled = true;
                    return;
                }
            }
            else
            {
                treePlaylistOrTrackDeleteButton.Enabled = true;
                return;
            }

            if (!client.GetUpdateID().Success)
            {
                MessageBox.Show("Could not fetch the current update ID!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                treePlaylistOrTrackDeleteButton.Enabled = true;
                return;
            }

            Result<RequestPlaylistDelete.ResponseParameters> result = client.RequestPlaylistDelete(data.NodeID, data.Name);

            if (!result.Success)
            {
                MessageBox.Show(string.Format("The process failed:\n\n{0}", result.ToString()), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                treePlaylistOrTrackDeleteButton.Enabled = true;
                return;
            }

            if (result.Product.Status.Status != WADMStatus.StatusCode.Success)
            {
                MessageBox.Show(string.Format("The process failed:\n\n{0}", result.ToString()), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                treePlaylistOrTrackDeleteButton.Enabled = true;
                return;
            }

            MessageBox.Show("The process succeeded!", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            UpdateBrowser();
            treePlaylistOrTrackDeleteButton.Enabled = true;
        }

        RequestPlayableNavData.ContentDataPlayable markedNode = null;
        private void treeMarkButton_Click(object sender, EventArgs e)
        {
            if (treeItemsListBox.SelectedItems.Count != 1)
            {
                MessageBox.Show("Nothing selected!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            if (currentDataSet.ContentData[treeItemsListBox.SelectedIndex].NodeType != RequestPlayableNavData.NodeType.Playable)
            {
                MessageBox.Show("The selection is no playable node!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            MarkNode((RequestPlayableNavData.ContentDataPlayable)currentDataSet.ContentData[treeItemsListBox.SelectedIndex]);

            uploadTitleTextBox.Text = markedNode.Name;
            uploadArtistTextBox.Text = markedNode.Artist;
            uploadAlbumTextBox.Text = markedNode.Album;
            uploadGenreTextBox.Text = markedNode.Genre;
            uploadTracknoTextBox.Text = markedNode.TrackNo.ToString();
            uploadYearTextBox.Text = markedNode.Year.ToString();
        }

        private void MarkNode(RequestPlayableNavData.ContentDataPlayable NewNode)
        {
            markedNode = NewNode;
            UpdateMark();
        }

        private void ClearMark()
        {
            markedNode = null;
            UpdateMark();
        }

        private void UpdateMark()
        {
            if (markedNode == null)
            {
                treeMarkLabel.Text = "None";
                return;
            }

            treeMarkLabel.Text = markedNode.Name;
        }

        private void treeDeleteButton_Click(object sender, EventArgs e)
        {
            treeDeleteButton.Enabled = false;
            Application.DoEvents();

            if (markedNode == null)
            {
                MessageBox.Show("Nothing marked!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                treeDeleteButton.Enabled = true;
                return;
            }

            if (MessageBox.Show(string.Format("Do you really want to delete the following track:\n{0} - {1}", markedNode.Artist, markedNode.Title), "Attention!",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                treeDeleteButton.Enabled = true;
                return;
            }

            if (!client.GetUpdateID().Success)
            {
                MessageBox.Show("Could not fetch the current update ID!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                treeDeleteButton.Enabled = true;
                return;
            }

            Result<RequestObjectDestroy.ResponseParameters> result = client.RequestObjectDestroy(markedNode.NodeID);

            if (!result.Success)
            {
                MessageBox.Show(string.Format("The process failed:\n\n{0}", result.ToString()), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                treeDeleteButton.Enabled = true;
                return;
            }

            if (result.Product.Status.Status != WADMStatus.StatusCode.Success)
            {
                MessageBox.Show(string.Format("The process failed:\n\n{0}", result.ToString()), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                treeDeleteButton.Enabled = true;
                return;
            }

            ClearMark();
            treeResetButton.PerformClick();
            treeDeleteButton.Enabled = true;
        }

        private void treeDownloadButton_Click(object sender, EventArgs e)
        {
            treeDownloadButton.Enabled = false;
            Application.DoEvents();

            if (markedNode == null)
            {
                MessageBox.Show("Nothing marked!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                treeDownloadButton.Enabled = true;
                return;
            }

            try
            {
                string fileExt = markedNode.URL.Contains('.') ? markedNode.URL.Substring(markedNode.URL.LastIndexOf('.')) : ".mp3";
                string downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), markedNode.Title + fileExt);

                saveDownloadFileDialog.FileName = downloadPath;
                if (saveDownloadFileDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    treeDownloadButton.Enabled = true;
                    return;
                }

                web.DownloadFile(markedNode.URL, saveDownloadFileDialog.FileName);
                MessageBox.Show(string.Format("Successfully downloaded {0} - {1} to:\n{2}",
                    markedNode.Artist, markedNode.Title, downloadPath), "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error downloading the media file:\n{0}", ex.Message), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                treeDownloadButton.Enabled = true;
                return;
            }

            treeDownloadButton.Enabled = true;
        }

        private void treePlaylistPasteButton_Click(object sender, EventArgs e)
        {
            treePlaylistPasteButton.Enabled = false;
            Application.DoEvents();

            if (markedNode == null)
            {
                MessageBox.Show("Nothing marked!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                treePlaylistPasteButton.Enabled = true;
                return;
            }

            if (((currentNodeID >> 24) & 0xFF) != 0x1c) // 0x1c = 0b00011100 = Playlist sub item
            {
                MessageBox.Show("The currently active node is no playlist!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                treePlaylistPasteButton.Enabled = true;
                return;
            }

            if (!client.GetUpdateID().Success)
            {
                MessageBox.Show("Could not fetch the current update ID!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                treePlaylistPasteButton.Enabled = true;
                return;
            }

            Result<RequestPlaylistTrackInsert.ResponseParameters> result = client.RequestPlaylistTrackAdd(currentNodeID, markedNode.NodeID);
            if (!result.Success)
            {
                MessageBox.Show(string.Format("The process failed:\n\n{0}", result.ToString()), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                treePlaylistPasteButton.Enabled = true;
                return;
            }

            if (result.Product.Status.Status != WADMStatus.StatusCode.Success)
            {
                MessageBox.Show(string.Format("The process failed:\n\n{0}", result.ToString()), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                treePlaylistPasteButton.Enabled = true;
                return;
            }

            MessageBox.Show("The process succeeded!", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            UpdateBrowser();
            treePlaylistPasteButton.Enabled = true;
        }

        private void uploadUpdateMarkedButton_Click(object sender, EventArgs e)
        {
            uploadUpdateMarkedButton.Enabled = false;
            Application.DoEvents();

            if (markedNode == null)
            {
                MessageBox.Show("Nothing marked!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                uploadUpdateMarkedButton.Enabled = true;
                return;
            }

            if (!client.GetUpdateID().Success)
            {
                MessageBox.Show("Could not fetch the current update ID!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                uploadUpdateMarkedButton.Enabled = true;
                return;
            }

            Result<RequestObjectUpdate.ResponseParameters> result;
            uint fieldcount = 0;
            if (uploadTitleTextBox.Text != markedNode.Name)
            {
                result = client.RequestObjectUpdate(RequestObjectUpdate.FieldType.Name, markedNode.NodeID, uploadTitleTextBox.Text, markedNode.Name);
                if (!HandleUpdateObjectError(result))
                {
                    uploadUpdateMarkedButton.Enabled = true;
                    return;
                }

                fieldcount++;
            }

            if (uploadArtistTextBox.Text != markedNode.Artist)
            {
                result = client.RequestObjectUpdate(RequestObjectUpdate.FieldType.Artist, markedNode.NodeID, uploadArtistTextBox.Text, markedNode.Artist);
                if (!HandleUpdateObjectError(result))
                {
                    uploadUpdateMarkedButton.Enabled = true;
                    return;
                }

                fieldcount++;
            }

            if (uploadAlbumTextBox.Text != markedNode.Album)
            {
                result = client.RequestObjectUpdate(RequestObjectUpdate.FieldType.Album, markedNode.NodeID, uploadAlbumTextBox.Text, markedNode.Album);
                if (!HandleUpdateObjectError(result))
                {
                    uploadUpdateMarkedButton.Enabled = true;
                    return;
                }

                fieldcount++;
            }

            if (uploadGenreTextBox.Text != markedNode.Genre)
            {
                result = client.RequestObjectUpdate(RequestObjectUpdate.FieldType.Genre, markedNode.NodeID, uploadGenreTextBox.Text, markedNode.Genre);
                if (!HandleUpdateObjectError(result))
                {
                    uploadUpdateMarkedButton.Enabled = true;
                    return;
                }

                fieldcount++;
            }

            if (uploadTracknoTextBox.Text != markedNode.TrackNo.ToString())
            {
                uint newTrackno;
                if (!uint.TryParse(uploadTracknoTextBox.Text, out newTrackno))
                    MessageBox.Show("Invalid trackno! Skipping.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                else
                {
                    result = client.RequestObjectUpdate(RequestObjectUpdate.FieldType.TrackNum, markedNode.NodeID, newTrackno.ToString(), markedNode.TrackNo.ToString());
                    if (!HandleUpdateObjectError(result))
                    {
                        uploadUpdateMarkedButton.Enabled = true;
                        return;
                    }
                }

                fieldcount++;
            }

            if (uploadYearTextBox.Text != markedNode.Year.ToString())
            {
                uint newYear;
                if (!uint.TryParse(uploadYearTextBox.Text, out newYear))
                    MessageBox.Show("Invalid year! Skipping.", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                else
                {
                    result = client.RequestObjectUpdate(RequestObjectUpdate.FieldType.Year, markedNode.NodeID, newYear.ToString(), markedNode.Year.ToString());
                    if (!HandleUpdateObjectError(result))
                    {
                        uploadUpdateMarkedButton.Enabled = true;
                        return;
                    }
                }

                fieldcount++;
            }

            MessageBox.Show(string.Format("Successfully updated {0} field(s)!", fieldcount), "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            uploadUpdateMarkedButton.Enabled = true;
        }

        private bool HandleUpdateObjectError(Result<RequestObjectUpdate.ResponseParameters> Result)
        {
            if (!Result.Success)
            {
                MessageBox.Show(string.Format("The process failed:\n\n{0}", Result.ToString()), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            if (Result.Product.Status.Status != WADMStatus.StatusCode.Success)
            {
                MessageBox.Show("The stereo reported failure!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            return true;
        }

        private void treeMarkLabel_Click(object sender, EventArgs e)
        {
            ClearMark();
        }
    }
}