#region Release Information
//
// =====================================================================================
// Copyright 2013, SC Leong, All Rights Reserved.
// =====================================================================================
// FileName       1500P.cs
// Revision:      1.0 -   27 AUG 2013, By SC Leong
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
    /// 1500P message handler class
    /// </summary>
    public  class P1500 : AbstractMessageHandler, IDisposable
    {

        # region Class Fields and Properties Declaration

        // The name of current class
        private static readonly string _className =
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();

        // Create logger for use in this class
        private static readonly log4net.ILog _logger =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        # endregion

        # region Class Constructor, Dispose & Destructor

        /// <summary>
        /// Class constructer.
        /// </summary>
        public P1500()
        {
            if (!Init())
                throw new Exception("Creating class " + _className + " object failed! <Constructor()>");
        }

        /// <summary>
        /// Class destructer.
        /// </summary>
        ~P1500()
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

        # endregion

        #region Class Method Declaration

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
            string inMsgSender, inMsgReceiver, channelName, type, length, sequence, gid, location,lincesePlate, xRayID, status, BITStation, ETDStation, PLCTimeStamp;
            List<string> reasonCode = new List<string>();
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
                gid = string.Empty;
                lincesePlate = string.Empty;
                location = string.Empty;
                xRayID = string.Empty;
                status = string.Empty;
                BITStation = string.Empty;
                ETDStation = string.Empty;
                PLCTimeStamp = string.Empty;

                // 1. Decode message.
                MessageDecoding(message, out type, out length, out sequence, out gid, out location, out lincesePlate, out xRayID, out status, out BITStation, out ETDStation, out PLCTimeStamp);

               // 2. Log message data into database.
                LogMessageIntoDatabase(inMsgSender, inMsgReceiver, gid, lincesePlate, xRayID, status, BITStation, ETDStation, PLCTimeStamp,location);

                // 3. Log message data into log file.
                if (_logger.IsInfoEnabled)
                    _logger.Info("[Channel:" + channelName +
                        "] <- [MSG(" + message.Format.AliasName +
                        "): Type=" + Functions.InvisibleCharacterFormating(ref type) +
                        ", Length=" + Functions.InvisibleCharacterFormating(ref length) +
                        ", Sequence=" + Functions.InvisibleCharacterFormating(ref sequence) +
                        ", Location=" + Functions.InvisibleCharacterFormating(ref location) +
                        ", GID=" + Functions.InvisibleCharacterFormating(ref gid) +
                        ", License plate=" + Functions.InvisibleCharacterFormating(ref lincesePlate) +
                        ", X-Ray ID=" + Functions.InvisibleCharacterFormating(ref xRayID) +
                        ", Bag Status=" + Functions.InvisibleCharacterFormating(ref status) +
                        ", BRP=" + Functions.InvisibleCharacterFormating(ref ETDStation) +
                        ", BIT=" + Functions.InvisibleCharacterFormating(ref BITStation) +
                        ", PLC Time Stamp, =" + Functions.InvisibleCharacterFormating(ref PLCTimeStamp) +
                        "]. (Perf:" + DateTime.Now.Subtract(Perf).TotalMilliseconds.ToString() +
                        "ms). <" + thisMethod + ">");
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
                        out string sequence, out string gid, out string location, out string lincesePlate,
                        out string xRayID, out string status, out string BitStation, out string ETDStation, out string PLCTimeStamp)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            byte[] byteArray = new byte[14];

              //<telegram alias="1500P" name="1500P_Information_Message" sequence="True" acknowledge="False">
              //  <field name="Type" offset="0" length="4" default="48,48,49,55"/>
              //  <field name="Length" offset="4" length="4" default="48,49,53,55"/>
              //  <field name="Sequence" offset="8" length="4" default="?"/>
              //  <field name="GID_MSB" offset="12" length="1" default="?"/>
              //  <field name="GID_LSB" offset="13" length="4" default="?"/>
              //  <field name="LOCATION" offset="17" length="2" default="?"/>
              //  <field name="LIC" offset="19" length="10" default="?"/>
              //  <field name="TYPE" offset="29" length="1" default="?"/>
              //  <field name="X_RAYID" offset="30" length="1" default="?"/>
              //  <field name="BRP" offset="31" length="1" default="?"/>
              //  <field name="BIT" offset="32" length="1" default="?"/>
              //  <field name="YEAR" offset="33" length="4" default="?"/>
              //  <field name="MON" offset="37" length="4" default="?"/>
              //  <field name="DAY" offset="41" length="4" default="?"/>
              //  <field name="HR" offset="45" length="4" default="?"/>
              //  <field name="MIN" offset="49" length="4" default="?"/>
              //  <field name="SEC" offset="53" length="4" default="?"/>
              //</telegram>

            type = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Type"), -1, HexToStrMode.ToAscString).Trim();
            length = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("Length"), -1, HexToStrMode.ToAscString).Trim();

            sequence = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("Sequence"), "32");
            gid = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("GID_MSB"), "16") +
                     Utilities.ConvertVal2Decimal(message.GetFieldActualValue("GID_LSB"), "32");
            location = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("LOCATION"),"32");
            lincesePlate = Functions.ConvertByteArrayToString(
                            message.GetFieldActualValue("LIC"), -1, HexToStrMode.ToAscString).Trim();
            status = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("TYPE"), "16");
            xRayID = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("X_RAYID"), "16");
            BitStation = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("BIT"),"16");
            ETDStation = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("BRP"),"16");
            PLCTimeStamp = Utilities.ConvertVal2Decimal(message.GetFieldActualValue("YEAR"), "16").Trim() + "/" +
                                          Utilities.ConvertVal2Decimal(message.GetFieldActualValue("MON"), "16").Trim().PadLeft(2, '0') + "/" +
                                          Utilities.ConvertVal2Decimal(message.GetFieldActualValue("DAY"), "16").Trim().PadLeft(2, '0') + " " +
                                          Utilities.ConvertVal2Decimal(message.GetFieldActualValue("HR"), "16").Trim().PadLeft(2, '0') + ":" +
                                          Utilities.ConvertVal2Decimal(message.GetFieldActualValue("MIN"), "16").Trim().PadLeft(2, '0') + ":" +
                                          Utilities.ConvertVal2Decimal(message.GetFieldActualValue("SEC"), "16").Trim().PadLeft(2, '0');
             return;
        }

        /// <summary>
        /// Log Message into database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="receiver"></param>
        /// <param name="gid"></param>
        /// <param name="lincesePlate"></param>
        /// <param name="xRayID"></param>
        /// <param name="status"></param>
        /// <param name="BitStation"></param>
        /// <param name="ETDStation"></param>
        /// <param name="PLCTimeStamp"></param>
        private void LogMessageIntoDatabase(string sender, string receiver, string gid, string lincesePlate,
                        string xRayID, string status, string BitStation, string ETDStation, string PLCTimeStamp, string Location)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            SqlConnection sqlConn = null;
            SqlCommand sqlCmd = null;
            string temp = string.Empty;
            try
            {
                sqlConn = new SqlConnection(DBPersistor.ClassParameters.DBConnectionString);
                sqlCmd = new SqlCommand(DBPersistor.ClassParameters.STP_SAC_1500PINFORMATION, sqlConn);
                sqlCmd.CommandType = CommandType.StoredProcedure;

                sqlCmd.Parameters.Add("@GID", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@GID"].Value = gid;

                sqlCmd.Parameters.Add("@LICENSE_PLATE", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@LICENSE_PLATE"].Value = lincesePlate;

                sqlCmd.Parameters.Add("@XRAY_ID", SqlDbType.VarChar, 10);
                sqlCmd.Parameters["@XRAY_ID"].Value = xRayID;

                sqlCmd.Parameters.Add("@BIT_STATION", SqlDbType.VarChar, 20);
                sqlCmd.Parameters["@BIT_STATION"].Value = BitStation;

                sqlCmd.Parameters.Add("@ETD_STATION", SqlDbType.VarChar, 20);
                sqlCmd.Parameters["@ETD_STATION"].Value = ETDStation;

                sqlCmd.Parameters.Add("@PLC_TIMESTAMP", SqlDbType.VarChar, 20);
                sqlCmd.Parameters["@PLC_TIMESTAMP"].Value = PLCTimeStamp;

                sqlCmd.Parameters.Add("@BAG_STATUS", SqlDbType.VarChar, 2);
                sqlCmd.Parameters["@BAG_STATUS"].Value = status;

                sqlCmd.Parameters.Add("@LOCATION", SqlDbType.VarChar, 20);
                sqlCmd.Parameters["@LOCATION"].Value = Location;

                sqlConn.Open();
                sqlCmd.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("P5K message database process failure! <" + thisMethod + ">", ex);

                return;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Logging received P5K message failure! <" + thisMethod + ">", ex);

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
