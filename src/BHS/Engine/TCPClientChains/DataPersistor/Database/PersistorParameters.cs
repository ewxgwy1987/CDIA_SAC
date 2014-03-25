#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       PersistorParameters.cs
// Revision:      1.0 -   06 Jun 2009, By Xu Jian
// =====================================================================================
//
#endregion

using System;
using System.Xml;
using PALS.Utilities;

namespace BHS.Engine.TCPClientChains.DataPersistor.Database
{
    /// <summary>
    /// Parameter class used to store parameters of MessageHandler class.
    /// </summary>
    public class PersistorParameters: PALS.Common.IParameters, IDisposable
    {
        #region Class Fields Declaration

        private const string DB_CONNECTION_STRING = "connectionString";
        private const string DB_STP_SAC_GIDUSED = "stp_SAC_GIDUsed";
        private const string DB_STP_SAC_ITEMSCREENED = "stp_SAC_ItemScreened";
        private const string DB_STP_SAC_ITEMSCANNED = "stp_SAC_ItemScanned";
        private const string DB_STP_SAC_ITEMSORTATIONEVENT = "stp_SAC_ItemSortationEvent";
        private const string DB_STP_SAC_ITEMPROCEEDED = "stp_SAC_ItemProceeded";
        private const string DB_STP_SAC_ITEMLOST = "stp_SAC_ItemLost";
        private const string DB_STP_SAC_ITEMTRACKING = "stp_SAC_ItemTrackingInformation";
        private const string DB_STP_SAC_ITEMENCODINGREQUEST = "stp_SAC_ItemEncodingRequest";
        private const string DB_STP_SAC_AIRPORTCODEFUNCALLOCINFORMATION = "stp_SAC_AirportCodeFuncAllocInfomation";
        private const string DB_STP_SAC_BAGGAGEMEASUREMENTARRAY = "stp_SAC_BaggageMeasurementArray";
        private const string DB_STP_SAC_CARRIERALLOCINFORMATION = "stp_SAC_CarrierAllocInformation";
        private const string DB_STP_SAC_FALLBACKTAGINFORMATION = "stp_SAC_FallbackTagInformation";
        private const string DB_STP_SAC_FOURPIERTAGINFORMATION = "stp_SAC_FourPierTagInformation";
        private const string DB_STP_SAC_1500PINFORMATION = "stp_SAC_Item1500P";
        private const string DB_STP_SAC_ITEMREDIRECT = "stp_SAC_ItemRedirect";
        private const string DB_STP_SAC_COLLECTENTIREAFAI = "stp_SAC_CollectEntireAFAI";
        private const string DB_STP_SAC_COLLECTENTIRECRAI = "stp_SAC_CollectEntireCRAI";
        private const string DB_STP_SAC_COLLECTENTIREFBTI = "stp_SAC_CollectEntireFBTI";
        private const string DB_STP_SAC_COLLECTENTIREFPTI = "stp_SAC_CollectEntireFPTI";
        private const string DB_STP_SAC_COLLECTCHANGEDCRAI = "stp_SAC_CollectChangedCRAI";
        private const string DB_STP_SAC_COLLECTCHANGEDFBTI = "stp_SAC_CollectChangedFBTI";
        private const string DB_STP_SAC_COLLECTCHANGEDFPTI = "stp_SAC_CollectChangedFPTI";
        private const string DB_STP_SAC_COLLECTCHANGEDTABLES = "stp_SAC_CollectChangedTables";
        private const string DB_STP_SAC_TAGDUPLICATIONCHECK = "stp_SAC_TagDuplicationCheck";        
        private const string DB_STP_SAC_DUPLICATELICENSEPLATESTATUS = "stp_SAC_DuplicationLicensePlateStatus";
        private const string DB_STP_SAC_IRDVALUES = "stp_SAC_GetIRDValues";
        private const string DB_STP_SAC_ALLOCATIONPROPERTY = "stp_SAC_GetAllocProp";
        private const string DB_STP_SAC_IRDVALUESMES = "stp_SAC_GetIRDValuesMES";
        private const string DB_STP_SAC_CHECKMUAVAILABILITY = "stp_SAC_CheckMUAvailability";
        private const string DB_STP_SAC_GETBAGINFO = "stp_SAC_GETBAGINFO";
        private const string DB_STP_SAC_CHECKTAGDEST = "stp_SAC_CHECKTAGDEST";
       
