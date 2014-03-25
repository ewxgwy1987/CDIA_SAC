#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       DLPS.cs
// Revision:      1.0 -   07 Ot 2009, By HS Chia
// =====================================================================================
//
#endregion

using System;
using PALS.Utilities;
using PALS.Telegrams;

namespace BHS.Engine.TCPClientChains.Messages.Handlers
{
    /// <summary>
    /// DLPS message handler class
    /// </summary>
    public class DLPS
    {
        #region Class Fields and Properties Declaration

        // The name of current class 
        private static readonly string _className =
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
        // Create a logger for use in this class
        private static readonly log4net.ILog _logger =
                    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// GID value.
        /// </summary>
        public string GID { get; set; }

        /// <summary>
        /// License Plate field value.
        /// </summary>
        public string LicensePlate { get; set; }

        /// <summary>
        /// Status field value.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// DLPS message format.
        /// </summary>
        private TelegramFormat _messageFormat;

        #endregion

        #region Class Constructor, Dispose, & Destructor

        /// <summary>
        /// Class constructer.
        /// </summary>
        public DLPS(TelegramFormat MessageFormat)
        {
            if (MessageFormat == null)
                throw new Exception("Message format can not be null! Creating DLPS class object failure! " +
                    "<BHS.Engine.TCPClientChains.Messages.Handlers.DLPS.Constructor()>");

            _messageFormat = MessageFormat;
            GID = string.Empty;
            LicensePlate = string.Empty;
            Status = string.Empty;
        }

        #endregion

        #region Class Method Declaration.

        /// <summary>
        /// Construct DLPS message.
        /// </summary>
        /// <returns></returns>
        public Telegram ConstructDLPSMessage()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            Telegram msg = null;

            if (GID == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("GID is null, no DLPS message is constructed! <" +
                            thisMethod + ">");
                return null;
            }

            if (LicensePlate == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("License Plate is null, no DLPS message is constructed! <" +
                            thisMethod + ">");
                return null;
            }

            if (Status == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Status is null, no DLPS message is constructed! <" +
                            thisMethod + ">");
                return null;
            }

            try
            {
                //<telegram alias="DLPS" name="Duplicate_License_Plate_Message" sequence="True" acknowledge="False">
                //  <field name="Type" offset="0" length="4" default="48,48,50,54"/>
                //  <field name="Length" offset="4" length="4" default="48,48,51,52"/>
                //  <field name="Sequence" offset="8" length="4" default="?"/>
                //  <field name="GID" offset="12" length="10" default="?"/>
                //  <field name="LicensePlate" offset="22" length="10" default="?"/>
                //  <field name="Status" offset="32" length="2" default="?"/>
                //</telegram>

                byte[] data = null;
                msg = new Telegram(ref data);
                msg.Format = _messageFormat; ;

                bool temp;
                byte[] type, len, seq, gid, licensePlate, status;

                type = msg.GetFieldDefaultValue("Type");
                temp = msg.SetFieldActualValue("Type",ref type, PALS.Telegrams.Common.PaddingRule.Right);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("DLPS message \"Type\" field value assignment is failed! <" +
                                thisMethod + ">");
                    return null;
                }

                len = msg.GetFieldDefaultValue("Length");
                temp = msg.SetFieldActualValue("Length", ref len, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("DLPS message \"Length\" field value assignment is failed! <" +
                                thisMethod + ">");
                    return null;
                }

                // The new sequence number will be calculated and assigned to the
                // "Sequence" field of outgoing application messages, if this message associated
                // TelegramFormat object is indicated that it is the new sequence number
                // required message. The sequence number is globally contained by the static class:
                // PALS.Utilities.SequenceNo. You can get the application global wide unique
                // new sequence number by calling SequenceNo.NewSequenceNo Shared property directly, 
                // without instantial the SequenceNo.
                int fieldLen = _messageFormat.Field("Sequence").Length;
                seq = new byte[fieldLen];
                if (msg.Format.NeedNewSequence == true)
                {
                    long newSeq = SequenceNo.NewSequenceNo1;
                    seq = Functions.ConvertStringToFixLengthByteArray(
                            newSeq.ToString(), fieldLen, '0', Functions.PaddingRule.Left);
                }
                temp = msg.SetFieldActualValue("Sequence", ref seq, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("DLPS message \"Sequence\" field value assignment is failed! <" +
                                thisMethod + ">");
                    return null;
                }

                fieldLen = _messageFormat.Field("GID").Length;
                gid = Functions.ConvertStringToFixLengthByteArray(GID,
                            fieldLen, ' ', Functions.PaddingRule.Right);
                temp = msg.SetFieldActualValue("GID", ref gid, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("DLPS message \"GID\" field value assignment is failed! <" +
                                thisMethod + ">");
                    return null;
                }

                fieldLen = _messageFormat.Field("LicensePlate").Length;
                licensePlate = Functions.ConvertStringToFixLengthByteArray(LicensePlate,
                            fieldLen, ' ', Functions.PaddingRule.Right);
                temp = msg.SetFieldActualValue("LicensePlate", ref licensePlate, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("DLPS message \"LicensePlate\" field value assignment is failed! <" +
                                thisMethod + ">");
                    return null;
                }

                fieldLen = _messageFormat.Field("Status").Length;
                status = Functions.ConvertStringToFixLengthByteArray(Status,
                            fieldLen, ' ', Functions.PaddingRule.Right);
                temp = msg.SetFieldActualValue("Status", ref status, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("DLPS message \"Status\" field value assignment is failed! <" +
                                thisMethod + ">");
                    return null;
                }

                return msg;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Constructing DLPS message is failed! <" + thisMethod + ">", ex);

                return null;
            }
        }

        #endregion


    }
}
