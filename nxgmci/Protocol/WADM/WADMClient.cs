using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using nxgmci.Network;

namespace nxgmci.Protocol.WADM
{
    public class WADMClient
    {
        //internal static void Emit

        // TODO: Events

        public DeviceDescriptor Descriptor;

        private object eventLock;

        public WADMClient()
        {
            
        }

        public Result<GetUpdateID.ResponseParameters> GetUpdateID()
        {
            // Lock the class to ensure thread safety
            /*lock (eventLock)
            {
                Postmaster.QueryResponse queryResp = Postmaster.PostXML(new Uri(baseurl + ":8081/"),
                (transmitTextBox.Text = RequestRawData.Build(0, 0)), true);
                if (!queryResp.Success)
                {
                    MessageBox.Show("An error occured: " + queryResp.Message);
                    return;
                }
                Result<RequestRawData.ContentDataSet> contentResp = RequestRawData.Parse(queryResp.TextualResponse);
                if (!contentResp.Success)
                {
                    MessageBox.Show(contentResp.ToString());
                    return;
                }
            }*/
            throw new NotImplementedException();
        }

        public Result<QueryDatabase.ResponseParameters> QueryDatabase()
        {
            throw new NotImplementedException();
        }

        public Result<QueryDiskSpace.ResponseParameters> QueryDiskSpace()
        {
            throw new NotImplementedException();
        }

        public Result<RequestAlbumIndexTable.ContentDataSet> RequestAlbumIndexTable()
        {
            throw new NotImplementedException();
        }
    }
}
