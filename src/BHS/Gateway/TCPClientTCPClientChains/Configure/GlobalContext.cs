#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       Initializer.cs
// Revision:      1.0 -   02 Apr 2009, By Xu Jian
// =====================================================================================
//
#endregion


using System;

namespace BHS.Gateway.TCPClientTCPClientChains.Configure
{
    /// <summary>
    /// Parameter class for storing application global context settings.
    /// </summary>
    public class GlobalContext 
    {
        #region Class Fields Declaration

        /// <summary>
        /// Company name
        /// </summary>
        public string Company { get; set; }
        /// <summary>
        /// Department name
        /// </summary>
        public string Department { get; set; }
        /// <summary>
        /// Author name
        /// </summary>
        public string Author { get; set; }
        /// <summary>
        /// Application name
        /// </summary>
        public string AppName { get; set; }
        /// <summary>
        /// Application starting time
        /// </summary>
        public DateTime AppStartedTime { get; set; }

        #endregion

        #region Class Constructor & Destructor

        #endregion

        #region Class Properties

        #endregion

        #region Class Methods

        #endregion
    }
}
