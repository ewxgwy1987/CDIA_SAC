#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       ISC.cs
// Revision:      1.0 -   05 Oct 2009, By HS Chia
// =====================================================================================
//
#endregion

using System;
using System.Data;
using System.Data.SqlClient;
using PALS.Utilities;
using PALS.Telegrams;
using System.Collections.Generic;
using System.Collections;

namespace BHS.Engine.TCPClientChains.Messages.Handlers
{
    /// <summary>
    /// ISC message handler class
    /// </summary>
    public class ISC : AbstractMessageHandler, IDisposable
    {
        #region Class Fields and Properties Declaration

        // The name of current class 
        private static readonly string _className =
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
        // Create a logger for use in this class
        private static readonly log4net.ILog _logger =
                    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// DLPS Max Records.
        /// </summary>
        public int DLPSMaxRecords { get; set; }

        /// <summary>
        /// DLPS Status Single
        /// </summary>
        public string DLPSStatusSingle { get; set; }

        /// <summary>
        /// DLPS Status Duplicate
        /// </summary>
        public string DLPSStatusDuplicate { get; set; }

        /// <summary>
        /// DLPS Subscriber ATRs
        /// </summary>
        public List<string> DLPSSubscriberATR { get; set; }

        /// <summary>
        /// Enable Sending DLPS from SortEngn or not.
        /// </summary>
        public bool EnableSendingDLPS { get; set; }

        /// <summary>
        /// Enable Sending IRD from SAC Engine or not 
        /// </summary>
        public bool EnableSendingIRD { get; set; }

        /// <summary>
        /// IRD Subscriber ATRs
        /// </summary>
        public List<string> IRDSubscriber { get; set; }

        /// <summary>
        /// DLPS Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_DLPS { get; set; }

        /// <summary>
        /// IRD Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_IRD { get; set; }

        /// <summary>
        /// Event will be raised when message handler need send a message to external.
        /// </summary>
        public event EventHandler<MessageSendRequestEventArgs> OnSendRequest;

        #endregion

        #region Class Constructor, Dispose, & Destructor

        /// <summary>
        /// Class constructer.
        /// </summary>
        public ISC()
        {
            if (!Init())
                throw new Exception("Creating class " + _className + " object failed! <Constructor()>");
        }

        /// <summary>
        /// Class destructer.
        /// </summary>
        ~ISC()
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
            // Release managed & unmanaged resources...
            if (disposing)
            {
                if (_logger.IsInfoEnabled)
                    _logger.Info("Class:[" + _className + "] object has been destroyed. <" + _className + ".Dispose()>");
            }
        }

        #endregion

        #region Class Method Declaration.

        /// <summary>
        /// Init() method.
        /// </summary>
        /// <returns></returns>
        protected bool Init()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            if (_logger.IsInfoEnabled)
                _logger.Info("Class:[" + _className + "] object is initializing... <" + thisMethod + ">");

            // ========================================================
            // Add initialization task code here.
               
            // ========================================================

            if (_logger.IsInfoEnabled)
                _logger.Info("Class:[" + _className + "] object has been initialized. <" + thisMethod + ">");