        private const string DB_AIRPORT_CODE_DESC = "airportCodeDesc";
        private const string DB_FUNC_ALLOCATION_NOCR = "funcAllocation_NOCR";
        private const string DB_FUNC_ALLOCATION_NOAL = "funcAllocation_NOAL";
        private const string DB_FUNC_ALLOCATION_DUMPDISC = "funcAllocation_DUMP";
        private const string DB_FUNC_ALLOCATION_NORD = "funcAllocation_NORD";

        private const string DB_TBL_CARRIER_LOG = "table_CarrierLog";
        private const string DB_TBL_FALLBACK_TAG_MAPPING = "table_FallbackMapping";
        private const string DB_TBL_FOUR_PIER_TAG_MAPPING = "table_FourPierTagMapping";
        private const string DB_TBL_FUNCTION_ALLOC_LIST = "table_FunctionAllocList";
        private const string DB_TBL_SYS_CONFIG = "table_SysConfig";

        private const string DB_POLLING_TIME = "polling_Time";

        // The name of current class 
        private static readonly string _className =
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
        // Create a logger for use in this class
        private static readonly log4net.ILog _logger =
                    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Database ConnectionString.
        /// </summary>
        public string DBConnectionString { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_GIDUSED].
        /// </summary>
        public string STP_SAC_GIDUSED { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_ITEMSCREENED].
        /// </summary>
        public string STP_SAC_ITEMSCREENED { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_ITEMSCANNED].
        /// </summary>
        public string STP_SAC_ITEMSCANNED { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_ITEMSORTATIONEVENT].
        /// </summary>
        public string STP_SAC_ITEMSORTATIONEVENT { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_ITEMPROCEEDED].
        /// </summary>
        public string STP_SAC_ITEMPROCEEDED { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_ITEMREDIRECT].
        /// </summary>
        public string STP_SAC_ITEMREDIRECT { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_ITEMLOST].
        /// </summary>
        public string STP_SAC_ITEMLOST { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_ITEMTRACKING].
        /// </summary>
        public string STP_SAC_ITEMTRACKING { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_ITEMENCODINGREQUEST].
        /// </summary>
        public string STP_SAC_ITEMENCODINGREQUEST { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_GETBAGINFO]
        /// </summary>
        public string STP_SAC_GETBAGINFO { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_AIRPORTCODEFUNCALLOCINFORMATION].
        /// </summary>
        public string STP_SAC_AIRPORTCODEFUNCALLOCINFORMATION { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_BAGGAGEMEASUREMENTARRAY].
        /// </summary>
        public string STP_SAC_BAGGAGEMEASUREMENTARRAY { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_CARRIERALLOCINFORMATION].
        /// </summary>
        public string STP_SAC_CARRIERALLOCINFORMATION { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_FALLBACKTAGINFORMATION].
        /// </summary>
        public string STP_SAC_FALLBACKTAGINFORMATION { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_FOURPIERTAGINFORMATION].
        /// </summary>
        public string STP_SAC_FOURPIERTAGINFORMATION { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [STP_SAC_1500PINFORMATION].
        /// </summary>
        public string STP_SAC_1500PINFORMATION { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_COLLECTENTIREAFAI].
        /// </summary>
        public string STP_SAC_COLLECTENTIREAFAI { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_COLLECTENTIRECRAI].
        /// </summary>
        public string STP_SAC_COLLECTENTIRECRAI { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_COLLECTENTIREFBTI].
        /// </summary>
        public string STP_SAC_COLLECTENTIREFBTI { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_COLLECTENTIREFPTI].
        /// </summary>
        public string STP_SAC_COLLECTENTIREFPTI { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_COLLECTENTIRETPTI].
        /// </summary>
        public string STP_SAC_COLLECTENTIRETPTI { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_COLLECTCHANGEDCRAI].
        /// </summary>
        public string STP_SAC_COLLECTCHANGEDCRAI { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_COLLECTCHANGEDFBTI].
        /// </summary>
        public string STP_SAC_COLLECTCHANGEDFBTI { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_COLLECTCHANGEDFPTI].
        /// </summary>
        public string STP_SAC_COLLECTCHANGEDFPTI { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_COLLECTCHANGEDTPT].
        /// </summary>
        public string STP_SAC_COLLECTCHANGEDTPTI { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_COLLECTCHANGEDTABLES].
        /// </summary>
        public string STP_SAC_COLLECTCHANGEDTABLES { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_TAGDUPLICATIONCHECK].
        /// </summary>
        public string STP_SAC_TAGDUPLICATIONCHECK { get; set; }

        /// <summary>
        /// The name of DB StoredProcedure [stp_SAC_DUPLICATELICENSEPLATESTATUS].
        /// </summary>
        public string STP_SAC_DUPLICATELICENSEPLATESTATUS { get; set; }

        /// <summary>
        /// The name of DB Stored Procedure [stp_SAC_IRDValues]
        /// </summary>
        public string STP_SAC_IRDVALUES { get; set; }

        /// <summary>
        /// The name of DB Stored Procedure [stp_SAC_IRDValuesMES]
        /// </summary>
        public string STP_SAC_IRDVALUESMES { get; set; }

        /// <summary>
        /// The name of DB Stored Procedure [stp_SAC_GetAllocProp]
        /// </summary>
        public string STP_SAC_ALLOCPROP { get; set; }

        /// <summary>
        /// The name of DB Stored Procedure [stp_SAC_CheckMUAvailability]
        /// </summary>
        public string STP_SAC_CHECKMUAVAILABILITY { get; set; }

        /// <summary>
        /// The name of DB Stored Procedure [stp_SAC_CheckTagDest]
        /// </summary>
        public string STP_SAC_CHECKTAGDEST { get; set; }

        /// <summary>
        /// The AIRPORT_CODE_DESC.
        /// </summary>
        public string AIRPORT_CODE_DESC { get; set; }

        /// <summary>
        /// The FUNC_ALLOCATION_NOCR.
        /// </summary>
        public string FUNC_ALLOCATION_NOCR { get; set; }

        /// <summary>
        /// The FUNC_ALLOCATION_NOAL.
        /// </summary>
        public string FUNC_ALLOCATION_NOAL { get; set; }

        /// <summary>
        /// The FUNC_ALLOCATION_NOCR.
        /// </summary>
        public string FUNC_ALLOCATION_DUMP { get; set; }

        /// <summary>
        /// The FUNC_ALLOCATION_NORD.
        /// </summary>
        public string FUNC_ALLOCATION_NORD { get; set; }

        /// <summary>
        /// The TABLE OF CARRIER_LOG.
        /// </summary>
        public string TBL_CARRIER_LOG { get; set; }

        /// <summary>
        /// The TABLE OF FALLBACK_TAG_MAPPING.
        /// </summary>
        public string TBL_FALLBACK_TAG_MAPPING { get; set; }

        /// <summary>
        /// The TABLE OF FOUR_PIER_TAG_MAPPING.
        /// </summary>
        public string TBL_FOUR_PIER_TAG_MAPPING { get; set; }

        /// <summary>
        /// The TABLE OF FUNCTION_ALLOC_LIST.
        /// </summary>
        public string TBL_FUNCTION_ALLOC_LIST { get; set; }

        /// <summary>
        /// The TABLE OF SYS_CONFIG.
        /// </summary>
        public string TBL_SYS_CONFIG { get; set; }

        /// <summary>
        /// Time interval of polling from DB table [CHANGE_MONITORING].
        /// </summary>
        public long TablesPollingInterval { get; set; }

        #endregion

        #region Class Constructor & Destructor

        /// <summary>
        /// Class constructer.
        /// </summary>
        public PersistorParameters(XmlNode configSet, XmlNode telegramSet)
        {
            if (configSet == null)
                throw new Exception("Constractor parameter can not be null! Creating class object fail! " +
                        "<BHS.Engine.DataPersistor.Database.PersistorParametersConstructor()>");

            if (Init(ref configSet, ref telegramSet) == false)
                    throw new Exception("Instantiate class object failure! " +
                        "<BHS.Engine.DataPersistor.Database.PersistorParameters.Constructor()>");
        }

        /// <summary>
        /// Class destructer.
        /// </summary>
        ~PersistorParameters()
        {
            Dispose(false);
        }

        /// <summary>
        /// Class method to be called by class wrapper for release resources explicitly.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        // Dispose(bool disposing) executes in two distinct scenarios. If disposing equals true, 
        // the method has been called directly or indirectly by a user's code. Managed and 
        // unmanaged resources can be disposed.
        // If disposing equals false, the method has been called by the runtime from inside the 
        // finalizer and you should not reference other objects. Only unmanaged resources can be disposed.
        private void Dispose(bool disposing)
        {
            return;
        }

        #endregion

        #region Class Methods

        /// <summary>
        /// Class Initialization.
        /// </summary>
        /// <param name="configSet"></param>
        /// <param name="telegramSet"></param>
        /// <returns></returns>
        public bool Init(ref XmlNode configSet, ref XmlNode telegramSet)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            try
            {
                DBConnectionString = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_CONNECTION_STRING, string.Empty)).Trim();
                if ( DBConnectionString == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("Database ConnectionString setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_GIDUSED = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_GIDUSED, string.Empty)).Trim();
                if (STP_SAC_GIDUSED == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_GIDUSED + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_ITEMSCREENED = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_ITEMSCREENED, string.Empty)).Trim();
                if (STP_SAC_ITEMSCREENED == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_ITEMSCREENED + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_ITEMSCANNED = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_ITEMSCANNED, string.Empty)).Trim();
                if (STP_SAC_ITEMSCANNED == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_ITEMSCANNED + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_ITEMSORTATIONEVENT = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_ITEMSORTATIONEVENT, string.Empty)).Trim();
                if (STP_SAC_ITEMSORTATIONEVENT == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_ITEMSORTATIONEVENT + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_ITEMPROCEEDED = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_ITEMPROCEEDED, string.Empty)).Trim();
                if (STP_SAC_ITEMPROCEEDED == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_ITEMPROCEEDED + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_ITEMREDIRECT = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_ITEMREDIRECT, string.Empty)).Trim();
                if (STP_SAC_ITEMREDIRECT == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_ITEMREDIRECT + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_ITEMLOST = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_ITEMLOST, string.Empty)).Trim();
                if (STP_SAC_ITEMLOST == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_ITEMLOST + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_ITEMTRACKING = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_ITEMTRACKING, string.Empty)).Trim();
                if (STP_SAC_ITEMTRACKING == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_ITEMTRACKING + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_ITEMENCODINGREQUEST = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_ITEMENCODINGREQUEST, string.Empty)).Trim();
                if (STP_SAC_ITEMENCODINGREQUEST == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_ITEMENCODINGREQUEST + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_GETBAGINFO = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_GETBAGINFO, string.Empty)).Trim();
                if (STP_SAC_GETBAGINFO == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_GETBAGINFO + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_AIRPORTCODEFUNCALLOCINFORMATION = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_AIRPORTCODEFUNCALLOCINFORMATION, string.Empty)).Trim();
                if (STP_SAC_AIRPORTCODEFUNCALLOCINFORMATION == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_AIRPORTCODEFUNCALLOCINFORMATION + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_BAGGAGEMEASUREMENTARRAY = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_BAGGAGEMEASUREMENTARRAY, string.Empty)).Trim();
                if (STP_SAC_BAGGAGEMEASUREMENTARRAY == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_BAGGAGEMEASUREMENTARRAY + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_CARRIERALLOCINFORMATION = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_CARRIERALLOCINFORMATION, string.Empty)).Trim();
                if (STP_SAC_CARRIERALLOCINFORMATION == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_CARRIERALLOCINFORMATION + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_FALLBACKTAGINFORMATION = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_FALLBACKTAGINFORMATION, string.Empty)).Trim();
                if (STP_SAC_FALLBACKTAGINFORMATION == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_FALLBACKTAGINFORMATION + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_FOURPIERTAGINFORMATION = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_FOURPIERTAGINFORMATION, string.Empty)).Trim();
                if (STP_SAC_FOURPIERTAGINFORMATION == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_FOURPIERTAGINFORMATION + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_1500PINFORMATION = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_1500PINFORMATION, string.Empty)).Trim();
                if (STP_SAC_1500PINFORMATION == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_1500PINFORMATION + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_COLLECTENTIREAFAI = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_COLLECTENTIREAFAI, string.Empty)).Trim();
                if (STP_SAC_COLLECTENTIREAFAI == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_COLLECTENTIREAFAI + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_COLLECTENTIRECRAI = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_COLLECTENTIRECRAI, string.Empty)).Trim();
                if (STP_SAC_COLLECTENTIRECRAI == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_COLLECTENTIRECRAI + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_COLLECTENTIREFBTI = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_COLLECTENTIREFBTI, string.Empty)).Trim();
                if (STP_SAC_COLLECTENTIREFBTI == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_COLLECTENTIREFBTI + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_COLLECTENTIREFPTI = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_COLLECTENTIREFPTI, string.Empty)).Trim();
                if (STP_SAC_COLLECTENTIREFPTI == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_COLLECTENTIREFPTI + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_COLLECTCHANGEDCRAI = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_COLLECTCHANGEDCRAI, string.Empty)).Trim();
                if (STP_SAC_COLLECTCHANGEDCRAI == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_COLLECTCHANGEDCRAI + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_COLLECTCHANGEDFBTI = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_COLLECTCHANGEDFBTI, string.Empty)).Trim();
                if (STP_SAC_COLLECTCHANGEDFBTI == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_COLLECTCHANGEDFBTI + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_COLLECTCHANGEDFPTI = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_COLLECTCHANGEDFPTI, string.Empty)).Trim();
                if (STP_SAC_COLLECTCHANGEDFPTI == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_COLLECTCHANGEDFPTI + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_COLLECTCHANGEDTABLES = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_COLLECTCHANGEDTABLES, string.Empty)).Trim();
                if (STP_SAC_COLLECTCHANGEDTABLES == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_COLLECTCHANGEDTABLES + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }   

                STP_SAC_TAGDUPLICATIONCHECK = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_TAGDUPLICATIONCHECK, string.Empty)).Trim();
                if (STP_SAC_TAGDUPLICATIONCHECK == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_TAGDUPLICATIONCHECK + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }  

                STP_SAC_DUPLICATELICENSEPLATESTATUS = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_STP_SAC_DUPLICATELICENSEPLATESTATUS, string.Empty)).Trim();
                if (STP_SAC_DUPLICATELICENSEPLATESTATUS == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_DUPLICATELICENSEPLATESTATUS + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_IRDVALUES = (XMLConfig.GetSettingFromInnerText(
                    configSet, DB_STP_SAC_IRDVALUES, string.Empty)).Trim();
                if (STP_SAC_IRDVALUES == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_IRDVALUES + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_IRDVALUESMES = (XMLConfig.GetSettingFromInnerText(
                    configSet, DB_STP_SAC_IRDVALUESMES, string.Empty)).Trim();
                if (STP_SAC_IRDVALUESMES == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_IRDVALUESMES + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_ALLOCPROP = (XMLConfig.GetSettingFromInnerText(
                    configSet, DB_STP_SAC_ALLOCATIONPROPERTY, string.Empty)).Trim();
                if (STP_SAC_ALLOCPROP == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_ALLOCATIONPROPERTY + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_CHECKMUAVAILABILITY = (XMLConfig.GetSettingFromInnerText(
                    configSet, DB_STP_SAC_CHECKMUAVAILABILITY, string.Empty)).Trim();
                if (STP_SAC_CHECKMUAVAILABILITY == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_CHECKMUAVAILABILITY + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                STP_SAC_CHECKTAGDEST = (XMLConfig.GetSettingFromInnerText(
                    configSet, DB_STP_SAC_CHECKTAGDEST, string.Empty)).Trim();
                if (STP_SAC_CHECKTAGDEST == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_STP_SAC_CHECKTAGDEST + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                AIRPORT_CODE_DESC = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_AIRPORT_CODE_DESC, string.Empty)).Trim();
                if (AIRPORT_CODE_DESC == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_AIRPORT_CODE_DESC + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                FUNC_ALLOCATION_NOCR = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_FUNC_ALLOCATION_NOCR, string.Empty)).Trim();
                if (FUNC_ALLOCATION_NOCR == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_FUNC_ALLOCATION_NOCR + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                FUNC_ALLOCATION_NOAL = (XMLConfig.GetSettingFromInnerText(
                            configSet, DB_FUNC_ALLOCATION_NOAL, string.Empty)).Trim();
                if (FUNC_ALLOCATION_NOAL == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_FUNC_ALLOCATION_NOAL + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                FUNC_ALLOCATION_DUMP = (XMLConfig.GetSettingFromInnerText(
                           configSet, DB_FUNC_ALLOCATION_DUMPDISC, string.Empty)).Trim();
                if (FUNC_ALLOCATION_DUMP == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_FUNC_ALLOCATION_DUMPDISC + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                FUNC_ALLOCATION_NORD = (XMLConfig.GetSettingFromInnerText(
                           configSet, DB_FUNC_ALLOCATION_NORD, string.Empty)).Trim();
                if (FUNC_ALLOCATION_NORD == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_FUNC_ALLOCATION_NORD + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                TBL_CARRIER_LOG = (XMLConfig.GetSettingFromInnerText(
                           configSet, DB_TBL_CARRIER_LOG, string.Empty)).Trim();
                if (TBL_CARRIER_LOG == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_TBL_CARRIER_LOG + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                TBL_FALLBACK_TAG_MAPPING = (XMLConfig.GetSettingFromInnerText(
                           configSet, DB_TBL_FALLBACK_TAG_MAPPING, string.Empty)).Trim();
                if (TBL_FALLBACK_TAG_MAPPING == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_TBL_FALLBACK_TAG_MAPPING + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                TBL_FOUR_PIER_TAG_MAPPING = (XMLConfig.GetSettingFromInnerText(
                           configSet, DB_TBL_FOUR_PIER_TAG_MAPPING, string.Empty)).Trim();
                if (TBL_FOUR_PIER_TAG_MAPPING == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_TBL_FOUR_PIER_TAG_MAPPING + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                TBL_FUNCTION_ALLOC_LIST = (XMLConfig.GetSettingFromInnerText(
                           configSet, DB_TBL_FUNCTION_ALLOC_LIST, string.Empty)).Trim();
                if (TBL_FUNCTION_ALLOC_LIST == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_TBL_FUNCTION_ALLOC_LIST + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                TBL_SYS_CONFIG = (XMLConfig.GetSettingFromInnerText(
                           configSet, DB_TBL_SYS_CONFIG, string.Empty)).Trim();
                if (TBL_SYS_CONFIG == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<" + DB_TBL_SYS_CONFIG + "> setting can not be empty! <" + thisMethod + ">");

                    return false;
                }

                TablesPollingInterval = Convert.ToInt64(XMLConfig.GetSettingFromInnerText(configSet, DB_POLLING_TIME, "1000"));

                return true;

            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Initializing class setting is failed! <" + thisMethod + ">", ex);

                return false;
            }
        }

        #endregion

    }
}
