using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Protocol
{
    internal static class DLNA
    {
        // == DLNA ==
        // Body refers to the XML body to be send
        // Action refers to the HTTP header SOAPAction to be set with each request

        // Note: we can play local URIs! file:///mnt/hda/content/media/29/29866.mp3

        // == Audio/Video Transport Endpoint ==

        // == Action Header
        internal static readonly string DLNAAction = "SOAPAction";

        // == Select media
        // Selects the remote media to be played
        // Parameter 0: URL to be loaded
        internal static readonly string DLNASelectBody = "<?xml version=\"1.0\"?>" +
            "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" SOAP-ENV:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
            "<SOAP-ENV:Body>" +
            "<u:SetAVTransportURI xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\">" +
            "<InstanceID>0</InstanceID>" +
            "<CurrentURI>{0}</CurrentURI>" +
            "<CurrentURIMetaData></CurrentURIMetaData>" +
            "</u:SetAVTransportURI>" +
            "</SOAP-ENV:Body>" +
            "</SOAP-ENV:Envelope>";
        internal static readonly string DLNASelectAction = "urn:schemas-upnp-org:service:AVTransport:1#SetAVTransportURI";

        // == Play
        // Starts playback of the currently loaded remote media
        // Parameter 0: Playback speed (usually 1)
        internal static readonly string DLNAPlayBody = "<?xml version=\"1.0\"?>" +
            "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" SOAP-ENV:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
            "<SOAP-ENV:Body>" +
            "<u:Play xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>0</InstanceID><Speed>{0}</Speed></u:Play>" +
            "</SOAP-ENV:Body>" +
            "</SOAP-ENV:Envelope>";
        internal static readonly string DLNAPlayAction = "urn:schemas-upnp-org:service:AVTransport:1#Play";

        // == Pause
        // Pauses current playback
        internal static readonly string DLNAPauseBody = "<?xml version=\"1.0\"?>" +
            "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" SOAP-ENV:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
            "<SOAP-ENV:Body>" +
            "<u:Pause xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>0</InstanceID></u:Pause>" +
            "</SOAP-ENV:Body>" +
            "</SOAP-ENV:Envelope>";
        internal static readonly string DLNAPauseAction = "urn:schemas-upnp-org:service:AVTransport:1#Pause";

        // == Stop
        // Stops current playback and exits DLNA playback
        internal static readonly string DLNAStopBody = "<?xml version=\"1.0\"?>" +
            "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" SOAP-ENV:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
            "<SOAP-ENV:Body>" +
            "<u:Stop xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>0</InstanceID></u:Stop>" +
            "</SOAP-ENV:Body>" +
            "</SOAP-ENV:Envelope>";
        internal static readonly string DLNAStopAction = "urn:schemas-upnp-org:service:AVTransport:1#Stop";

        // == Seek
        // Seeks 

        // Escape a string to be somewhat XML-safe
        internal static string EscapeString(string Input)
        {
            // Sanity checks
            if (string.IsNullOrEmpty(Input))
                return Input;

            // Replace all offensive XML characters with their escapes
            Input = Input.Replace("'", "&apos;");
            Input = Input.Replace("\"", "&quot;");
            Input = Input.Replace("<", "&lt;");
            Input = Input.Replace(">", "&gt;");
            Input = Input.Replace("&", "&amp;");

            // Return the new string
            return Input;
        }
    }
}
