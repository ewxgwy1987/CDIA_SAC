#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       AFAI.cs
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
    /// AFAI message handler class
    /// </summary>
    public class AFAI
    {
        #region Class Fields and Properties Declaration

        // The name of current class 
        private static readonly string _className =
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
        // Create a logger for use in this class
        private static readonly log4net.ILog _logger =
                    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Airport Location Code field value.
        /// </summary>
        public string AirportLocationCode { get; set; }

        /// <summary>
        /// No Carrier Destination field value.
        /// </summary>
        public int NoCarrierDestination { get; set; }

        /// <summary>
        /// No Allocation Destination field value.
        /// </summary>
        public int NoAllocationDestination { get; set; }

        /// <summary>
        /// No Carrier Destination field value.
        /// </summary>
        public int DumpDischargeDestination { get; set; }

        /// <summary>
        /// No Read Destination field value.
        /// </summary>
        public int NoReadDestination { get; set; }

        /// <summary>
        /// AFAI message format.
        /// </summary>
        private TelegramFormat _messageFormat;

        #endregion

        #region Class Constructor, Dispose, & Destructor

        /// <summary>
        /// Class constructer.
        /// </summary>
        public AFAI(TelegramFormat MessageFormat)
        {
            if (MessageFormat == null)
                throw new Exception("Message format can not be null! Creating AFAI class object failure! " +
                    "<BHS.Engine.TCPClientChains.Messages.Handlers.AFAI.Constructor()>");

            _messageFormat = MessageFormat;
            AirportLocationCode = string.Empty;
            NoCarrierDestination = 0;
            NoAllocationDestination = 0;
            DumpDischargeDestination = 0;
            NoReadDestination = 0;
        }

        #endregion

        #region Class Method Declaration.

        /// <summary>
        /// Construct AFAI message.
        /// </summary>
        /// <returns></returns>
        public Telegram ConstructAFAIMessage()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            Telegram msg = null;

            if (AirportLocationCode == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Airport Location Code is null, no AFAI message is constructed! <" +
                            thisMethod + ">");
                return null;
            }

            if (NoCarrierDestination == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("No Carrier Destination is null, no AFAI message is constructed! <" +
                            thisMethod + ">");
                return null;
            }

            if (NoAllocationDestination == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("No Allocation Destination is null, no AFAI message is constructed! <" +
                            thisMethod + ">");
                return null;
            }

            if (DumpDischargeDestination == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Dump Discharge Destination is null, no AFAI message is constructed! <" +
                            thisMethod + ">");
                return null;
            }

            if (NoReadDestination == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("No Read Destination is null, no AFAI message is constructed! <" +
                            thisMethod + ">");
                return null;
            }

            try
            {
                //<telegram alias="AFAI" name="Airport_Code_And_Function_Allocation_Info_Message" sequence="True" acknowledge="True">
                //        <field name="Type" offset="0" length="4" default="48,48,49,51"/>
                //        <field name="Length" offset="4" length="4" default="48,48,50,52"/>
                //        <field name="Sequence" offset="8" length="4" default="?"/>
                //        <field name="AIRPORT_CODE" offset="12" length="4" default="?"/>
                //        <field name="NO_ALLOCATION" offset="16" length="2" default="?"/>
                //        <field name="NO_CARRIER" offset="18" length="2" default="?"/>
                //        <field name="NO_READ" offset="20" length="2" default="?"/>
                //        <field name="DUMP" offset="22" length="2" default="?"/>           
                //      </telegram> 

                byte[] data = null;
                msg = new Telegram(ref data);
                msg.Format = _messageFormat; ;

                bool temp;
                byte[] type, len, seq, airportCode, NoCarrierDest,NoReadDest,NoAllocDest,DumpDiscDest;

                type = msg.GetFieldDefaultValue("Type");
                temp = msg.SetFieldActualValue("Type",ref type, PALS.Telegrams.Common.PaddingRule.Right);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("AFAI message \"Type\" field value assignment is failed! <" +
                                thisMethod + ">");
                    return null;
                }

                len = msg.GetFieldDefaultValue("Length");
                temp = msg.SetFieldActualValue("Length", ref len, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("AFAI message \"Length\" field value assignment is failed! <" +
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
                    string HexValue = newSeq.ToString("X");
                    seq = Utilities.ToByteArray(HexValue, fieldLen, false);
                }
                temp = msg.SetFieldActualValue("Sequence", ref seq, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("AFAI message \"Sequence\" field value assignment is failed! <" +
                                thisMethod + ">");
                    return null;
                }

                fieldLen = _messageFormat.Field("AIRPORT_CODE").Length;
                airportCode = Functions.ConvertStringToFixLengthByteArray(AirportLocationCode,
                            fieldLen, ' ', Functions.PaddingRule.Right);
                temp = msg.SetFieldActualValue("AIRPORT_CODE", ref airportCode, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("AFAI message \"AIRPORT_CODE\" field value assignment is failed! <" +
                                thisMethod + ">");
                    return null;
                }

                fieldLen = _messageFormat.Field("NO_CARRIER").Length;
                NoCarrierDest = Utilities.ToByteArray(NoCarrierDestination.ToString("X"), fieldLen, false);
                temp = msg.SetFieldActualValue("NO_CARRIER", ref NoCarrierDest , PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("AFAI message \"NO_CARRIER\" field value assignment is failed! <" +
                                thisMethod + ">");
                    return null;
                }

                fieldLen = _messageFormat.Field("NO_ALLOCATION").Length;
                NoAllocDest = Utilities.ToByteArray(NoAllocationDestination.ToString("X"), fieldLen, false);
                temp = msg.SetFieldActualValue("NO_ALLOCATION", ref NoAllocDest, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("AFAI message \"NO_ALLOCATION\" field value assignment is failed! <" +
                                thisMethod + ">");
                    return null;
                }

                fieldLen = _messageFormat.Field("DUMP").Length;
                DumpDiscDest = Utilities.ToByteArray(DumpDischargeDestination.ToString("X"), fieldLen, false);
                temp = msg.SetFieldActualValue("DUMP", ref DumpDiscDest, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("AFAI message \"DUMP\" field value assignment is failed! <" +
                                thisMethod + ">");
                    return null;
                }

                fieldLen = _messageFormat.Field("NO_READ").Length;
                NoReadDest = Utilities.ToByteArray(NoReadDestination.ToString("X"), fieldLen, false);
                temp = msg.SetFieldActualValue("NO_READ", ref NoReadDest, PALS.Telegrams.Common.PaddingRule.Left);
                if (temp == false)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("AFAI message \"NO_READ\" field value assignment is failed! <" +
                                thisMethod + ">");
                    return null;
                }

                return msg;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Constructing AFAI message is failed! <" + thisMethod + ">", ex);

                return null;
            }
        }

        #endregion


    }
}
