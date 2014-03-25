#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       FPTI.cs
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
    /// FPTI message handler class
    /// </summary>
    public class FPTI
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
        public int NoOfFourPier { get; set; }

        /// <summary>
        /// Allocation Data (Code, Sort Device) field value.
        /// </summary>
        public Tag[] AllocationData { get; set; }

              /// <summary>
        /// FPTI message format.
        /// </summary>
        private TelegramFormat _messageFormat;

        #endregion

        #region Class Constructor, Dispose, & Destructor

        /// <summary>
        /// Class constructer.
        /// </summary>
        public FPTI(TelegramFormat MessageFormat)
        {
            if (MessageFormat == null)
                throw new Exception("Message format can not be null! Creating FPTI class object failure! " +
                    "<BHS.Engine.TCPClientChains.Messages.Handlers.FPTI.Constructor()>");

            _messageFormat = MessageFormat;
            NoOfFourPier = 0;
            AllocationData = null;
        }

        #endregion

        #region Class Method Declaration.

        /// <summary>
        /// Construct FPTI message.
        /// </summary>
        /// <returns></returns>
        public Telegram ConstructFPTIMessage()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            Telegram msg = null;

            if (NoOfFourPier == 0)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("No Of Four Digits Pier is 0, no FPTI message is constructed! <" +
                            thisMethod + ">");
                return null;
            }

            if (AllocationData == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Allocation Data is null, no FPTI message is constructed! <" +
                            thisMethod + ">");
                return null;
            }

            try
            {
                //<telegram alias="FPTI" name="Four_Digits_Pier_Tag_Information_Message" sequence="True" acknowledge="True">
                //  <field name="Type" offset="0" length="4" default="48,48,49,54"/>
                //  <field name="Length" offset="4" length="4" default="?"/>
                //  <field name="Sequence" offset="8" length="4" default="?"/>
                //  <field name="NO_4TAG" offset="12" length="1" default="?"/>
                //  <field name="4PIER_NO" offset="13" length="2" default="?"/>
                //  <field name="DEST" offset="15" length="2" default="?"/>   
                //</telegram>      

                byte[] data = null;
                msg = new Telegram(ref data);
                msg.Format = _messageFormat; 
                
                //bool temp;
                byte[] type, len, seq, noFourPier, code, dest;
                List<byte> msgSend = new List<byte>();

                type = msg.GetFieldDefaultValue("Type");

                foreach (byte tempList in type)
                {
                    msgSend.Add(tempList);
                }

                int fieldLen = _messageFormat.Field("Length").Length;
                int tempLen = _messageFormat.FieldsLengthSum() +
                                ((NoOfFourPier - 1) * (_messageFormat.Field("4PIER_NO").Length +
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
                    string strHexVal = newSeq.ToString("X");
                    seq = Utilities.ToByteArray(strHexVal, fieldLen, false);
                }

                foreach (byte tempList in seq)
                {
                    msgSend.Add(tempList);
                }


                fieldLen = _messageFormat.Field("NO_4TAG").Length;
                string strHexNo4Tag = NoOfFourPier.ToString("X");
                noFourPier = Utilities.ToByteArray(strHexNo4Tag, fieldLen, false);
                foreach (byte tempList in noFourPier)
                {
                    msgSend.Add(tempList);
                }

                string strHexCode = string.Empty;
                string strHexDest = string.Empty;
                for (int i = 0; i < AllocationData.Length; i++)
                {
                    fieldLen = _messageFormat.Field("4PIER_NO").Length;
                    strHexCode = AllocationData[i].Code.ToString("X");
                    code = Utilities.ToByteArray(strHexCode, fieldLen, false);
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
                    _logger.Error("Constructing FPTI message is failed! <" + thisMethod + ">", ex);

                return null;
            }
        }

        #endregion


    }
}
