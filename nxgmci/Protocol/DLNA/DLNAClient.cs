using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using nxgmci.Device;
using System.Net;
using nxgmci.Network;

namespace nxgmci.Protocol.DLNA
{
    public class DLNAClient
    {
        public readonly EndpointDescriptor Endpoint;
        private readonly IPEndPoint ipEndpoint;

        // Rendering control endpoint
        private readonly string dlnaRCLEndoint = "/UD/action?0";
        // Audio/Video transport endpoint
        private readonly string dlnaAVTEndpoint = "/UD/action?2";

        public DLNAClient(EndpointDescriptor Endpoint)
        {
            // Input sanity checks
            // The actual port number is not yet validated since it could still change
            if (Endpoint == null)
                throw new ArgumentNullException("Endpoint");
            if (Endpoint.IPAddress == null)
                throw new NullReferenceException("Endpoint.IPAddress may not be null!");

            // Store the descriptor locally
            this.Endpoint = Endpoint;

            // Convert the endpoint to an IP endpoint
            ipEndpoint = new IPEndPoint(new IPAddress(Endpoint.IPAddress), (int)Endpoint.PortDLNAClient);
        }

        public bool QueryStatus()
        {
            return false;
        }

        public bool Ping()
        {
            return false;
        }

        public bool QueryRemoteLibrary()
        {
            return false;
        }

        public bool QueryPosition()
        {
            return false;
        }

        public bool SelectLibaryMedia(MediaElement Media)
        {
            return false;
        }

        public bool SelectMedia(Uri MediaUri)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(DLNA.DLNAAction, DLNA.DLNASelectAction);
            Postmaster.QueryResponse response =
                Postmaster.PostXML(ipEndpoint,
                dlnaAVTEndpoint,
                string.Format(DLNA.DLNASelectBody, DLNA.EscapeString(MediaUri.ToString())),
                true,
                headers);

            return (response.Success && response.StatusCode == 200);
        }

        public bool Play()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(DLNA.DLNAAction, DLNA.DLNAPlayAction);
            Postmaster.QueryResponse response =
                Postmaster.PostXML(ipEndpoint,
                dlnaAVTEndpoint,
                string.Format(DLNA.DLNAPlayBody, 1),
                true,
                headers);

            return (response.Success && response.StatusCode == 200);
        }

        public bool Pause()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(DLNA.DLNAAction, DLNA.DLNAPauseAction);
            Postmaster.QueryResponse response =
                Postmaster.PostXML(ipEndpoint,
                dlnaAVTEndpoint,
                string.Format(DLNA.DLNAPauseBody, 1),
                true,
                headers);

            return (response.Success && response.StatusCode == 200);
        }

        public bool Stop()
        {
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add(DLNA.DLNAAction, DLNA.DLNAStopAction);
            Postmaster.QueryResponse response =
                Postmaster.PostXML(ipEndpoint,
                dlnaAVTEndpoint,
                string.Format(DLNA.DLNAStopBody, 1),
                true,
                headers);

            return (response.Success && response.StatusCode == 200);
        }

        public bool Seek(float Position)
        {
            return false;
        }

        public bool GetVolume()
        {
            return false;
        }

        public bool SetVolume()
        {
            return false;
        }
    }
}
