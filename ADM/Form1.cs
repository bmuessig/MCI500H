﻿using System;
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

namespace ADM
{
    public partial class Form1 : Form
    {
        private static string baseurl = "http://10.0.0.10";
        private MCI500H stereo = new MCI500H(IPAddress.Parse(baseurl.Substring(baseurl.LastIndexOf('/') + 1)));
        int lastid = -1;

        public Form1()
        {
            InitializeComponent();
            CoverCrypt.CalculateCryptoKey();
        }

        private void speedTestButton_Click(object sender, EventArgs e)
        {
            string url = baseurl + ":8081/";
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
            Postmaster.QueryResponse response = Postmaster.PostXML(new Uri(baseurl + ":8081/"), transmitTextBox.Text);
            if (response.Success)
                MessageBox.Show(string.Format("HTTP {0} {1}:\n\n{2}", response.StatusCode, response.Message, response.TextualResponse));
            else
                MessageBox.Show("An error occured: " + response.Message);
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
            Postmaster.QueryResponse response = Postmaster.PostXML(new Uri(baseurl + ":8081/"),
                (transmitTextBox.Text = RequestRawData.Build(0, 0)), true);
            if (!response.Success)
            {
                MessageBox.Show("An error occured: " + response.Message);
                return;
            }
            ActionResult<RequestRawData.ContentDataSet> contentResp = RequestRawData.Parse(response.TextualResponse);
            if (!contentResp.Success)
            {
                MessageBox.Show("An error occured: " + contentResp.ErrorMessage);
                return;
            }

            // Fetch artists
            response = Postmaster.PostXML(new Uri(baseurl + ":8081/"),
                (transmitTextBox.Text = RequestArtistIndexTable.Build()), true);
            if (!response.Success)
            {
                MessageBox.Show("An error occured: " + response.Message);
                return;
            }
            ActionResult<RequestArtistIndexTable.ContentDataSet> artistResp = RequestArtistIndexTable.Parse(response.TextualResponse);
            if (!artistResp.Success)
            {
                MessageBox.Show("An error occured: " + artistResp.ErrorMessage);
                return;
            }

            // Fetch albums
            response = Postmaster.PostXML(new Uri(baseurl + ":8081/"),
                (transmitTextBox.Text = RequestAlbumIndexTable.Build()), true);
            if (!response.Success)
            {
                MessageBox.Show("An error occured: " + response.Message);
                return;
            }
            ActionResult<RequestAlbumIndexTable.ContentDataSet> albumResp = RequestAlbumIndexTable.Parse(response.TextualResponse);
            if (!albumResp.Success)
            {
                MessageBox.Show("An error occured: " + albumResp.ErrorMessage);
                return;
            }

            // Fetch urimetadata
            response = Postmaster.PostXML(new Uri(baseurl + ":8081/"),
                (transmitTextBox.Text = RequestUriMetaData.Build()), true);
            if (!response.Success)
            {
                MessageBox.Show("An error occured: " + response.Message);
                return;
            }
            ActionResult<RequestUriMetaData.ResponseParameters> metaDataResp = RequestUriMetaData.Parse(response.TextualResponse);
            if (!metaDataResp.Success)
            {
                MessageBox.Show("An error occured: " + metaDataResp.ErrorMessage);
                return;
            }

            currentMediaLib = contentResp.Result;
            currentArtistIndex = artistResp.Result;
            currentAlbumIndex = albumResp.Result;
            currentUriMetaData = metaDataResp.Result;
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
            DateTime internalStart = DateTime.Now;

            Postmaster.QueryResponse response = Postmaster.PostXML(new Uri(baseurl + ":8081/"),
                (transmitTextBox.Text = QueryDiskSpace.Build()), true);

            if (!response.Success)
            {
                MessageBox.Show("An error occured: " + response.Message);
                return;
            }

            ActionResult<QueryDiskSpace.ResponseParameters> parserResp = QueryDiskSpace.Parse(response.TextualResponse);
            if (!parserResp.Success)
            {
                MessageBox.Show("An error occured: " + parserResp.ErrorMessage);
                return;
            }

            DateTime internalStop = DateTime.Now;

            MessageBox.Show(string.Format("Free disk space: {0} GB / {1} GB ({2}%)\n\nTook {3}ms for request, response and parsing.",
                parserResp.Result.Size / 1024 / 1024 / 1024,
                parserResp.Result.TotalSize / 1024 / 1024 / 1024,
                (parserResp.Result.Size * 100) / parserResp.Result.TotalSize,
                (internalStop - internalStart).TotalMilliseconds));
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
            if (lastid + 1 < currentMediaLib.ContentData.Count)
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
                    MessageBox.Show("Can't make heads or tails about the entered string!");
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
    }
}
