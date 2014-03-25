#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       IRD.cs
// Revision:      1.0 -   23 Aug 2013, By SC Leong
// =====================================================================================
//
#endregion

using System;
using System.Data;
using System.Data.SqlClient;
using PALS.Utilities;
using PALS.Telegrams;
using BHS;

namespace BHS.Engine.TCPClientChains.Messages.Handlers
{
    /// <summary>
    /// IRD message handler class
    /// </summary>
    public class IRD 
    {
        #region Class Fields and Properties Declaration

        // The name of current class 
        private static readonly string _className =
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
        // Create a logger for use in this class
        private static readonly log4net.ILog _logger =
                    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// GID_MSB
        /// </summary>
        public int GID_MSB { get; set; }

        /// <summary>
        /// GID_LSB
        /// </summary>
        public int GID_LSB { get; set; }

        /// <summary>
        /// PLC Index
        /// </summary>
        public int PLC_IDX { get; set; }

        /// <summary>
        /// DBPersistor Connection String value
        /// </summary>
        public string DBPersistorConnStr {  get;set; }

        /// <summary>
        /// DBPersistor STP IRD INFO
        /// </summary>
        public string DBPersistor_STP_IRDINFO { get;set; }

        /// <summary>
        /// DBPersistor STP Allocation Property
        /// </summary>
        public string DBPersistor_STP_ALLOCPROP { get; set; }

        /// <summary>
        /// DBPersistor STP Check MU Availability
        /// </summary>
        public string DBPersistor_STP_CheckMUAvailability { get; set; }

        /// <summary>
        /// DBPersistor STP Check Tag's Destination
        /// </summary>
        public string DBPersistor_STP_CheckTagDest { get; set; }

        /// <summary>
        /// Scanner's Status 
        /// </summary>
        public string ScannerStatus { get; set; }

        /// <summary>
        /// License Plate 1
        /// </summary>
        public string LicensePlate1 { get; set; }

        /// <summary>
        /// License Plate 2
        /// </summary>
        public string LicensePlate2 { get; set; }

        /// <summary>
        /// Current Location
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Destination 1
        /// </summary>
        private string Destination1 { get; set; }

        /// <summary>
        /// Destination 2
        /// </summary>
        private string Destination2 { get; set; }

        /// <summary>
        /// Type of sortation
        /// </summary>
        private string Reason { get; set; }

        /// <summary>
        /// IRD message format
        /// </summary>
        private TelegramFormat _messageFormat;

        /// <summary>
        /// Allocation Property
        /// </summary>
        private string Allocation_Property { get; set; }

        /// <summary>
        /// License Plate
        /// </summary>
        private string strLicensePlate { get; set; }

        /// <summary>
        /// Return IRD Type back to Main Class
        /// </summary>
        public string strType { get; set; }

        /// <summary>
        /// Return IRD Length back to Main Class
        /// </summary>
        public string strLen { get; set; }

        /// <summary>
        /// Return IRD Sequence No back to Main Class
        /// </summary>
        public string strSeq { get; set; }

        /// <summary>
        /// Return IRD Destination 1 back to Main Class
        /// </summary>
        public string strDestination1 { get; set; }

        /// <summary>
        /// Return IRD Destination 2 back to Main Class
        /// </summary>
        public string strDestination2 { get; set; }

        /// <summary>
        /// Return IRD Sortation Type back to Main Class
        /// </summary>
        public string strReason { get; set; }

        /// <summary>
        /// Return PLC Index back to Main Class
        /// </summary>
        public string strPLCIdx { get; set; }

        /// <summary>
        /// Mode of Item Redirect : @MES | @PRE MAIN LINE ATR | @PRE DESTINATION CHUTE
        /// </summary>
        public string strMode { get; set; }

        /// <summary>
        /// Carrier Code from MES. 
        /// </summary>
        public string Carrier { get; set; }

        /// <summary>
        /// Flight No from MES.
        /// </summary>
        public string Flight { get; set; }

        /// <summary>
        /// SDO from MES
        /// </summary>
        public string SDO { get; set; }

        /// <summary>
        /// Encoded Type from MES
        /// </summary>
        public string EncodedType { get; set; }

