using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BHS
{
    /// <summary>
    /// Struct for CRAI.
    /// </summary>    
    public struct CRAIList
    {
        /// <summary>
        /// Carrier Code.
        /// </summary>  
        public int CarrierCode;

        /// <summary>
        /// Sort Device Destination.
        /// </summary>  
        public int SortDeviceDestination;

        /// <summary>
        /// Sort Device Destination Description
        /// </summary>
        public string SortDeviceDescription;
    }

    /// <summary>
    /// Struct for Tag.
    /// </summary>   
    public struct Tag
    {
        /// <summary>
        /// Tag Code.
        /// </summary>  
        public int Code;

        /// <summary>
        /// Tag Destination.
        /// </summary>  
        public int Destination;

        /// <summary>
        /// Tag Destination Description
        /// </summary>
        public string DestinationDescription;
    }

    public class Common
    {
        /// <summary>
        /// Global variable for Airport Location Code
        /// </summary>
        public static string AirportLocationCode;

    }

}
