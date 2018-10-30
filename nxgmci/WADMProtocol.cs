using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci
{
    internal static class WADMProtocol
    {
        // == WADM ==
        // This describes Philips' custom protocol used for library management on the MCI500H series of devices

        /*
        internal static string RequestRawData(uint fromIndex, uint numElem)
        {
            return string.Format(
                "<requestrawdata>" +
                "<requestparameters>" +
                "<fromindex>{0}</fromindex>" +
                "<numelem>{1}</numelem>" +
                "</requestparameters>" +
                "</requestrawdata>",
                fromIndex,
                numElem);
        }

        struct RequestRawData

        struct RequestRawDataReponse
        {
            string Name;
            int NodeID;
            int Album;
            int TrackNo;
            int Artist;
            int Genre;
            int Year;
            int MediaType;
            int DMMCookie;
        }

        <contentdataset>
    <contentdata>
        <name>		string:	Track title																				</name>
        <nodeid>	int:	Special ID number that has to be masked with & idmask to get the file's path			</nodeid>
        <album>		int:	ID number of the album set it belongs to												</album>
        <trackno>	int:	Positional index of the track in the album (might potentionally be string not int)		</trackno>
        <artist>	int:	ID number of the artist it belongs to													</artist>
        <genre>		int:	ID number of the genre it belongs to													</genre>
        <year>		int:	Year that the track was published / recorded in (might potentionally be string not int)	</year>
        <mediatype>	int:	Type of the media (refers to the urimetadata table of media types)						</mediatype>
        <dmmcookie>	int:	UNKNOWN! e.g. 1644662629																</dmmcookie>
    </contentdata>
    <totnumelem>	int: 	Total number of elements that could potentionally be queried							</totnumelem>
    <fromindex>		int:	Copy of the request start index parameter												</fromindex>
    <numelem>		int:	Number of elements returned in this query												</numelem>
    <updateid>		int: 	UNKNOWN! e.g. 422																		</updateid>
</contentdataset>*/
    }
}