        /// <summary>
        /// Encoded destination from MES
        /// </summary>
        public string EncodedDestination { get; set; }

        /// <summary>
        /// Item Sortation Event : Destination
        /// </summary>
        public string Destination { get; set; }

        #endregion

        #region Class Constructor, Dispose, & Destructor

            public IRD(TelegramFormat MessageFormat)
            {
                if (MessageFormat == null)
                throw new Exception("Message format can not be null! Creating IRD class object failure! " +
                    "<BHS.Engine.TCPClientChains.Messages.Handlers.IRD.Constructor()>");

                _messageFormat = MessageFormat;
                GID_MSB = 0;
                GID_LSB = 0;
                PLC_IDX = 0;
                DBPersistorConnStr = string.Empty;
                DBPersistor_STP_IRDINFO = string.Empty;
                ScannerStatus = string.Empty;
                LicensePlate1 = string.Empty;
                LicensePlate2 = string.Empty;
                Destination1 = string.Empty;
                Destination2 = string.Empty;
                Reason = string.Empty;
                Allocation_Property = string.Empty;
                Carrier = string.Empty;
                Flight = string.Empty;
                SDO = string.Empty;
                EncodedType = string.Empty;
                Destination = string.Empty;
                EncodedDestination = string.Empty;

            }
      
        #endregion

        #region Class Method Declaration.

        public Telegram ConstructIRDMessage()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            Telegram msg = null;

