#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       TPTI.cs
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
    /// TPTI message handler class
    /// </summary>
    public class TPTI
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
        public int NoOfTwoPier { get; set; }

        /// <summary>
        /// Allocation Data (Code, Sort Device) field value.
        /// </summary>
        public Tag[] AllocationData { get; set; }

              /// <summary>
        /// TPTI message format.
        /// </summary>
        private TelegramFormat _messageFormat;

        #endregion

        #region Class Constructor, Dispose, & Destructor

        /// <summary>
        /// Class constructer.
        /// </summary>
        public TPTI(TelegramFormat MessageFormat)
        {
            if (MessageFormat == null)
                throw new Exception("Message format can not be null! Creating TPTI class object failure! " +
                    "<BHS.Engine.TCPClientChains.Messages.Handlers.TPTI.Constructor()>");

            _messageFormat = MessageFormat;
            NoOfTwoPier = 0;
            AllocationData = null;
        }

        #endregion

        #region Class Method Declaration.

        /// <summary>
        /// Construct TPTI message.
        /// </summary>
        /// <returns></returns>
        public Telegram ConstructTPTIMessage()
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            Telegram msg = null;

            if (NoOfTwoPier == 0)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("No Of Two Digits Pier is 0, no TPTI message is constructed! <" +
                            thisMethod + ">");
                return null;
            }

            if (AllocationData == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Allocation Data is null, no TPTI message is constructed! <" +
                            thisMethod + ">");
                return null;
            }

            try
            {
                //<telegram alias="TPTI" name="Two_Digits_Pier_Tag_Information_Message" sequence="True" acknowledge="False">
                //  <field name="Type" offset="0" length="4" default="48,48,50,50"/>
                //  <field name="Length" offset="4" length="4" default="?"/>
                //  <field name="Sequence" offset="8" length="4" default="?"/>
                //  <field name="NoOfTwoPier" offset="12" length="2" default="?"/>
                //  <field name="TwoPierCode" offset="14" length="2" default="?"/>
                //  <field name="Destination" offset="16" length="10" default="?"/>   
                //</telegram>  

                byte[] data = null;
                msg = new Telegram(ref data);
                msg.Format = _messageFormat; 
                
                //bool temp;
                byte[] type, len, seq, noTwoPier, code, dest;
                List<byte> msgSend = new List<byte>();

                type = msg.GetFieldDefaultValue("Type");

                foreach (byte tempList in type)
                {
                    msgSend.Add(tempList);
                }

                int fieldLen = _messageFormat.Field("Length").Length;
                int tempLen = _messageFormat.FieldsLengthSum() +
                                ((NoOfTwoPier - 1) * (_messageFormat.Field("TwoPierCode").Length +
                                _messageFormat.Field("Destination").Length));
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
                    seq = Functions.ConvertStringToFixLengthByteArray(
                            newSeq.ToString(), fieldLen, '0', Functions.PaddingRule.Left);
                }

                foreach (byte tempList in seq)
                {
                    msgSend.Add(tempList);
                }


                fieldLen = _messageFormat.Field("NoOfTwoPier").Length;
                noTwoPier = Functions.ConvertStringToFixLengthByteArray(NoOfTwoPier.ToString(),
                                fieldLen, '0', Functions.PaddingRule.Left);
                foreach (byte tempList in noTwoPier)
                {
                    msgSend.Add(tempList);
                }

                for (int i = 0; i < AllocationData.Length; i++)
                {
                    fieldLen = _messageFormat.Field("TwoPierCode").Length;
                    code = Functions.ConvertStringToFixLengthByteArray(AllocationData[i].Code.ToString(),
                                fieldLen, ' ', Functions.PaddingRule.Right);
                    foreach (byte tempList in code)
                    {
                        msgSend.Add(tempList);
                    }

                    fieldLen = _messageFormat.Field("Destination").Length;
                    dest = Functions.ConvertStringToFixLengthByteArray(AllocationData[i].Destination.ToString(),
                                fieldLen, ' ', Functions.PaddingRule.Right);
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
                    _logger.Error("Constructing TPTI message is failed! <" + thisMethod + ">", ex);

                return null;
            }
        }

        #endregion


    }
}
