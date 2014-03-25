#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       FBTI.cs
// Revision:      1.0 -   07 Ot 2009, By HS Chia
// =====================================================================================
//
#endregion

using System;
using PALS.Utilities;
using PALS.Telegrams;
using System.Collections;
using System.Collections.Generic;

namespace BHS.Engine.TCPClientChains.Messages.Handlers
{
    /// <summary>
    /// FBTI message handler class
    /// </summary>
    public class FBTI
    {
        #region Class Fields and Properties Declaration

        // The name of current class 
        private static readonly string _className =
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
        // Create a logger for use in this class
        private static readonly log4net.ILog _logger =
                    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// No Of Carrier field value.
        /// </summary>
        public int NoOfFallback { get; set; }

        /// <summary>
        /// Allocation Data (Code, Sort Device) field value.
        /// </summary>
        public Tag[] AllocationData { get; set; }

        /// <summary>
        /// FBTI message format.
        /// </summary>
        private TelegramFormat _messageFormat;

        #endregion

        #region Class Constructor, Dispose, & Destructor

        /// <summary>
        /// Class constructer.
        /// </summary>
        public FBTI(TelegramFormat MessageFormat)
        {
            if (MessageFormat == null)
                throw new Exception("Message format can not be null! Creating FBTI class object failure! " +
                    "<BHS.Engine.TCPClientChains.Messages.Handlers.FBTI.Constructor()>");

            _messageFormat = MessageFormat;
            NoOfFallback = 0;
            AllocationData = null;
        }

        #endregion

        #region Class Method Declaration.

        /// <summary>
        /// Construct FBTI message.
        /// </summary>
        /// <returns></returns>
        public Telegram ConstructFBTIMessage()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            Telegram msg = null;

            if (NoOfFallback == 0)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("No Of Fallback is 0, no FBTI message is constructed! <" +
                            thisMethod + ">");
                return null;
            }

            if (AllocationData == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Allocation Data is null, no FBTI message is constructed! <" +
                            thisMethod + ">");
                return null;
            }

            try
            {
              //<telegram alias="FBTI" name="Fallback_Tag_Information_Message" sequence="True" acknowledge="True">
              //  <field name="Type" offset="0" length="4" default="48,48,49,53"/>
              //  <field name="Length" offset="4" length="4" default="?"/>
              //  <field name="Sequence" offset="8" length="4" default="?"/>
              //  <field name="NO_FALLBACK" offset="12" length="1" default="?"/>
              //  <field name="FALLBACK_NO" offset="13" length="1" default="?"/>
              //  <field name="DEST" offset="14" length="2" default="?"/>   
              //</telegram>  

                byte[] data = null;
                msg = new Telegram(ref data);
                msg.Format = _messageFormat; 
                
                byte[] type, len, seq, noOfTag, code, dest;
                List<byte> msgSend = new List<byte>();

                type = msg.GetFieldDefaultValue("Type");

                foreach (byte tempList in type)
                {
                    msgSend.Add(tempList);
                }

                int fieldLen = _messageFormat.Field("Length").Length;
                int tempLen = _messageFormat.FieldsLengthSum() +
                                ((NoOfFallback - 1) * (_messageFormat.Field("FALLBACK_NO").Length +
                                _messageFormat.Field("DEST").Length));
                len = Functions.ConvertStringToFixLengthByteArray(
                            tempLen.ToString(), fieldLen, '0', Functions.PaddingRule.Left);

                foreach (byte tempList in len)
                {
                    msgSend.Add(tempList);
                }

                // The new sequence number will be calculated and assigned to the
                // "Sequence" field of outgoing application messages, if this message associated
                // TelegramFormat object is indicated that it is the new sequence number
                // required message. The sequence number is globally contained by the static class:
                // PALS.Utilities.SequenceNo. You can get the application global wide unique
                // new sequence number by calling SequenceNo.NewSequenceNo Shared property directly, 
                // without instantial the SequenceNo.
                fieldLen = _messageFormat.Field("Sequence").Length;
                seq = new byte[fieldLen];
                if (msg.Format.NeedNewSequence == true)
                {
                    long newSeq = SequenceNo.NewSequenceNo1;
                    string strHexSeq = newSeq.ToString("X");
                    seq = Utilities.ToByteArray(strHexSeq, fieldLen, false);
                }

                foreach (byte tempList in seq)
                {
                    msgSend.Add(tempList);
                }


                fieldLen = _messageFormat.Field("NO_FALLBACK").Length;
                string strHexNoFallback = NoOfFallback.ToString("X");
                noOfTag = Utilities.ToByteArray(strHexNoFallback, fieldLen, false);
                foreach (byte tempList in noOfTag)
                {
                    msgSend.Add(tempList);
                }

                string strHexFallbackNo = string.Empty;
                string strHexDest = string.Empty;
                for (int i = 0; i < AllocationData.Length; i++)
                {
                    fieldLen = _messageFormat.Field("FALLBACK_NO").Length;
                    strHexFallbackNo = AllocationData[i].Code.ToString("X");
                    code = Utilities.ToByteArray(strHexFallbackNo, fieldLen, false);
                    foreach (byte tempList in code)
                    {
                        msgSend.Add(tempList);
                    }

                    fieldLen = _messageFormat.Field("DEST").Length;
                    strHexDest = AllocationData[i].Destination.ToString("X");
                    dest = Utilities.ToByteArray(strHexDest, fieldLen, false);
                    foreach (byte tempList in dest)
                    {
                        msgSend.Add(tempList);
                    }
                }

                msg.RawData = msgSend.ToArray();

                return msg;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Constructing FBTI message is failed! <" + thisMethod + ">", ex);

                return null;
            }
        }

        #endregion


    }
}