            try
            {
              //<telegram alias="IRD" name="Item_Redirect_Message" sequence="True" acknowledge="False">
              //  <field name="Type" offset="0" length="4" default="48,48,48,55"/>
              //  <field name="Length" offset="4" length="4" default="48,48,50,52"/>
              //  <field name="Sequence" offset="8" length="4" default="?"/>
              //  <field name="GID_MSB" offset="12" length="1" default="?"/>
              //  <field name="GID_LSB" offset="13" length="4" default="?"/>
              //  <field name="DEST_1" offset="17" length="2" default="?"/>
              //  <field name="DEST_2" offset="19" length="2" default="?"/>
              //  <field name="TYPE" offset="21" length="1" default="?"/>
              //  <field name="PLC_IDX" offset="22" length="1" default="?"/>
              //</telegram>
                 
                byte[] data = null;
                msg = new Telegram(ref data);
                msg.Format = _messageFormat;

                bool temp ;
                byte[] type, len, seq, gid_msb,gid_lsb, dest_1, dest_2, reason, plc_idx;

                // Only check priority of different tags if bag was readed as multiple tags with different tag type.
                if (ScannerStatus == "3")
                {
                    // To check validity of Fallback Tag. Original Tag value will be return from this function if it is not a fallback tag
                    LicensePlate1 = CheckValidityFallbackTag(LicensePlate1);
                    LicensePlate2 = CheckValidityFallbackTag(LicensePlate2);

                    // To prioritize the license plate according to the tag type.
                    // 1) 4 digits tag has the highest priority than any other tag
                    // 2) Fallback tag is the second highest
                    // 3) IATA tag is the tag with lowest priority. 
                    // If a bag detected with 1 IATA Tag & Fallback Tag, Fallback tag will be used for sortation. 
                    // If a bag detected with 1 IATA tag & 4 digits sortation tag, 4 digits sortation tag will be used for sortation.
                    int intFirstIntLP1, intFirstIntLP2;
                    intFirstIntLP1 = LicensePlate1.Trim() == string.Empty ? 99 : LicensePlate1.Trim().Length == 4 ? 10 : int.Parse(LicensePlate1.Trim().Substring(0, 1));
                    intFirstIntLP2 = LicensePlate2.Trim() == string.Empty ? 99 : LicensePlate2.Trim().Length == 4 ? 10 : int.Parse(LicensePlate2.Trim().Substring(0, 1));

                    // Identify the priority of tags. 1 - 4 Pier Tag, 2 - Fallback Tag, 3 - IATA Tag =========================
                    intFirstIntLP1 = CompareLC(intFirstIntLP1);
                    intFirstIntLP2 = CompareLC(intFirstIntLP2);

                    if (intFirstIntLP1 < intFirstIntLP2)
                    {
                        strLicensePlate = LicensePlate1;
                    }
                    else if (intFirstIntLP1 > intFirstIntLP2)
                    {
                        strLicensePlate = LicensePlate2;
                    }
                    else if (intFirstIntLP1 == intFirstIntLP2)
                    {
                        strLicensePlate = LicensePlate1;
                    }
                    // -------------------------------------------------------------------------------------------------------------------------------
                }
                else if (ScannerStatus == "7")     //  Multiple License Plate with same tag type
                {
                    // based on only the first license plate to identify the tag type
                    string strTagType = string.Empty;
                    if ((LicensePlate1.Trim().Substring(0, 1) == "1") && (LicensePlate1.Trim().Length ==  10))
                    {
                        strTagType = "1";   // Fallback Tag
                    }
                    else if (LicensePlate1.Trim().Length ==  4)
                    {
                        strTagType = "2";   // 4 digits sortation
                    }
                    else if ((LicensePlate1.Trim().Substring(0, 1) != "1") && (LicensePlate1.Trim().Length ==  10))
                    {
                        strTagType = "3";   // Normal IATA Tag
                    }

                    // return only the first License Plate for sortation, because both heading to the same destination
                    // if the return value is not empty ; assign the Scanner Status = 1 (Read Ok with Single Tag) 
                    // if the return value is empty ; use the same Scanner Status 
                    strLicensePlate = CheckDestination(strTagType, LicensePlate1, LicensePlate2);
                    if (strLicensePlate != string.Empty)
                    {
                        ScannerStatus = "1";
                    }
                }
                else
                {
                    strLicensePlate = LicensePlate1;
                }

                 // Identify Bag's allocation property (RUSH / LATE / EARLY / TOO EARLY), only if it is a normal IATA Tag (not Fallback Tag & not 4 Digits Sortation Tag)
                Allocation_Property = string.Empty;
                if (ScannerStatus.Contains("1") || ScannerStatus.Contains("3") || ScannerStatus == string.Empty || (ScannerStatus.Contains("7") && strLicensePlate != string.Empty))
                {
                    // only IATA Tag need to check for Allocation Property
                    if (strLicensePlate != string.Empty)
                    {
                        if (strLicensePlate.Trim().Substring(0, 1) != "1" && strLicensePlate.Trim().Length != 4)
                        {
                            Allocation_Property = Utilities.AllocationProperty(strLicensePlate, Carrier, Flight, SDO, DBPersistorConnStr, DBPersistor_STP_ALLOCPROP);
                        }
                    }

                }

                 if (strMode == "ATR")
                 {
                     GetIRDValues();
                 }
                 else if (strMode == "MES")
                 {
                     GetIRDValuesMES();
                 }
                 else if (strMode == "ISE")
                 {
                     GetIRDValuesISE();
                 }
                
                #region Telegram : Type 

                type = msg.GetFieldDefaultValue("Type");
                // return Message Type back to Main Telegram for logging purpose
                strType = Functions.ConvertByteArrayToString(type, -1, HexToStrMode.ToAscString).Trim();

                temp = msg.SetFieldActualValue("Type", ref type, PALS.Telegrams.Common.PaddingRule.Right);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("IRD Message  \"Type\" field value assignment is failed! <"+ thisMethod + ">");
                    return null;
                }
                #endregion 

                #region Telegram : Length 

                len = msg.GetFieldDefaultValue("Length");
                strLen = Functions.ConvertByteArrayToString(len, -1, HexToStrMode.ToAscString).Trim();

                temp = msg.SetFieldActualValue("Length", ref len, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("IRD Message  \"Length\" field value assignment is failed! <"+ thisMethod + ">");
                    return null;
                }
                #endregion 

                #region Telegram : Sequence 
                int fieldLen = _messageFormat.Field("Sequence").Length;
                seq = new byte[fieldLen];
                if (msg.Format.NeedNewSequence == true)
                {
                    long newSeq = SequenceNo.NewSequenceNo1;
                    strSeq = newSeq.ToString();

                    // Convert Decimal value to Hex Value
                    string HexValue = newSeq.ToString("X");

                    seq = Utilities.ToByteArray(HexValue , fieldLen, false);
                 }
                temp = msg.SetFieldActualValue("Sequence", ref seq , PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                    {
                        _logger.Error("IRD Message \"Sequence\" field value assignment is failed! <" + thisMethod + ">");
                        return null;
                    }
                }
                # endregion 

