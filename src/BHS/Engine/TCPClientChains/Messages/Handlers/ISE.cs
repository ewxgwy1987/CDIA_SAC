#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       ISE.cs
// Revision:      1.0 -   05 Oct 2009, By HS Chia
// =====================================================================================
//
#endregion

using System;
using System.Data;
using System.Data.SqlClient;
using PALS.Utilities;
using PALS.Telegrams;

namespace BHS.Engine.TCPClientChains.Messages.Handlers
{
    /// <summary>
    /// ISE message handler class
    /// </summary>
    public class ISE : AbstractMessageHandler, IDisposable
    {
        #region Class Fields and Properties Declaration

        // The name of current class 
        private static readonly string _className =
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
        // Create a logger for use in this class
        private static readonly log4net.ILog _logger =
                    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// IRD Message format
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
        public ISE()
        {
            if (!Init())
                throw new Exception("Creating class " + _className + " object failed! <Constructor()>");
        }

        /// <summary>
        /// Class destructer.
        /// </summary>
        ~ISE()
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
            string inMsgSender, inMsgReceiver, channelName, type, length, sequence, source,
                    originalDest, gid, gid_msb, gid_lsb, plc_index , sortationEvent, strDest1, strDest2, strReason;
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
                type = string.Empty;
                length = string.Empty;
                sequence = string.Empty;
                source = string.Empty;
                originalDest = string.Empty;
                gid = string.Empty;
                sortationEvent = string.Empty;
                plc_index = string.Empty;

                // 1. Decode message.
                MessageDecoding(message, out type, out length, out sequence,  out source, out originalDest
                        , out gid,out gid_msb, out gid_lsb,  out plc_index, out sortationEvent);

                // 2. Log message data into database.
                LogMessageIntoDatabase(inMsgSender, inMsgReceiver, source , originalDest, gid, plc_index, sortationEvent);

                // 3. Log message data into log file.
                if (_logger.IsInfoEnabled)
                    _logger.Info("[Channel:" + channelName +
                        "] <- [MSG(" + message.Format.AliasName +
                        "): Type=" + Functions.InvisibleCharacterFormating(ref type) +
                        ", Length=" + Functions.InvisibleCharacterFormating(ref length) +
                        ", Sequence=" + Functions.InvisibleCharacterFormating(ref sequence) +
                        ", Location=" + Functions.InvisibleCharacterFormating(ref source) +
                        ", Destination=" + Functions.InvisibleCharacterFormating(ref originalDest) +
                        ", GID=" + Functions.InvisibleCharacterFormating(ref gid) +
                        ", PLC Index=" + Functions.InvisibleCharacterFormating(ref plc_index) +
                        ", Sortation Type=" + Functions.InvisibleCharacterFormating(ref sortationEvent) +
                        "]. (Perf:" + DateTime.Now.Subtract(Perf).TotalMilliseconds.ToString() +
                        "ms). <" + thisMethod + ">");

                // 4. Construct IRD Telegram
                # region Construct IRD Telegram

                string strLP1,  strLP2, strAirline, strFlightNo, strSDO, strDestination, strEncodedType, strScannedStatus;
                GetBagInfo(gid, out strLP1, out strLP2, out strAirline, out strFlightNo, out strSDO, out strDestination, out strEncodedType, out strScannedStatus);

                Perf = DateTime.Now;

                IRD Ird = new IRD(MessageFormat_IRD);

