using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;

namespace nxgmci.Network
{
    public static class Postmaster
    {
        internal static uint ConnectTimeoutMilliseconds = 5000;
        internal static uint ResponseTimeoutMilliseconds = 1000;
        internal static uint ReceiveTimeoutMilliseconds = 1500;

        private static readonly Regex httpRegex = new Regex("^HTTP\\/(\\d\\.\\d)[ \\t]+(\\d+)[ \\t]+([\\w ]+)$",
            RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex headersRegex = new Regex("^([\\w-]+):[ \\t]+(.*)$",
            RegexOptions.Singleline | RegexOptions.Compiled);

        public static QueryResponse PostXML(Uri Uri, string Payload,
            bool ForceTextualResponse = false, Dictionary<string, string> QueryHeaders = null)
        {
            // Handle the XML-specific header
            if (QueryHeaders == null)
                QueryHeaders = new Dictionary<string, string>();
            if (QueryHeaders.ContainsKey("Content-Type"))
                QueryHeaders.Remove("Content-Type");
            QueryHeaders.Add("Content-Type", "text/xml");

            return PostString(Uri, Payload, ForceTextualResponse, QueryHeaders);
        }

        public static QueryResponse PostXML(IPEndPoint EndPoint, string Path, string Payload,
            bool ForceTextualResponse = false, Dictionary<string, string> QueryHeaders = null)
        {
            // Handle the XML-specific header
            if (QueryHeaders == null)
                QueryHeaders = new Dictionary<string, string>();
            if (QueryHeaders.ContainsKey("Content-Type"))
                QueryHeaders.Remove("Content-Type");
            QueryHeaders.Add("Content-Type", "text/xml");

            return PostString(EndPoint, Path, Payload, ForceTextualResponse, QueryHeaders);
        }

        public static QueryResponse PostString(Uri Uri, string Payload,
            bool ForceTextualResponse = false, Dictionary<string, string> QueryHeaders = null)
        {
            IPEndPoint endpoint;
            if (!NetUtils.EndPointFromUri(Uri, out endpoint))
                return new QueryResponse("Cannot resolve endpoint from URI!");

            return PostString(endpoint, Uri.PathAndQuery, Payload, ForceTextualResponse, QueryHeaders);
        }

        public static QueryResponse PostString(IPEndPoint EndPoint, string Path, string Payload,
            bool ForceTextualResponse = false, Dictionary<string, string> QueryHeaders = null)
        {
            if (Payload == null)
                return new QueryResponse("Payload may not be null!");

            return PostIt(EndPoint, Path, Encoding.UTF8.GetBytes(Payload), true, ForceTextualResponse, QueryHeaders);
        }

        private static QueryResponse PostIt(IPEndPoint EndPoint, string Path, byte[] Payload, bool TextualPayload,
            bool ForceTextualResponse, Dictionary<string, string> QueryHeaders)
        {
            if (EndPoint == null)
                return new QueryResponse("EndPoint may not be null!");
            if(EndPoint.Address == null || EndPoint.Port == 0 || EndPoint.Port >= ushort.MaxValue)
                return new QueryResponse("Invalid port or address!");
            if (string.IsNullOrWhiteSpace(Path))
                Path = "/";
            if (Payload == null)
                Payload = new byte[0];

            try
            {
                // Try setting up a new TcpClient to connect to one of the stereo's web servers
                TcpClient client = new TcpClient()
                {
                    ReceiveBufferSize = 1024 * 1024,
                    ReceiveTimeout = (int)ReceiveTimeoutMilliseconds
                };
                
                // Connect "async" to give us control over the connection process and also support a timeout
                IAsyncResult result = client.BeginConnect(EndPoint.Address, EndPoint.Port, null, null);
                if (!result.AsyncWaitHandle.WaitOne((int)ConnectTimeoutMilliseconds, true))
                {
                    // Timeout error
                    client.Close();
                    return new QueryResponse("Timeout connecting to server!");
                }
                // Finish connecting
                client.EndConnect(result);
                
                // Make sure we are now connected
                if (!client.Connected)
                    return new QueryResponse("Error connecting to server!");

                // Get us the underlying stream
                NetworkStream stream = client.GetStream();
                // And create a textwriter object to simplify working with the UTF8/ASCII based HTTP protocol
                TextWriter writer = new StreamWriter(stream, Encoding.ASCII);
                TextReader reader = new StreamReader(stream, Encoding.UTF8);

                // Prepare the headers to be sent
                if (QueryHeaders == null)
                    QueryHeaders = new Dictionary<string, string>();
                // Host
                if (!QueryHeaders.ContainsKey("Host"))
                    QueryHeaders.Remove("Host");
                QueryHeaders.Add("Host", EndPoint.Address.ToString());
                // Accept
                if (!QueryHeaders.ContainsKey("Accept"))
                    QueryHeaders.Add("Accept", "*/*");
                // Content-Type
                if (!QueryHeaders.ContainsKey("Content-Type") && TextualPayload)
                    QueryHeaders.Add("Content-Type", "text/plain");
                // Content-Length
                if (QueryHeaders.ContainsKey("Content-Length"))
                    QueryHeaders.Remove("Content-Length");
                QueryHeaders.Add("Content-Length", Payload.Length.ToString());

                // Assemble and send the HTTP header
                writer.Write("POST {0} HTTP/1.0\r\n", Path);

                // Now, loop through all headers in order
                foreach (KeyValuePair<string, string> queryHeader in QueryHeaders)
                {
                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(queryHeader.Key) || queryHeader.Value == null)
                        continue;
                    // And write them in the correct format
                    writer.Write("{0}: {1}\r\n", queryHeader.Key.Trim(), queryHeader.Value.TrimStart());
                }

                // Finish the request header
                writer.Write("\r\n");
                // Make sure the writer has transmitted everything
                writer.Flush();

                // Finally, send our Payload
                stream.Write(Payload, 0, Payload.Length);
                // And make sure again it's all transmitted
                stream.Flush();

                // Wait until we actually receive data
                DateTime startTime = DateTime.Now;
                while (client.Available <= 0)
                    if ((DateTime.Now - startTime).Milliseconds >= ResponseTimeoutMilliseconds)
                    {
                        stream.Close();
                        client.Close();
                        return new QueryResponse("Timeout waiting for server reply!");
                    }


                string line = reader.ReadLine();
                Match httpMatch = httpRegex.Match(line);
                // We need to succeed parsing the reply which has 1 (complete match) + 3 groups:
                // (1) HTTP 1.x Version (either 1 or 0)
                // (2) Status code (e.g. 200, 404, etc.)
                // (3) Message (e.g. OK, Not found, etc.)
                if (!httpMatch.Success || httpMatch.Groups.Count != 4)
                {
                    stream.Close();
                    client.Close();
                    return new QueryResponse("Error parsing server response!");
                }

                // Declare variables for our fields
                uint statusCode;
                string httpVersion = httpVersion = httpMatch.Groups[1].Value.Trim(),
                    message = httpMatch.Groups[3].Value.Trim();

                // First, check the protocol version
                if(httpVersion != "1.0" && httpVersion != "1.1")
                {
                    stream.Close();
                    client.Close();
                    return new QueryResponse("Unsupported HTTP version! Make sure the server's protocol version is 1.0 or 1.1.");
                }

                // Next, parse the status code
                if (!uint.TryParse(httpMatch.Groups[2].Value.Trim(), out statusCode))
                {
                    stream.Close();
                    client.Close();
                    return new QueryResponse("Error parsing HTTP status code!");
                }

                // Allocate space for the headers and other state variables
                Dictionary<string, string> headers = new Dictionary<string, string>();
                uint expectedSize = 0;
                string expectedType = null;
                bool expectingTextualReply = false;

                // Finally, loop through all headers
                do
                {
                    line = reader.ReadLine();
                    Match headerMatch = headersRegex.Match(line);
                    
                    // Check, if our headers are readable
                    if (!headerMatch.Success)
                    {
                        // These two fields are pretty important for us, so if they are corrupted, we've got a problem!
                        if (line.ToLower().Contains("content-type") || line.ToLower().Contains("content-length"))
                        {
                            stream.Close();
                            client.Close();
                            return new QueryResponse(statusCode, "Corrupted Content-Type or Content-Length headers encountered!", headers);
                        }
                        else
                            continue;
                    }

                    // We need to succeed parsing the header which has 1 (complete match) + 2 groups:
                    // (1) Name of the header
                    // (2) Value of the header
                    if (headerMatch.Groups.Count != 3)
                        continue;

                    // Store and trim the values temporarily
                    string headerName = headerMatch.Groups[1].Value.Trim(), headerValue = headerMatch.Groups[2].Value.Trim();

                    // Finally, check if we can make use of the header data
                    switch (headerName.ToLower())
                    {
                        case "content-length":
                            if (!uint.TryParse(headerValue, out expectedSize))
                            {
                                stream.Close();
                                client.Close();
                                return new QueryResponse(statusCode, "Cannot parse Content-Length field!", headers);
                            }
                            break;
                        case "content-type":
                            expectedType = headerValue;
                            if(headerValue.ToLower().StartsWith("text/") || headerValue.ToLower().Contains("utf-8") ||
                                headerValue.ToLower().Contains("ascii"))
                                expectingTextualReply = true;
                            break;
                        default:
                            headers.Add(headerName, headerValue);
                            break;
                    }
                } while (line.Length > 0);

                if (expectingTextualReply)
                {
                    string reply;
                    StringBuilder replyBuilder = new StringBuilder();

                    string buffer = string.Empty;
                    do
                    {
                        buffer = reader.ReadToEnd();
                        replyBuilder.Append(buffer);
                    } while (buffer.Length > 0);

                    // Get the reply and copy it
                    reply = string.Copy(replyBuilder.ToString());

                    // Close the streams and clean up
                    stream.Close();
                    client.Close();
                    replyBuilder.Clear();

                    return new QueryResponse(statusCode, message, headers, reply, expectedSize, expectedType);
                }
                else
                {
                    byte[] reply;
                    MemoryStream replyStream = new MemoryStream();
                    reader.Dispose();

                    // Check, if we know the size of the binary content ahead of time
                    if (expectedSize > 0)
                    {
                        // Copy everything the server throws at us
                        long lastPosition = 0;
                        while (replyStream.Position < expectedSize)
                        {
                            // Copy over all receieved data
                            stream.CopyTo(replyStream);

                            // Break if we receive no more data
                            if (replyStream.Position - lastPosition == 0)
                                break;
                        }

                        // Make sure our response is actually of the expected size
                        if (replyStream.Position > expectedSize)
                        {
                            stream.Close();
                            client.Close();
                            replyStream.Close();
                            return new QueryResponse(statusCode, "The server's reply did not match the Content-Length field!",
                                headers, expectedSize, expectedType);
                        }
                    }
                    else
                    {
                        // Copy over all receieved data
                        long lastPosition = 0;
                        while (replyStream.Position - lastPosition == 0)
                            stream.CopyTo(replyStream);
                    }

                    // Get the reply stream's buffer
                    reply = replyStream.ToArray();

                    // Close the streams
                    stream.Close();
                    client.Close();
                    replyStream.Close();

                    return new QueryResponse(statusCode, message, headers, reply, expectedSize, expectedType);
                }
            }
            catch (Exception ex)
            {
                return new QueryResponse(ex.Message);
            }
        }