                # region Telegram : GID_MSB 
                int intGID_MSB_FIELDLEN = _messageFormat.Field("GID_MSB").Length;
                
                gid_msb = Utilities.ToByteArray(GID_MSB.ToString("X"), intGID_MSB_FIELDLEN, false);
                temp = msg.SetFieldActualValue("GID_MSB", ref gid_msb, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                    {
                        _logger.Error("IRD Message \"GID_MSB\" field value assignment is failed! <" + thisMethod + ">");
                        return null;
                    }
                }
                # endregion 

                # region Telegram : GID_LSB
                int intGID_LSB_FIELDLEN = _messageFormat.Field("GID_LSB").Length;
                gid_lsb = Utilities.ToByteArray(GID_LSB.ToString("X"), intGID_LSB_FIELDLEN, false);
                temp = msg.SetFieldActualValue("GID_LSB", ref gid_lsb, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                    {
                        _logger.Error("IRD Message \"GID_LSB\" field value assignment is failed! <" + thisMethod + ">");
                        return null;
                    }
                }
                # endregion 
                
                # region Telegram : Destination 1
                int intDest1 = int.Parse(Destination1==string.Empty ? "0" : Destination1);
                strDestination1 = (Destination1 == string.Empty ? "0" : Destination1); 
                string strDest1 = intDest1.ToString("X");
                fieldLen = _messageFormat.Field("DEST_1").Length;
                dest_1 = new byte[fieldLen];

                dest_1 = Utilities.ToByteArray(strDest1, fieldLen, false);
                temp = msg.SetFieldActualValue("DEST_1", ref dest_1, PALS.Telegrams.Common.PaddingRule.Right);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("IRD Message \"DEST_1\" field value assignment is failed!<" + thisMethod + ">");
                    return null;
                }
                # endregion 

                # region Telegram : Destination 2 
                int intDest2 = int.Parse(Destination2 == string.Empty ? "0" : Destination2);
                strDestination2 = Destination2 == string.Empty ? "0" : Destination2;
                string strDest2 = intDest2.ToString("X");
                fieldLen = _messageFormat.Field("DEST_2").Length;
                dest_2 = new byte[fieldLen];