                Ird.ScannerStatus = strScannedStatus;
                Ird.GID_MSB = int.Parse(gid_msb);
                Ird.GID_LSB = int.Parse(gid_lsb);
                Ird.PLC_IDX = int.Parse(plc_index);
                Ird.LicensePlate1 = strLP1;
                Ird.LicensePlate2 = strLP2;
                Ird.Carrier = strAirline;
                Ird.Flight = strFlightNo;
                Ird.SDO = strSDO == string.Empty ? DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString().PadLeft(2, '0') + "-" + DateTime.Now.Day.ToString().PadLeft(2, '0') :
                                                     strSDO.Substring(0, 4) + "-" + strSDO.Substring(4, 2) + "-" + strSDO.Substring(6, 2);
                Ird.EncodedType = strEncodedType;
                Ird.strMode = "ISE";
                Ird.DBPersistorConnStr = DBPersistor.ClassParameters.DBConnectionString;
                Ird.DBPersistor_STP_ALLOCPROP = DBPersistor.ClassParameters.STP_SAC_ALLOCPROP;
                Ird.DBPersistor_STP_IRDINFO = strEncodedType != string.Empty && strEncodedType != null ? DBPersistor.ClassParameters.STP_SAC_IRDVALUESMES : DBPersistor.ClassParameters.STP_SAC_IRDVALUES;
                Ird.DBPersistor_STP_CheckMUAvailability = DBPersistor.ClassParameters.STP_SAC_CHECKMUAVAILABILITY;
                Ird.Destination = originalDest;
                Ird.Location = source;
                Ird.EncodedDestination = strDestination;

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
                        LogIRDIntoDatabase(inMsgSender, inMsgReceiver, gid, strDest1, strDest2, strReason, "0");

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
                                ", PLC Index = " + Functions.InvisibleCharacterFormating(ref plc_index) +
                                "]. (Perf:" + DateTime.Now.Subtract(Perf).TotalMilliseconds.ToString() +
                                "ms). <" + thisMethod + ">");
                    }
                }
                
                #endregion 

            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Message handling is failed! <" + thisMethod + ">", ex);
            }
        }

        /// <summary>
        /// Message Decoding
        /// </summary>
        private void MessageDecoding(Telegram message, out string type, out string length,
                        out string sequence, out string source,out string originalDest, out string gid, out string gid_msb, out string gid_lsb,
                        out string plc_index,out string sortationEvent)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

          //<telegram alias="ISE" name="Item_Sortation_Event_Message" sequence="True" acknowledge="False">
          //  <field name="Type" offset="0" length="4" default="48,48,48,56"/>
          //  <field name="Length" offset="4" length="4" default="48,48,50,51"/>
          //  <field name="Sequence" offset="8" length="4" default="?"/>
          //  <field name="GID_MSB" offset="12" length="1" default="?"/>
          //  <field name="GID_LSB" offset="13" length="4" default="?"/>
          //  <field name="LOCATION" offset="17" length="2" default="?"/>
          //  <field name="DEST" offset="19" length="2" default="?"/>
          //  <field name="TYPE" offset="21" length="1" default="?"/>
          //  <field name="PLC_IDX" offset="22" length="1" default="?"/>
          //</telegram>

            type = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Type"), -1, HexToStrMode.ToAscString).Trim();
            length = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Length"), -1, HexToStrMode.ToAscString).Trim();

            sequence = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("Sequence"), "32");
            gid_msb = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("GID_MSB"), "16");
            gid_lsb = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("GID_LSB"), "32");
            gid = gid_msb + gid_lsb ;
            source = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("LOCATION"), "32");
            originalDest = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("DEST"), "16");
            sortationEvent = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("TYPE"), "16");
            plc_index = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("PLC_IDX"), "32");

            return;
        }

        private void LogMessageIntoDatabase(string sender, string receiver,
                        string source, string originalDest, string gid, string plc_index ,string sortationEvent)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;

            try
            {
                sqlConn = new SqlConnection(DBPersistor.ClassParameters.DBConnectionString);
                sqlCmd = new SqlCommand(DBPersistor.ClassParameters.STP_SAC_ITEMSORTATIONEVENT, sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                sqlCmd.Parameters.Add("@GID", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@GID"].Value = gid;

                sqlCmd.Parameters.Add("@Location", SqlDbType.VarChar, 20);
                sqlCmd.Parameters["@Location"].Value = source;

                sqlCmd.Parameters.Add("@Destination ", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@Destination "].Value = originalDest;

                sqlCmd.Parameters.Add("@SortationType", SqlDbType.VarChar, 2);
                sqlCmd.Parameters["@SortationType"].Value = sortationEvent;    

                sqlCmd.Parameters.Add("@PLC_INDEX", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@PLC_INDEX"].Value = plc_index;

                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("ISE message database process failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Logging received ISE message failure! <" + thisMethod + ">", ex);

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

        private void GetBagInfo(string GID, out string licensePlate1, out string licensePlate2, out string airline, out string flightNo, out string SDO, out string Destination, out string EncodedType, out string ScannedStatus)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;

            sqlConn = new SqlConnection(DBPersistor.ClassParameters.DBConnectionString);
            sqlCmd = new SqlCommand(DBPersistor.ClassParameters.STP_SAC_GETBAGINFO, sqlConn);
            sqlCmd.CommandType = CommandType.StoredProcedure;

            sqlCmd.Parameters.Add("@GID", SqlDbType.VarChar, 10);
            sqlCmd.Parameters["@GID"].Value = GID;

            sqlCmd.Parameters.Add("@LICENSE_PLATE1", SqlDbType.VarChar, 10).Direction = ParameterDirection.Output;
            sqlCmd.Parameters.Add("@LICENSE_PLATE2", SqlDbType.VarChar, 10).Direction = ParameterDirection.Output;
            sqlCmd.Parameters.Add("@AIRLINE", SqlDbType.VarChar, 3).Direction = ParameterDirection.Output;
            sqlCmd.Parameters.Add("@FLIGHT_NUMBER", SqlDbType.VarChar, 5).Direction = ParameterDirection.Output;
            sqlCmd.Parameters.Add("@SDO", SqlDbType.VarChar, 10).Direction = ParameterDirection.Output;
            sqlCmd.Parameters.Add("@DESTINATION", SqlDbType.VarChar, 20).Direction = ParameterDirection.Output;
            sqlCmd.Parameters.Add("@ENCODEDTYPE", SqlDbType.VarChar, 2).Direction = ParameterDirection.Output;
            sqlCmd.Parameters.Add("@SCANNED_STATUS", SqlDbType.VarChar, 2).Direction = ParameterDirection.Output;

            sqlConn.Open();
            sqlCmd.ExecuteNonQuery();

            licensePlate1 = sqlCmd.Parameters["@LICENSE_PLATE1"].Value.ToString();
            licensePlate2 = sqlCmd.Parameters["@LICENSE_PLATE2"].Value.ToString();
            airline = sqlCmd.Parameters["@AIRLINE"].Value.ToString();
            flightNo = sqlCmd.Parameters["@FLIGHT_NUMBER"].Value.ToString();
            SDO = sqlCmd.Parameters["@SDO"].Value.ToString();
            Destination = sqlCmd.Parameters["@DESTINATION"].Value.ToString();
            EncodedType = sqlCmd.Parameters["@ENCODEDTYPE"].Value.ToString();
            ScannedStatus = sqlCmd.Parameters["@SCANNED_STATUS"].Value.ToString();

            return;

        }

        #endregion

    }
}