        public class QueryResponse
        {
            public uint StatusCode { get; private set; }
            public Dictionary<string, string> Headers { get; private set; }
            public uint ExpectedSize { get; private set; }
            public string ExpectedType { get; private set; }
            public bool IsTextualReponse { get; private set; }
            public string TextualResponse { get; private set; }
            public byte[] BinaryResponse { get; private set; }
            public bool Success { get; private set; }
            public string Message { get; private set; }

            // This is the most basic constructor - the request failed with no more information
            internal QueryResponse(string Message = "")
            {
                if (Message == null)
                    Message = string.Empty;

                this.Success = false;
                this.Message = Message;
                this.Headers = new Dictionary<string,string>();
            }

            internal QueryResponse(uint StatusCode, string Message)
            {
                if (Message == null)
                    Message = string.Empty;

                this.Success = false;
                this.StatusCode = StatusCode;
                this.Message = Message;
                this.Headers = new Dictionary<string, string>();
            }

            internal QueryResponse(uint StatusCode, string Message, Dictionary<string, string> Headers,
                uint ExpectedSize = 0, string ExpectedType = null)
            {
                if (Headers == null)
                    Headers = new Dictionary<string, string>();
                if (Message == null)
                    Message = string.Empty;

                this.Success = false;
                this.StatusCode = StatusCode;
                this.Message = Message;
                this.Headers = Headers;
                this.ExpectedSize = ExpectedSize;
                this.ExpectedType = ExpectedType;
            }

