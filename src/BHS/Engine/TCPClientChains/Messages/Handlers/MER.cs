#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       MER.cs
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
    /// MER message handler class
    /// </summary>
    public class MER : AbstractMessageHandler, IDisposable
    {
        #region Class Fields and Properties Declaration

        // The name of current class 
        private static readonly string _className =
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
        // Create a logger for use in this class
        private static readonly log4net.ILog _logger =
                    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
        public MER()
        {
            if (!Init())
                throw new Exception("Creating class " + _className + " object failed! <Constructor()>");
        }

        /// <summary>
        /// Class destructer.
        /// </summary>
        ~MER()
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
            string inMsgSender, inMsgReceiver, channelName, type, length, sequence, source, gid,gid_msb,gid_lsb,
                        lincesePlate, airline, flightNumber, sdo, destination, encodedType, strDest1, strDest2, strReason, plcIdx;
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
                gid = string.Empty;
                lincesePlate = string.Empty;
                airline = string.Empty;
                flightNumber = string.Empty;
                sdo = string.Empty;
                destination = string.Empty;
                encodedType = string.Empty;
                plcIdx = string.Empty;

                // 1. Decode message.
                MessageDecoding(message, out type, out length, out sequence,  out source, out gid, out gid_msb, out gid_lsb,
                                out lincesePlate, out airline, out flightNumber, out sdo, out destination, out encodedType, out plcIdx);

                // 2. Log message data into database.
                LogMessageIntoDatabase(inMsgSender, inMsgReceiver, source, gid, lincesePlate, airline, flightNumber, sdo, destination, encodedType, plcIdx);

                // 3. Log message data into log file.
                if (_logger.IsInfoEnabled)
                    _logger.Info("[Channel:" + channelName +
                        "] <- [MSG(" + message.Format.AliasName +
                        "): Type=" + Functions.InvisibleCharacterFormating(ref type) +
                        ", Length=" + Functions.InvisibleCharacterFormating(ref length) +
                        ", Sequence=" + Functions.InvisibleCharacterFormating(ref sequence) +
                        ", GID=" + Functions.InvisibleCharacterFormating(ref gid) +
                        ", Location=" + Functions.InvisibleCharacterFormating(ref source) +
                        ", License plate=" + Functions.InvisibleCharacterFormating(ref lincesePlate) +
                        ", Airline=" + Functions.InvisibleCharacterFormating(ref airline) +
                        ", Flight Number=" + Functions.InvisibleCharacterFormating(ref flightNumber) +
                        ", SDO=" + Functions.InvisibleCharacterFormating(ref sdo) +
                        ", Destination=" + Functions.InvisibleCharacterFormating(ref destination) +
                        ", Encoded Type=" + Functions.InvisibleCharacterFormating(ref encodedType) +
                        ", PLC Index=" + Functions.InvisibleCharacterFormating(ref plcIdx) +
                        "]. (Perf:" + DateTime.Now.Subtract(Perf).TotalMilliseconds.ToString() +
                        "ms). <" + thisMethod + ">");

                // 4. Construct IRD Telegram 
                # region Construct IRD Telegram 

                if (encodedType != "3" && encodedType != "5")
                {
                    Perf = DateTime.Now;

                    IRD Ird = new IRD(MessageFormat_IRD);

                    Ird.ScannerStatus = string.Empty;
                    Ird.GID_MSB = int.Parse(gid_msb);
                    Ird.GID_LSB = int.Parse(gid_lsb);
                    Ird.PLC_IDX = int.Parse(plcIdx);
                    Ird.LicensePlate1 = lincesePlate;
                    Ird.LicensePlate2 = string.Empty;
                    Ird.Carrier = airline;
                    Ird.Flight = flightNumber;
                    Ird.SDO = sdo == string.Empty ? DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString().PadLeft(2,'0') + "-" + DateTime.Now.Day.ToString().PadLeft(2,'0') : 
                                                                             sdo.Substring(0, 4) + "-" + sdo.Substring(4, 2) + "-" + sdo.Substring(6, 2);
                    Ird.EncodedType = encodedType;
                    Ird.strMode = "MES";
                    Ird.DBPersistorConnStr = DBPersistor.ClassParameters.DBConnectionString;
                    Ird.DBPersistor_STP_ALLOCPROP = DBPersistor.ClassParameters.STP_SAC_ALLOCPROP;
                    Ird.DBPersistor_STP_IRDINFO = DBPersistor.ClassParameters.STP_SAC_IRDVALUESMES;
                    Ird.DBPersistor_STP_CheckMUAvailability = DBPersistor.ClassParameters.STP_SAC_CHECKMUAVAILABILITY;
                    Ird.DBPersistor_STP_CheckTagDest = DBPersistor.ClassParameters.STP_SAC_CHECKTAGDEST;
                    Ird.Location = source;

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
                            LogIRDIntoDatabase(inMsgSender, inMsgReceiver, gid, strDest1, strDest2, strReason, plcIdx);

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
                                    ", PLC Index = " + Functions.InvisibleCharacterFormating(ref plcIdx) +
                                    "]. (Perf:" + DateTime.Now.Subtract(Perf).TotalMilliseconds.ToString() +
                                    "ms). <" + thisMethod + ">");
                        }
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
                        out string sequence,  out string source, out string gid, out string gid_msb, out string gid_lsb,
                        out string lincesePlate, out string airline, out string flightNumber, out string sdo,
                        out string destination, out string encodedType, out string plc_idx)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

      //<telegram alias="MER" name="Item_Manual_Encoding_Request_Message" sequence="True" acknowledge="False">
      //  <field name="Type" offset="0" length="4" default="48,48,49,50"/>
      //  <field name="Length" offset="4" length="4" default="48,49,52,52"/>
      //  <field name="Sequence" offset="8" length="4" default="?"/>
      //  <field name="GID_MSB" offset="12" length="1" default="?"/>
      //  <field name="GID_LSB" offset="13" length="4" default="?"/>
      //  <field name="LOCATION" offset="17" length="2" default="?"/>
      //  <field name="LIC" offset="19" length="10" default="?"/>
      //  <field name="AIRLINE" offset="29" length="4" default="?"/>
      //  <field name="FLIGHT" offset="33" length="2" default="?"/>
      //  <field name="SDO" offset="35" length="6" default="?"/>
      //  <field name="DEST" offset="41" length="2" default="?"/>
      //  <field name="TYPE" offset="43" length="1" default="?"/>
      //</telegram> 

            type = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Type"), -1, HexToStrMode.ToAscString).Trim();
            length = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Length"), -1, HexToStrMode.ToAscString).Trim();
            sequence = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("Sequence"), "16");
            gid_msb = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("GID_MSB"), "16");
            gid_lsb = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("GID_LSB"), "32");
            gid = gid_msb + gid_lsb;
            source = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("LOCATION"), "32");
            lincesePlate = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("LIC"), -1, HexToStrMode.ToAscString).Trim();
            airline = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("AIRLINE"), -1, HexToStrMode.ToAscString).Trim();
            flightNumber = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("FLIGHT"), "32").PadLeft(4, '0');
            sdo = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("SDO"), -1, HexToStrMode.ToAscString).Trim();
            destination = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("DEST"), "32");
            encodedType = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("TYPE"), "16");
            plc_idx = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("PLC_IDX"), "32");

            return;
        }

        private void LogMessageIntoDatabase(string sender, string receiver,  string source, string gid, string lincesePlate, string airline, 
                        string flightNumber, string sdo, string destination, string encodedType, string PLC_IDX)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;

            try
            {
                sqlConn = new SqlConnection(DBPersistor.ClassParameters.DBConnectionString);
                sqlCmd = new SqlCommand(DBPersistor.ClassParameters.STP_SAC_ITEMENCODINGREQUEST, sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                sqlCmd.Parameters.Add("@Location", SqlDbType.VarChar, 20);
                sqlCmd.Parameters["@Location"].Value = source;

                sqlCmd.Parameters.Add("@GID", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@GID"].Value = gid;

                sqlCmd.Parameters.Add("@LicensePlate", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@LicensePlate"].Value = lincesePlate;

                sqlCmd.Parameters.Add("@Airline", SqlDbType.VarChar, 3);
                sqlCmd.Parameters["@Airline"].Value = airline;  

                sqlCmd.Parameters.Add("@FlightNumber", SqlDbType.VarChar, 5);
                sqlCmd.Parameters["@FlightNumber"].Value = flightNumber;  

                sqlCmd.Parameters.Add("@SDO", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@SDO"].Value = sdo;  

                sqlCmd.Parameters.Add("@Destination", SqlDbType.VarChar, 20);
                sqlCmd.Parameters["@Destination"].Value = destination;  

                sqlCmd.Parameters.Add("@EncodingType", SqlDbType.VarChar, 2);
                sqlCmd.Parameters["@EncodingType"].Value = encodedType;

                sqlCmd.Parameters.Add("@PLC_IDX", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@PLC_IDX"].Value = PLC_IDX;
 
                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("MER message database process failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Logging received MER message failure! <" + thisMethod + ">", ex);

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
