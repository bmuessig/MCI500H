﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;

namespace nxgmci
{
    public class MCI500H
    {
        public IPAddress DeviceIP
        {
            get;
            private set;
        }

        public float Volume
        {
            get;
            private set;
        }

        public Uri MediaUri
        {
            get;
            private set;
        }

        public MediaLibrary RemoteLibrary
        {
            get;
            private set;
        }

        public event EventHandler<CompletedEventArgs> Completed;

        internal WebClient Client;

        private static readonly Regex mediaTypeRegex = new Regex("(\\d+)[ \\t]*=+[ \\t]*(\\w+)(?:[ \\t]*,|$)",
            RegexOptions.Singleline | RegexOptions.Compiled);

        // Rendering control endpoint
        private static readonly string dlnaRCLEndoint = "/UD/action?0";
        // Audio/Video transport endpoint
        private static readonly string dlnaAVTEndpoint = "/UD/action?2";

        protected virtual void OnCompleted(CompletedEventArgs e)
        {
            if (Completed != null && e != null)
                Completed(this, e);
        }

        public MCI500H(IPAddress DeviceIP)
        {
            this.DeviceIP = DeviceIP;
            this.Client = new WebClient();
        }

        public bool QueryStatus()
        {
            return true;
        }

        public void QueryStatusAsync()
        {

        }

        public bool Ping()
        {
            return true;
        }
        
        public void PingAsync()
        {

        }

        public bool QueryRemoteLibrary()
        {
            return true;
        }

        public void QueryRemoteLibraryAsync()
        {

        }

        public bool QueryPosition()
        {
            return true;
        }

        public void QueryPositionAsync()
        {

        }

        public bool SelectLibaryMedia(MediaElement Media)
        {
            return true;
        }

        public void SelectLibraryMediaAsync(MediaElement Media)
        {
            
        }

        public bool SelectMedia(Uri MediaUri)
        {
            Dictionary<string, string> headers = new Dictionary<string,string>();
            headers.Add(DLNAProtocol.DLNAAction, DLNAProtocol.DLNASelectAction);
            Postmaster.QueryResponse response =
                Postmaster.PostXML(new IPEndPoint(DeviceIP, 8100),
                dlnaAVTEndpoint,
                string.Format(DLNAProtocol.DLNASelectBody, DLNAProtocol.EscapeString(MediaUri.ToString())),
                true,
                headers);

            return (response.Success && response.StatusCode == 200);
        }

        public void SelectMediaAsync(Uri MediaUri)
        {
            
        }

        public bool Play()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(DLNAProtocol.DLNAAction, DLNAProtocol.DLNAPlayAction);
            Postmaster.QueryResponse response =
                Postmaster.PostXML(new IPEndPoint(DeviceIP, 8100),
                dlnaAVTEndpoint,
                string.Format(DLNAProtocol.DLNAPlayBody, 1),
                true,
                headers);

            return (response.Success && response.StatusCode == 200);
        }

        public void PlayAsync()
        {

        }

        public bool Pause()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(DLNAProtocol.DLNAAction, DLNAProtocol.DLNAPauseAction);
            Postmaster.QueryResponse response =
                Postmaster.PostXML(new IPEndPoint(DeviceIP, 8100),
                dlnaAVTEndpoint,
                string.Format(DLNAProtocol.DLNAPauseBody, 1),
                true,
                headers);

            return (response.Success && response.StatusCode == 200);
        }

        public void PauseAsync()
        {

        }

        public bool Stop()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(DLNAProtocol.DLNAAction, DLNAProtocol.DLNAStopAction);
            Postmaster.QueryResponse response =
                Postmaster.PostXML(new IPEndPoint(DeviceIP, 8100),
                dlnaAVTEndpoint,
                string.Format(DLNAProtocol.DLNAStopBody, 1),
                true,
                headers);

            return (response.Success && response.StatusCode == 200);
        }

        public bool Seek(float Position)
        {
            return true;
        }

        public void SeekAsync(float Position)
        {

        }

        public void StopAsync()
        {

        }

        public bool GetVolume()
        {
            return true;
        }

        public void GetVolumeAsync()
        {

        }

        public bool SetVolume()
        {
            return true;
        }

        public void SetVolumeAsync()
        {

        }
    }
}