            internal QueryResponse(uint StatusCode, string Message, Dictionary<string, string> Headers, string TextualResponse,
                uint ExpectedSize = 0, string ExpectedType = null)
            {
                if (Headers == null)
                    Headers = new Dictionary<string, string>();
                if (Message == null)
                    Message = string.Empty;
                if (TextualResponse == null)
                    TextualResponse = string.Empty;

                this.Success = true;
                this.StatusCode = StatusCode;
                this.Message = Message;
                this.Headers = Headers;
                this.IsTextualReponse = true;
                this.TextualResponse = TextualResponse;
                this.ExpectedSize = ExpectedSize;
                this.ExpectedType = ExpectedType;
            }

            internal QueryResponse(uint StatusCode, string Message, Dictionary<string, string> Headers, byte[] BinaryResponse,
                uint ExpectedSize = 0, string ExpectedType = null)
            {
                if (Headers == null)
                    Headers = new Dictionary<string, string>();
                if (Message == null)
                    Message = string.Empty;
                if (BinaryResponse == null)
                    BinaryResponse = new byte[0];

                this.Success = true;
                this.StatusCode = StatusCode;
                this.Message = Message;
                this.Headers = Headers;
                this.IsTextualReponse = false;
                this.BinaryResponse = BinaryResponse;
                this.ExpectedSize = ExpectedSize;
                this.ExpectedType = ExpectedType;
            }
        }
    }
}