            return true;
        }

        /// <summary>
        /// Overridden class method for message handling.
        /// </summary>
        /// <param name="msgInfo"></param>
        protected override void MessageHandling(IncomingMessageInfo msgInfo)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            string inMsgSender, inMsgReceiver, channelName, type, length, sequence, gid, gid_msb, gid_lsb, location, lic_1, lic_2,  scn_sts, plc_idx;
            string strDest1, strDest2, strReason;

            List<int> scn_head;
            Telegram message;
            DateTime Perf = DateTime.Now;

            if (msgInfo == null)
                return;

            inMsgSender = msgInfo.Sender;
            inMsgReceiver = msgInfo.Receiver;
            channelName = msgInfo.ChannelName;
            message = msgInfo.Message;

            if (message == null)
                return;

            try
            {
              // <telegram alias="ISC" name="Item_Scanned_Message" sequence="True" acknowledge="False">
              //  <field name="Type" offset="0" length="4" default="48,48,48,53"/>
              //  <field name="Length" offset="4" length="4" default="48,48,53,49"/>
              //  <field name="Sequence" offset="8" length="4" default="?"/>
              //  <field name="GID_MSB" offset="12" length="1" default="?"/>
              //  <field name="GID_LSB" offset="13" length="4" default="?"/>
              //  <field name="LOCATION" offset="17" length="10" default="?"/>
              //  <field name="LIC_1" offset="27" length="10" default="?"/>
              //  <field name="LIC_2" offset="37" length="10" default="?"/>
              //  <field name="SCN_HEAD" offset="47" length="2" default="?"/>
              //  <field name="SCN_STS" offset="49" length="1" default="?"/>
              //  <field name="PLC_IDX" offset="50" length="1" default="?"/>
              //</telegram>

                type = string.Empty;
                length = string.Empty;
                sequence = string.Empty;
                gid = string.Empty;
                gid_msb = string.Empty;
                gid_lsb = string.Empty;
                location = string.Empty;
                lic_1 = string.Empty;
                lic_2 = string.Empty;
                scn_head = null;
                scn_sts = string.Empty;
                plc_idx = string.Empty;
                
                // 1. Decode message.
                MessageDecoding(message, out type, out length, out sequence, out gid_msb, out gid_lsb, out gid, out location, out lic_1, out lic_2, out scn_head, out scn_sts, out plc_idx);

                // 2. Log message data into database.
                LogMessageIntoDatabase(inMsgSender, inMsgReceiver, location, gid, lic_1, lic_2, scn_head, scn_sts, plc_idx);

                // 3. Log message data into log file.
                int[] head = scn_head.ToArray();
                string headString = string.Empty;

                foreach (int temp in head)
                {
                    headString = headString + temp;
                }

                if (_logger.IsInfoEnabled)
                    _logger.Info("[Channel:" + channelName +
                        "] <- [MSG(" + message.Format.AliasName +
                        "): Type=" + Functions.InvisibleCharacterFormating(ref type) +
                        ", Length=" + Functions.InvisibleCharacterFormating(ref length) +
                        ", Sequence=" + Functions.InvisibleCharacterFormating(ref sequence) +
                        ", GID=" + Functions.InvisibleCharacterFormating(ref gid) +
                        ", Location=" + Functions.InvisibleCharacterFormating(ref location) +
                        ", License Plate 1=" + Functions.InvisibleCharacterFormating(ref lic_1) +
                        ", License Plate 2=" + Functions.InvisibleCharacterFormating(ref lic_2) +
                        ", Scan Head=" + Functions.InvisibleCharacterFormating(ref headString) +
                        ", Scan Status=" + Functions.InvisibleCharacterFormating(ref scn_sts) +
                        ", PLC Index=" + Functions.InvisibleCharacterFormating(ref plc_idx) +
                        "]. (Perf:" + DateTime.Now.Subtract(Perf).TotalMilliseconds.ToString() +
                        "ms). <" + thisMethod + ">");

                # region Contruct IRD Telegram

                 //4. Response to PLC with Item Redirect telegram
                //      4.1.1 Only feature for sending IRD is enabled 
                //      4.1.2 Only location subscribed for responding IRD
                if (EnableSendingIRD)
                {
                    Perf = DateTime.Now;

                    if (IRDSubscriber.Contains(location))
                    {
                        IRD Ird = new IRD(MessageFormat_IRD);

                        Ird.ScannerStatus = scn_sts; 
                        Ird.GID_MSB = int.Parse(gid_msb);
                        Ird.GID_LSB = int.Parse(gid_lsb);
                        Ird.PLC_IDX = int.Parse(plc_idx);
                        Ird.LicensePlate1 = lic_1;
                        Ird.LicensePlate2 = lic_2;
                        Ird.Carrier = string.Empty;
                        Ird.Flight = string.Empty;
                        Ird.SDO = string.Empty;
                        Ird.EncodedType = string.Empty;
                        Ird.strMode = "ATR";
                        Ird.DBPersistorConnStr = DBPersistor.ClassParameters.DBConnectionString;
                        Ird.DBPersistor_STP_IRDINFO = DBPersistor.ClassParameters.STP_SAC_IRDVALUES;
                        Ird.DBPersistor_STP_ALLOCPROP = DBPersistor.ClassParameters.STP_SAC_ALLOCPROP;
                        Ird.DBPersistor_STP_CheckMUAvailability = DBPersistor.ClassParameters.STP_SAC_CHECKMUAVAILABILITY;
                        Ird.DBPersistor_STP_CheckTagDest = DBPersistor.ClassParameters.STP_SAC_CHECKTAGDEST;
                        Ird.Location = location;
                        Ird.EncodedDestination = string.Empty;
                        
                         //Construct IRD Message
                        Telegram msg = Ird.ConstructIRDMessage();

                        if (msg != null)
                        {
                            EventHandler<MessageSendRequestEventArgs> temp = OnSendRequest;
                            if (temp != null)
                            {
                                // Raise on send out going message
                                temp(this, new MessageSendRequestEventArgs(inMsgReceiver, inMsgSender, channelName, msg));

                                // Assign IRD values into local variables
                                type = Ird.strType;
                                length = Ird.strLen;
                                strDest1 = Ird.strDestination1;
                                strDest2 = Ird.strDestination2;
                                strReason = Ird.strReason;

                                // Log Sent Data into historical table
                                LogIRDIntoDatabase(inMsgSender, inMsgReceiver, gid, strDest1, strDest2, strReason, plc_idx); 

                                // Log into log file 
                                if (_logger.IsInfoEnabled)
                                    _logger.Info("[Channel:" + channelName +
                                        "] -> [MSG(" + msg.Format.AliasName +
                                        "): Type=" + Functions.InvisibleCharacterFormating(ref type) +
                                        ", Length=" + Functions.InvisibleCharacterFormating(ref length) +
                                        ", Sequence=" + Functions.InvisibleCharacterFormating(ref sequence) +
                                        ", GID=" + Functions.InvisibleCharacterFormating(ref gid) +
                                        ", Destination 1=" + Functions.InvisibleCharacterFormating(ref strDest1) +
                                        ", Destination 2=" + Functions.InvisibleCharacterFormating(ref strDest2) +
                                        ", Reason = " + Functions.InvisibleCharacterFormating(ref strReason) +
                                        ", PLC Index = " + Functions.InvisibleCharacterFormating(ref plc_idx) + 
                                        "]. (Perf:" + DateTime.Now.Subtract(Perf).TotalMilliseconds.ToString() +
                                        "ms). <" + thisMethod + ">");
                            }
                        }
                    }

                }
                # endregion

                #region Checking on Duplicate License Plate @ ATR  #not using for this project
                //if (EnableSendingDLPS)
                //{
                //    int duplicateCount = 0;
                //    string dlpsStatus = string.Empty;

                //    // If the Scanner ID is regiscter in xml then do following steps.
                //    if (DLPSSubscriberATR.Contains(scannerID))
                //    {
                //        // If the lincesePlate1 is IATA Interline or IATA Fallback tag
                //        int tagLength = licensePlate1.Length;

                //        if (tagLength == 10)
                //        {
                //            // Check database records whether have this tag number or not
                //            // if return database from database is 0 mean Single tag not a duplicate tag
                //            // if return more than 0, mean duplicate tag.
                //            DBPersistor.DLPSTagDuplicationCheck(gid, licensePlate1, DLPSMaxRecords, out duplicateCount);

                //            if (duplicateCount == 0)
                //            {
                //                // Single
                //                dlpsStatus = DLPSStatusSingle;
                //            }
                //            else
                //            {
                //                // Duplicate
                //                dlpsStatus = DLPSStatusDuplicate;
                //            }


                //            // Contract DLPS msg and sending out
                //            DLPS hdlrDLPS = new DLPS(MessageFormat_DLPS);

                //            hdlrDLPS.Status = dlpsStatus;
                //            hdlrDLPS.GID = gid;
                //            hdlrDLPS.LicensePlate = licensePlate1;

                //            Telegram msg = hdlrDLPS.ConstructDLPSMessage();

                //            if (msg != null)
                //            {
                //                // Copy to a temporary variable to be thread-safe.
                //                EventHandler<MessageSendRequestEventArgs> temp = OnSendRequest;
                //                // Event could be null if there are no subscribers, so check it before raise event
                //                if (temp != null)
                //                {
                //                    // Raise OnSendRequest event to send outgoing message.
                //                    temp(this, new MessageSendRequestEventArgs(inMsgReceiver, inMsgSender, channelName, msg));

                //                    //// Log sent data into historical DB table.
                //                    DBPersistor.SentDLPSLogging(inMsgReceiver, inMsgSender, gid, licensePlate1, dlpsStatus);

                //                    if (_logger.IsInfoEnabled)
                //                        _logger.Info("[Channel:" + channelName +
                //                            "] -> [MSG(" + message.Format.AliasName +
                //                            "): Type=" + Functions.InvisibleCharacterFormating(ref type) +
                //                            ", Length=" + Functions.InvisibleCharacterFormating(ref length) +
                //                            ", Sequence=" + Functions.InvisibleCharacterFormating(ref sequence) +
                //                            ", GID=" + Functions.InvisibleCharacterFormating(ref gid) +
                //                            ", License Plate=" + Functions.InvisibleCharacterFormating(ref licensePlate1) +
                //                            ", Status=" + Functions.InvisibleCharacterFormating(ref dlpsStatus) +
                //                            "]. (Perf:" + DateTime.Now.Subtract(Perf).Milliseconds.ToString() +
                //                            "ms). <" + thisMethod + ">");
                //                }
                //            }
                //        }
                //    }
                //}
                # endregion 

            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Message handling is failed! <" + thisMethod + ">", ex);
            }
        }

        /// <summary>
        /// Message Decoding : Old Method
        /// </summary>
        //private void MessageDecoding(Telegram message, out string type, out string length,
        //                out string sequence, out string subsystem, out string source, out string gid,
        //                out string lincesePlate1, out string lincesePlate2, out List<int> scannerHead, 
        //                out string scannerID, out string scannerStatus)
        //{
        //    string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
        //    byte[] byteArray = new byte[8];

        //    //<telegram alias="ISC" name="Item_Scanned_Message" sequence="True" acknowledge="False">
        //    //  <field name="Type" offset="0" length="4" default="48,48,48,53"/>
        //    //  <field name="Length" offset="4" length="4" default="48,48,56,54"/>
        //    //  <field name="Sequence" offset="8" length="4" default="?"/>
        //    //  <field name="SubSystem" offset="12" length="10" default="?"/>
        //    //  <field name="Source" offset="22" length="20" default="?"/>
        //    //  <field name="GID" offset="42" length="10" default="?"/>
        //    //  <field name="LicensePlate1" offset="52" length="10" default="?"/>
        //    //  <field name="LicensePlate2" offset="62" length="10" default="?"/>
        //    //  <field name="ScannerID" offset="72" length="4" default="?"/>
        //    //  <field name="ScannerHead" offset="76" length="8" default="?"/>
        //    //  <field name="ScanStatus" offset="84" length="2" default="?"/>
        //    //</telegram>  

        //    type = Functions.ConvertByteArrayToString(
        //                    message.GetFieldActualValue("Type"), -1, HexToStrMode.ToAscString).Trim();
        //    length = Functions.ConvertByteArrayToString(
        //                    message.GetFieldActualValue("Length"), -1, HexToStrMode.ToAscString).Trim();
        //    sequence = Functions.ConvertByteArrayToString(
        //                    message.GetFieldActualValue("Sequence"), -1, HexToStrMode.ToAscString).Trim();
        //    subsystem = Functions.ConvertByteArrayToString(
        //                    message.GetFieldActualValue("SubSystem"), -1, HexToStrMode.ToAscString).Trim();
        //    source = Functions.ConvertByteArrayToString(
        //                    message.GetFieldActualValue("Source"), -1, HexToStrMode.ToAscString).Trim();
        //    gid = Functions.ConvertByteArrayToString(
        //                    message.GetFieldActualValue("GID"), -1, HexToStrMode.ToAscString).Trim();
        //    lincesePlate1 = Functions.ConvertByteArrayToString(
        //                    message.GetFieldActualValue("LicensePlate1"), -1, HexToStrMode.ToAscString).Trim();
        //    lincesePlate2 = Functions.ConvertByteArrayToString(
        //                    message.GetFieldActualValue("LicensePlate2"), -1, HexToStrMode.ToAscString).Trim();
        //    scannerID = Functions.ConvertByteArrayToString(
        //                    message.GetFieldActualValue("ScannerID"), -1, HexToStrMode.ToAscString).Trim();
        //    byteArray = message.GetFieldActualValue("ScannerHead");
        //    scannerStatus = Functions.ConvertByteArrayToString(
        //                    message.GetFieldActualValue("ScanStatus"), -1, HexToStrMode.ToAscString).Trim();


        //    // Get values from each scanner Head
        //    byte[] andLogical = new byte[4] { 0x1, 0x2, 0x4, 0x8}; // values - 1,2,4,8,16,32,64,128
        //    int[] position = new int[8] {28, 24, 20, 16, 12, 8, 4, 0}; // as 4 bits 
        //    byte asii0 = 0x30; // 48
        //    byte asiiA = 0x40; // 64
        //    byte asiiA2F = 0x37; // 55
        //    int counterByte;
        //    int innerCounter;

        //    // Convert from ASII to Char (0 to F)
        //    for (counterByte =0;counterByte < byteArray.Length; counterByte++)
        //    {
        //        if (byteArray[counterByte] < asiiA) //0-9
        //        {
        //            byteArray[counterByte] = (byte)(byteArray[counterByte] - asii0);             
        //        }
        //        else // A-F
        //        {
        //             byteArray[counterByte] = (byte)(byteArray[counterByte] - asiiA2F);
        //        }
        //    }

        //    int[] head = new int[32] ; 

  
        //    // Convert to Bit (first 4 bits)
        //     for (counterByte =0;counterByte < byteArray.Length; counterByte++)
        //     {
        //         innerCounter = 0;
        //         if (counterByte > 2)
        //         {
        //             while (innerCounter < 4)
        //             {
        //                 if ((byteArray[counterByte] & andLogical[innerCounter]) == andLogical[innerCounter])
        //                 {
        //                     (head[innerCounter + position[counterByte]]) = 1;
        //                 }
        //                 else if ((byteArray[counterByte] & andLogical[innerCounter]) == 0x0)
        //                 {
        //                     (head[innerCounter + position[counterByte]]) = 0;
        //                 }
        //                 innerCounter = innerCounter + 1;
        //             }
        //         }
        //     }

        //     scannerHead = new List<int>();

        //     foreach (int temp in head)
        //     {
        //         scannerHead.Add(temp);
        //     }

        //    return;
        //}

        private void MessageDecoding(Telegram message, out string type, out string length,
                       out string sequence, out string gid_msb, out string gid_lsb, out string gid, out string location, out string lic1, out string lic2, out List<int> scan_head, out string scan_sts, out string plc_idx)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            //byte[] byteArray = new byte[8];

            string strSequence = string.Empty;
            string strGID_MSB = string.Empty;
            string strGID_LSB = string.Empty;
            string strSCN_HEAD = string.Empty;
            string strSCN_STS = string.Empty;
            string strPLC_IDX = string.Empty;

            type = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Type"), -1, HexToStrMode.ToAscString).Trim();
            length = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Length"), -1, HexToStrMode.ToAscString).Trim();
            location = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("LOCATION"), "16"); 
            lic1 = Functions.ConvertByteArrayToString(
                message.GetFieldActualValue("LIC_1"), -1, HexToStrMode.ToAscString).Trim();
            lic2 = Functions.ConvertByteArrayToString(
                message.GetFieldActualValue("LIC_2"), -1, HexToStrMode.ToAscString).Trim();

           strSequence = Functions.ConvertByteArrayToString(
                        message.GetFieldActualValue("Sequence"), -1, HexToStrMode.ToPaddedHexString).Trim().Replace(" ", string.Empty);
            strSCN_HEAD = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("SCN_HEAD"), -1, HexToStrMode.ToPaddedHexString).Trim().Replace(" ", string.Empty);
            strSCN_STS = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("SCN_STS"), -1, HexToStrMode.ToPaddedHexString).Trim().Replace(" ", string.Empty);
            strPLC_IDX = Functions.ConvertByteArrayToString(
                           message.GetFieldActualValue("PLC_IDX"), -1, HexToStrMode.ToPaddedHexString).Trim().Replace(" ", string.Empty);

            Int32 intSeq = Int32.Parse(strSequence, System.Globalization.NumberStyles.HexNumber);
            Int32 intSCNHead = Int32.Parse(strSCN_HEAD, System.Globalization.NumberStyles.HexNumber);
            Int32 intSCN_STS = Int32.Parse(strSCN_STS, System.Globalization.NumberStyles.HexNumber);
            Int32 intPLC_IDX = Int32.Parse(strPLC_IDX, System.Globalization.NumberStyles.HexNumber);

            // Convert Scan Head from Char into ASCII
            byte[] byteArray_Scn_Head = Functions.ConvertStringToFixLengthByteArray(intSCNHead.ToString(), 5, '0', Functions.PaddingRule.Left);

            // Convert Decimal value to Binary
            string binary = Convert.ToString(intSCNHead, 2).PadLeft(16, '0');

            scan_head = new List<int>();

            int intCount = 0;
            int intHead = 0;
            int intDESCPost = binary.Length - 1 ;
            while (intCount < binary.Length)
            {
                intHead = int.Parse(binary.Substring(intDESCPost,1));
                scan_head.Add(intHead);

                intCount += 1;
                intDESCPost -= 1;
            }

            sequence = intSeq.ToString();
            gid_msb = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("GID_MSB"), "16"); ;
            gid_lsb = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("GID_LSB"), "32");
            gid = gid_msb.ToString() + gid_lsb.ToString();

            scan_sts = intSCN_STS.ToString();
            plc_idx = intPLC_IDX.ToString();
            
            return;
        }

        private void LogMessageIntoDatabase(string sender, string receiver, string source, string gid, string lincesePlate1, string lincesePlate2,
                         List<int> scannerHead,  string scannerStatus, string PLCIdx)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;

            try
            {
                sqlConn = new SqlConnection(DBPersistor.ClassParameters.DBConnectionString);
                sqlCmd = new SqlCommand(DBPersistor.ClassParameters.STP_SAC_ITEMSCANNED, sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                sqlCmd.Parameters.Add("@GID", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@GID"].Value = gid;

                sqlCmd.Parameters.Add("@Location", SqlDbType.VarChar, 20);
                sqlCmd.Parameters["@Location"].Value = source;

                sqlCmd.Parameters.Add("@LicensePlate1", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@LicensePlate1"].Value = lincesePlate1;

                sqlCmd.Parameters.Add("@LicensePlate2", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@LicensePlate2"].Value = lincesePlate2;

                sqlCmd.Parameters.Add("@Status", SqlDbType.VarChar, 2);
                sqlCmd.Parameters["@Status"].Value = scannerStatus;

                sqlCmd.Parameters.Add("@plc_idx", SqlDbType.Int);
                sqlCmd.Parameters["@plc_idx"].Value = PLCIdx;

                int[] head = scannerHead.ToArray();

                for (int counter = 0; counter < 16; counter ++)
                {
                    sqlCmd.Parameters.Add("@Head" + (counter + 1).ToString(), SqlDbType.Int);
                    sqlCmd.Parameters["@Head" + (counter + 1).ToString()].Value = head[counter];  
                }

                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("ISC message database process failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Logging received ISC message failure! <" + thisMethod + ">", ex);

                return;
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
            }
        }

        /// <summary>
        /// Log IRD Message into Database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="receiver"></param>
        /// <param name="gid"></param>
        /// <param name="destination_1"></param>
        /// <param name="destination_2"></param>
        /// <param name="reason"></param>
        /// <param name="plc_idx"></param>
        private void LogIRDIntoDatabase(string sender, string receiver, string gid, string destination_1, string destination_2, string reason, string plc_idx)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;

            try
            {
                sqlConn = new SqlConnection(DBPersistor.ClassParameters.DBConnectionString);
                sqlCmd = new SqlCommand(DBPersistor.ClassParameters.STP_SAC_ITEMREDIRECT, sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                sqlCmd.Parameters.Add("@GID", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@GID"].Value = gid;

                sqlCmd.Parameters.Add("@DESTINATION_1", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@DESTINATION_1"].Value = destination_1;

                sqlCmd.Parameters.Add("@DESTINATION_2", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@DESTINATION_2"].Value = destination_2;

                sqlCmd.Parameters.Add("@REASON", SqlDbType.VarChar, 2);
                sqlCmd.Parameters["@REASON"].Value = reason;

                sqlCmd.Parameters.Add("@PLC_INDEX", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@PLC_INDEX"].Value = plc_idx;

                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("IRD message database process failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Logging received IRD message failure! <" + thisMethod + ">", ex);

                return;
            }
            finally
            {
                if (sqlConn != null) sqlConn.Close();
            }
        }

        #endregion
        
    }
}