                dest_2 = Utilities.ToByteArray(strDest2, fieldLen, false);
                temp = msg.SetFieldActualValue("DEST_2", ref dest_2, PALS.Telegrams.Common.PaddingRule.Right);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("IRD Message \"DEST_2\" field value assignment is failed!<" + thisMethod + ">");
                    return null;
                }
                # endregion 

                # region Telegram : Type 
                fieldLen = _messageFormat.Field("TYPE").Length;
                reason = new byte[fieldLen];
                int intReason = int.Parse(Reason==string.Empty ? "1" : Reason);
                strReason = (Reason == string.Empty ? "1" : Reason);
                string strHexValue = intReason.ToString("X");
                strReason = Reason;

                reason = Utilities.ToByteArray(strHexValue, fieldLen, false);
                temp = msg.SetFieldActualValue("TYPE", ref reason, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                    {
                        _logger.Error("IRD Message \"TYPE\" field value assignment is failed! <" + thisMethod + ">");
                        return null;
                    }
                }
                #endregion 

                #region Telegram : PLC Index 
                fieldLen = _messageFormat.Field("PLC_IDX").Length;
                plc_idx = new byte[fieldLen];
                string strHexPlcIndex = PLC_IDX.ToString("X");

                plc_idx = Utilities.ToByteArray(strHexPlcIndex, fieldLen, false);
                temp = msg.SetFieldActualValue("PLC_IDX", ref plc_idx, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                    {
                        _logger.Error("IRD Message \"PLC_IDX\" field value assignment is failed! <" + thisMethod + ">");
                        return null;
                    }
                }
                #endregion 

                return msg;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Constructing IRD message is failed! <" +  thisMethod + ">", ex);

                return null;
            }
        }

        /// <summary>
        /// Get IRD value @ Pre-Main Line ATR
        /// </summary>
        private void GetIRDValues ()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;

            try
            {
                   
                    sqlConn = new SqlConnection(DBPersistorConnStr);
                    sqlCmd = new SqlCommand(DBPersistor_STP_IRDINFO, sqlConn);
                    sqlCmd.CommandType = CommandType.StoredProcedure;
                    
                    sqlCmd.Parameters.AddWithValue("@SCANNER_STATUS", ScannerStatus);
                    sqlCmd.Parameters.AddWithValue("@LICENSE_PLATE", strLicensePlate);
                    sqlCmd.Parameters.AddWithValue("@ALLOCATION_PROP", Allocation_Property);
                   
                    int intCount = 1;
                    sqlConn.Open();
                    SqlDataReader sqlReader = sqlCmd.ExecuteReader();
                    while (sqlReader.Read())
                    {
                        Reason = sqlReader["REASON"].ToString();

                        if (intCount == 1)
                        {
                            Destination1 = CheckMUAvailability(sqlReader["DESTINATION"].ToString(), Location, Reason).Trim();
                        }
                        else
                        {
                            Destination2 = CheckMUAvailability(sqlReader["DESTINATION"].ToString(), Location, Reason).Trim();
                        }
                        intCount += 1;
                    }
                  
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Collecting of IRD values FAILURE !<" + thisMethod + ">", ex);

                return;
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
            }

        }

        /// <summary>
        /// Get IRD value @ MES
        /// </summary>
        private void GetIRDValuesMES()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;

            try
            {

                sqlConn = new SqlConnection(DBPersistorConnStr);
                sqlCmd = new SqlCommand(DBPersistor_STP_IRDINFO, sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                sqlCmd.Parameters.AddWithValue("@ENCODED_TYPE", EncodedType);
                sqlCmd.Parameters.AddWithValue("@LICENSE_PLATE", strLicensePlate);
                sqlCmd.Parameters.AddWithValue("@AIRLINE", Carrier);
                sqlCmd.Parameters.AddWithValue("@FLIGHT_NUMBER", Flight);
                sqlCmd.Parameters.AddWithValue("@SDO", SDO);
                sqlCmd.Parameters.AddWithValue("@ALLOCATION_PROP", Allocation_Property);
                sqlCmd.Parameters.AddWithValue("@SORTDESTINATION", EncodedDestination); 

                int intCount = 1;
               sqlConn.Open();
               SqlDataReader sqlReader = sqlCmd.ExecuteReader();
               while (sqlReader.Read())
               {
                   Reason = sqlReader["REASON"].ToString();

                   if (intCount == 1)
                   {
                       Destination1 = CheckMUAvailability(sqlReader["DESTINATION"].ToString(), Location, Reason).Trim();
                   }
                   else
                   {
                       Destination2 = CheckMUAvailability(sqlReader["DESTINATION"].ToString(), Location, Reason).Trim();
                   }
                   intCount += 1;
               }

            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Collecting of IRD values FAILURE !<" + thisMethod + ">", ex);

                return;
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
            }

        }

        /// <summary>
        /// Get IRD value @ ISE
        /// </summary>
        private void GetIRDValuesISE()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            
            try
            {
                // Inducted @ MES, follow MES Sortation Rules to determind the next destination 
                if (EncodedType != string.Empty && EncodedType != null)
                {
                    GetIRDValuesMES();
                }
                // Inducted @ pre-Main Line ATR, follow normal Sortation Rules to determind the next destination
                else
                {
                    GetIRDValues();
                }
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Collecting of IRD values FAILURE !<" + thisMethod + ">", ex);

                return;
            }
            
        }

        /// <summary>
        /// Check MU Availability
        /// </summary>
        private string CheckMUAvailability(string strDestination, string strLocation, string strReason)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;

            try
            {

                sqlConn = new SqlConnection(DBPersistorConnStr);
                sqlCmd = new SqlCommand(DBPersistor_STP_CheckMUAvailability, sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                sqlCmd.Parameters.AddWithValue("@DESTINATION", strDestination);
                sqlCmd.Parameters.AddWithValue("@DEST_DESCR", string.Empty);
                sqlCmd.Parameters.AddWithValue("@LOCATION", strLocation);
                sqlCmd.Parameters.AddWithValue("@ORGREASON", strReason);
                sqlCmd.Parameters.AddWithValue("@ORGREASON_DESCR", string.Empty);

                sqlCmd.Parameters.Add("@RETVAL", SqlDbType.VarChar, 4);
                sqlCmd.Parameters["@RETVAL"].Direction = ParameterDirection.Output;

                sqlCmd.Parameters.Add("@REASON", SqlDbType.VarChar, 4);
                sqlCmd.Parameters["@REASON"].Direction = ParameterDirection.Output;

                sqlCmd.Parameters.Add("@RETVAL_DESCR", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@RETVAL_DESCR"].Direction = ParameterDirection.Output;

                sqlCmd.Parameters.Add("@REASON_DESCR", SqlDbType.VarChar, 100);
                sqlCmd.Parameters["@REASON_DESCR"].Direction = ParameterDirection.Output;

                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();

                Reason = sqlCmd.Parameters["@REASON"].Value.ToString();

                return sqlCmd.Parameters["@RETVAL"].Value.ToString();
                
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Checking on MU Availability procedures is FAILURE !<" + thisMethod + ">", ex);

                return string.Empty;
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
            }

        }

        /// <summary>
        /// Compare 1st & 2nd License Plate to select the 1 with higher privillege
        /// </summary>
        /// <param name="intFirstLCNo"></param>
        /// <returns></returns>
        private int CompareLC(int intFirstLCNo)
        {
            int intRet = 0;

            if (intFirstLCNo == 99)
            {
                intRet = 99;
            }
            else
            {
                switch (intFirstLCNo)
                {
                    case 0: case 2: case 3: case 4: case 5: case 6: case 7: case 8: case 9:   // normal IATA tag
                        intRet = 3;
                        break;
                    case 1 :  //  fallback tag
                        intRet = 2;
                        break;
                    case 10:  //  4 digits sortation tag
                        intRet = 1;
                        break;
                }
            }

            return intRet;
        }

        /// <summary>
        /// To check validity of Fallback Tag based on Airport Location Code
        /// </summary>
        /// <param name="licensePlate"></param>
        /// <returns></returns>
        private string CheckValidityFallbackTag(string licensePlate)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            try
            {
                if (licensePlate.ToString().Trim().Substring(0,1) == "1")
                {
                    if (licensePlate.Trim().Substring(4, 4) != Common.AirportLocationCode)
                    {
                        licensePlate = string.Empty;
                    }
                }

                return licensePlate;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Checking on validity of Fallback Tag is FAILURE!", ex);

                return string.Empty;
            }
            
        }

        /// <summary>
        /// To verify both Tags are heading to the same destination
        /// </summary>
        /// <param name="type"></param>
        /// <param name="LC1"></param>
        /// <param name="LC2"></param>
        /// <returns></returns>
        private string CheckDestination(string type ,string LC1, string LC2)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;

            try
            {

                sqlConn = new SqlConnection(DBPersistorConnStr);
                sqlCmd = new SqlCommand(DBPersistor_STP_CheckTagDest, sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                sqlCmd.Parameters.AddWithValue("@TAG_TYPE", type);
                sqlCmd.Parameters.AddWithValue("@LICENSE_PLATE1", LC1);
                sqlCmd.Parameters.AddWithValue("@LICENSE_PLATE2", LC2);

                sqlCmd.Parameters.Add("@RETVAL", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@RETVAL"].Direction = ParameterDirection.Output;

                //sqlCmd.Parameters.Add("@RETREASON", SqlDbType.VarChar, 100);
                //sqlCmd.Parameters["@RETREASON"].Direction = ParameterDirection.Output;

                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();

                return sqlCmd.Parameters["@RETVAL"].Value.ToString();

            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Checking on Tag's Destination procedures is FAILURE !<" + thisMethod + ">", ex);

                return string.Empty;
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
            }
        }

        #endregion

    }
}
