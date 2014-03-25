#region Release Information
//
// =====================================================================================
// Copyright 2009, Xu Jian, All Rights Reserved.
// =====================================================================================
// FileName       MessageHandlerParameters.cs
// Revision:      1.0 -   07 May 2009, By Xu Jian
// =====================================================================================
//
#endregion

using System;
using System.Xml;
using PALS.Utilities;
using System.Collections.Generic;
using System.Collections;

namespace BHS.Engine.TCPClientChains.Messages.Handlers
{
    /// <summary>
    /// Parameter class used to store parameters of MessageHandler class.
    /// </summary>
    public class MessageHandlerParameters: PALS.Common.IParameters, IDisposable
    {
        #region Class Fields Declaration

        private const string APP_TELEGRAMSET = "Application_Telegrams";
        //... Common Message ...
        private const string MESSAGE_ALIAS_HEADER = "Header"; //Message Header
        private const string MESSAGE_ALIAS_INTM = "INTM"; //Code: 0103, Intermediate message
        private const string MESSAGE_ALIAS_CSNF = "CSNF"; //Code: 0108, Connection Status Notification Message
        private const string MESSAGE_ALIAS_GRNF = "GRNF"; //Code: 0109, Gateway Ready Notification message
        //... SAC2PLC GW Interface Message ...
        private const string MESSAGE_ALIAS_GID = "GID"; //Code: 0003, GID Generated Message
        private const string MESSAGE_ALIAS_ICR = "ICR"; //Code: 0004, Item Screened Message
        private const string MESSAGE_ALIAS_ISC = "ISC"; //Code: 0005, Item Scanned Message
        private const string MESSAGE_ALIAS_BMAM = "BMAM"; //Code: 0006, Baggage Measurement Array Message
        private const string MESSAGE_ALIAS_IRD = "IRD"; //Code 0007, Item Redirect Message
        private const string MESSAGE_ALIAS_ISE = "ISE"; //Code: 0008, Item Sort Event Message
        private const string MESSAGE_ALIAS_IPR = "IPR"; //Code: 0009, Item Proceeded Message
        private const string MESSAGE_ALIAS_ILT = "ILT"; //Code: 0010, Item Lost Message
        private const string MESSAGE_ALIAS_ITI = "ITI"; //Code: 0011, Item Tracking Information Message
        private const string MESSAGE_ALIAS_MER = "MER"; //Code: 0012, Item Manual EncodingRequest Message
        private const string MESSAGE_ALIAS_AFAI = "AFAI"; //Code: 0013, Airport Code and Function Allocation Information Message
         private const string MESSAGE_ALIAS_CRAI = "CRAI"; //Code: 0014, Carrier Allocation Information Message
        private const string MESSAGE_ALIAS_FBTI = "FBTI"; //Code: 0015, Fallback Tag Information Message
        private const string MESSAGE_ALIAS_FPTI = "FPTI"; //Code: 0016, Four Pier Tag Information Message
        private const string MESSAGE_ALIAS_P1500 = "1500P"; //Code: 0017, 1500P Information Message
        
        //private const string MESSAGE_ALIAS_TPTI = "TPTI"; //Code: 0022, Two Pier Tag Information Message
        //private const string MESSAGE_ALIAS_BSDI = "BSDI"; //Code: 0024, Bag Status Display Information Message
        //private const string MESSAGE_ALIAS_DLPS = "DLPS"; //Code: 0026, Duplicate License Plate Status Message

        #region Unused Const Fields by this project. But they may be need by other projects.
        //... Sortation Related Message ...
        //private const string MESSAGE_ALIAS_IRD = "IRD"; //Code: 0006, Item Redirect Message
        //private const string MESSAGE_ALIAS_CSR = "CSR"; //Code: 0011, Chute Status Request Message
        //private const string MESSAGE_ALIAS_CST = "CST"; //Code: 0012, Chute Status Reply Message
        //private const string MESSAGE_ALIAS_IDR = "IDR"; //Code: 0013, Item Destination Request Message
        //private const string MESSAGE_ALIAS_LRQ = "LRQ"; //Code: 0014, Baggage License Plate Request Message
        //private const string MESSAGE_ALIAS_IRP = "IRP"; //Code: 0015, Baggage License Plate Reply Message
        //private const string MESSAGE_ALIAS_IER = "IER"; //Code: 0016, Item Manual Encoding Request Message
        //... Common Message ...
        //private const string MESSAGE_ALIAS_SRQ = "SRQ"; //Code: 0101, Running Status Request message
        //private const string MESSAGE_ALIAS_SRP = "SRP"; //Code: 0102, Running Status Reply message
        //private const string MESSAGE_ALIAS_STR = "STR"; //Code: 0104, Service Start Command Message
        //private const string MESSAGE_ALIAS_STO = "STO"; //Code: 0105, Service Stop Command Message
        //private const string MESSAGE_ALIAS_PCN = "PCN"; //Code: 0106, Parameter Change Notification Message
        //private const string MESSAGE_ALIAS_PNA = "PNA"; //Code: 0107, Parameter Change Notification Acknowledge Message
        //... MES Message ...
        //private const string MESSAGE_ALIAS_IRY = "IRY"; //Code: 0201, Item Ready message
        //private const string MESSAGE_ALIAS_IEC = "IEC"; //Code: 0202, Item Encoded message
        //private const string MESSAGE_ALIAS_IRM = "IRM"; //Code: 0203, Item Removed message
        #endregion

        // The name of current class 
        private static readonly string _className =
                    System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.ToString();
        // Create a logger for use in this class
        private static readonly log4net.ILog _logger =
                    log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Bag Event Message (local) sender name.
        /// </summary>
        public string Sender { get; set; }

        ///// <summary>
        ///// Bag Event Message (remote) sender name.
        ///// </summary>
        //public string Receiver { get; set; }

        /// <summary>
        /// Bag Event Message - AFAI (remote) sender name.
        /// </summary>
        public List<string> AFAI_Receiver { get; set; }

        /// <summary>
        /// Bag Event Message - CRAI (remote) sender name.
        /// </summary>
        public List<string> CRAI_Receiver { get; set; }

        /// <summary>
        /// Bag Event Message - FBTI (remote) sender name.
        /// </summary>
        public List<string> FBTI_Receiver { get; set; }

        /// <summary>
        /// Bag Event Message - FPTI (remote) sender name.
        /// </summary>
        public List<string> FPTI_Receiver { get; set; }

        /// <summary>
        /// Bag Event Message - TPTI (remote) sender name.
        /// </summary>
        public List<string> TPTI_Receiver { get; set; }

        /// <summary>
        /// Enable Sending from SortEngn or not.
        /// </summary>
        public bool IsEnable { get; set; }

        /// <summary>
        /// Enable Sending DLPS from SortEngn or not.
        /// </summary>
        public bool EnableSendingDLPS { get; set; }

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
        /// IRD Subscriber ATRs
        /// </summary>
        public List<string> IRDSubscriberATR { get; set; }

        /// <summary>
        /// Enable Sending of IRD from SAC Engine
        /// </summary>
        public bool EnabledSendingIRD { get; set; } 

        /// <summary>
        /// GID Message type.
        /// </summary>
        public string MessageType_GID { get; set; }
        /// <summary>
        /// GID Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_GID { get; set; }

        /// <summary>
        /// ICR Message type.
        /// </summary>
        public string MessageType_ICR { get; set; }
        /// <summary>
        /// ICR Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_ICR { get; set; }

        /// <summary>
        /// ISC Message type.
        /// </summary>
        public string MessageType_ISC { get; set; }
        /// <summary>
        /// ISC Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_ISC { get; set; }

        /// <summary>
        /// ISE Message type.
        /// </summary>
        public string MessageType_ISE { get; set; }
        /// <summary>
        /// ISE Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_ISE { get; set; }

        /// <summary>
        /// IPR Message type.
        /// </summary>
        public string MessageType_IPR { get; set; }
        /// <summary>
        /// IPR Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_IPR { get; set; }

        /// <summary>
        /// ILT Message type.
        /// </summary>
        public string MessageType_ILT { get; set; }
        /// <summary>
        /// ILT Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_ILT { get; set; }

        /// <summary>
        /// ITI Message type.
        /// </summary>
        public string MessageType_ITI { get; set; }
        /// <summary>
        /// ITI Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_ITI { get; set; }

        /// <summary>
        /// MER Message type.
        /// </summary>
        public string MessageType_MER { get; set; }
        /// <summary>
        /// MER Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_MER { get; set; }

        /// <summary>
        /// AFAI Message type.
        /// </summary>
        public string MessageType_AFAI { get; set; }
        /// <summary>
        /// AFAI Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_AFAI { get; set; }

        /// <summary>
        /// BMAM Message type.
        /// </summary>
        public string MessageType_BMAM { get; set; }
        /// <summary>
        /// BMAM Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_BMAM { get; set; }

        /// <summary>
        /// CRAI Message type.
        /// </summary>
        public string MessageType_CRAI { get; set; }
        /// <summary>
        /// CRAI Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_CRAI { get; set; }

        /// <summary>
        /// FBTI Message type.
        /// </summary>
        public string MessageType_FBTI { get; set; }
        /// <summary>
        /// FBTI Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_FBTI { get; set; }

        /// <summary>
        /// FPTI Message type.
        /// </summary>
        public string MessageType_FPTI { get; set; }
        /// <summary>
        /// FPTI Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_FPTI { get; set; }

        /// <summary>
        /// P1500 Message type.
        /// </summary>
        public string MessageType_P1500 { get; set; }
        /// <summary>
        /// PV1K Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_P1500 { get; set; }

        /// <summary>
        /// DLPS Message type.
        /// </summary>
        public string MessageType_DLPS { get; set; }
        /// <summary>
        /// DLPS Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_DLPS { get; set; }

        /// <summary>
        /// IRD Message type.
        /// </summary>
        public string MessageType_IRD { get; set; }
        /// <summary>
        /// IRD Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_IRD { get; set; }

        #region Unused Class properties by this project. But they may be need by other projects.
        
        ///// <summary>
        ///// CSR Message type.
        ///// </summary>
        //public string MessageType_CSR { get; set; }
        ///// <summary>
        ///// CSR Message format.
        ///// </summary>
        //public PALS.Telegrams.TelegramFormat MessageFormat_CSR { get; set; }

        ///// <summary>
        ///// CST Message type.
        ///// </summary>
        //public string MessageType_CST { get; set; }
        ///// <summary>
        ///// CST Message format.
        ///// </summary>
        //public PALS.Telegrams.TelegramFormat MessageFormat_CST { get; set; }

        ///// <summary>
        ///// IDR Message type.
        ///// </summary>
        //public string MessageType_IDR { get; set; }
        ///// <summary>
        ///// IDR Message format.
        ///// </summary>
        //public PALS.Telegrams.TelegramFormat MessageFormat_IDR { get; set; }

        ///// <summary>
        ///// LRQ Message type.
        ///// </summary>
        //public string MessageType_LRQ { get; set; }
        ///// <summary>
        ///// LRQ Message format.
        ///// </summary>
        //public PALS.Telegrams.TelegramFormat MessageFormat_LRQ { get; set; }

        ///// <summary>
        ///// IRP Message type.
        ///// </summary>
        //public string MessageType_IRP { get; set; }
        ///// <summary>
        ///// IRP Message format.
        ///// </summary>
        //public PALS.Telegrams.TelegramFormat MessageFormat_IRP { get; set; }

        ///// <summary>
        ///// IER Message type.
        ///// </summary>
        //public string MessageType_IER { get; set; }
        ///// <summary>
        ///// IER Message format.
        ///// </summary>
        //public PALS.Telegrams.TelegramFormat MessageFormat_IER { get; set; }

        ///// <summary>
        ///// SRQ Message type.
        ///// </summary>
        //public string MessageType_SRQ { get; set; }
        ///// <summary>
        ///// SRQ Message format.
        ///// </summary>
        //public PALS.Telegrams.TelegramFormat MessageFormat_SRQ { get; set; }

        ///// <summary>
        ///// SRP Message type.
        ///// </summary>
        //public string MessageType_SRP { get; set; }
        ///// <summary>
        ///// SRP Message format.
        ///// </summary>
        //public PALS.Telegrams.TelegramFormat MessageFormat_SRP { get; set; }

        ///// <summary>
        ///// STR Message type.
        ///// </summary>
        //public string MessageType_STR { get; set; }
        ///// <summary>
        ///// STR Message format.
        ///// </summary>
        //public PALS.Telegrams.TelegramFormat MessageFormat_STR { get; set; }

        ///// <summary>
        ///// STO Message type.
        ///// </summary>
        //public string MessageType_STO { get; set; }
        ///// <summary>
        ///// STO Message format.
        ///// </summary>
        //public PALS.Telegrams.TelegramFormat MessageFormat_STO { get; set; }

        ///// <summary>
        ///// PCN Message type.
        ///// </summary>
        //public string MessageType_PCN { get; set; }
        ///// <summary>
        ///// PCN Message format.
        ///// </summary>
        //public PALS.Telegrams.TelegramFormat MessageFormat_PCN { get; set; }

        ///// <summary>
        ///// PNA Message type.
        ///// </summary>
        //public string MessageType_PNA { get; set; }
        ///// <summary>
        ///// PNA Message format.
        ///// </summary>
        //public PALS.Telegrams.TelegramFormat MessageFormat_PNA { get; set; }

        ///// <summary>
        ///// IRY Message type.
        ///// </summary>
        //public string MessageType_IRY { get; set; }
        ///// <summary>
        ///// IRY Message format.
        ///// </summary>
        //public PALS.Telegrams.TelegramFormat MessageFormat_IRY { get; set; }

        ///// <summary>
        ///// IEC Message type.
        ///// </summary>
        //public string MessageType_IEC { get; set; }
        ///// <summary>
        ///// IEC Message format.
        ///// </summary>
        //public PALS.Telegrams.TelegramFormat MessageFormat_IEC { get; set; }

        ///// <summary>
        ///// IRM Message type.
        ///// </summary>
        //public string MessageType_IRM { get; set; }
        ///// <summary>
        ///// IRM Message format.
        ///// </summary>
        //public PALS.Telegrams.TelegramFormat MessageFormat_IRM { get; set; }
        #endregion

        /// <summary>
        /// Message header format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_Header { get; set; }
                
        /// <summary>
        /// Bag Event Message type.
        /// </summary>
        public string MessageType_INTM { get; set; }
        /// <summary>
        /// Bag Event Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_INTM { get; set; }
                
        /// <summary>
        /// CSNF Message type.
        /// </summary>
        public string MessageType_CSNF { get; set; }
        /// <summary>
        /// CSNF Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_CSNF { get; set; }
                
        /// <summary>
        /// GRNF Message type.
        /// </summary>
        public string MessageType_GRNF { get; set; }
        /// <summary>
        /// GRNF Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_GRNF { get; set; }
        
        #endregion

        #region Class Constructor & Destructor

        /// <summary>
        /// Class constructer.
        /// </summary>
        public MessageHandlerParameters(XmlNode configSet, XmlNode telegramSet)
        {
            if (telegramSet == null)
                throw new Exception("Constractor parameter can not be null! Creating class " + _className +
                    " object fail! <BHS.Engine.TCPClientChains.Messages.Handlers.MessageHandlerParameters.Constructor()>");

            if (configSet == null)
                throw new Exception("Constractor parameter can not be null! Creating class " + _className +
                    " object fail! <BHS.Engine.TCPClientChains.Messages.Handlers.MessageHandlerParameters.Constructor()>");

            if (Init(ref configSet, ref telegramSet) == false)
                    throw new Exception("Instantiate class object failure! " +
                        "<BHS.Engine.TCPClientChains.Messages.Handlers.MessageHandlerParameters.Constructor()>");
        }

        /// <summary>
        /// Class destructer.
        /// </summary>
        ~MessageHandlerParameters()
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
            MessageFormat_GID = null;
            MessageFormat_ICR = null;
            MessageFormat_ISC = null;
            MessageFormat_IRD = null;
            MessageFormat_ISE = null;
            MessageFormat_IPR = null;
            MessageFormat_ILT = null;
            MessageFormat_ITI = null;
            MessageFormat_MER = null;
            MessageFormat_AFAI = null;
            MessageFormat_BMAM = null;
            MessageFormat_CRAI= null;
            MessageFormat_FBTI = null;
            MessageFormat_FPTI = null;
            MessageFormat_P1500 = null;
            //MessageFormat_BSDI = null;
            
            MessageFormat_Header = null;
            MessageFormat_INTM = null;
            MessageFormat_CSNF = null;
            MessageFormat_GRNF = null;
        }

        #endregion

        #region Class Properties

        #endregion

        #region Class Methods

        /// <summary>
        /// Class Initialization.
        /// </summary>
        /// <param name="configSet"></param>
        /// <param name="telegramSet"></param>
        /// <returns></returns>
        public bool Init(ref XmlNode configSet, ref XmlNode telegramSet)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";

            MessageType_GID = string.Empty;
            MessageType_ICR = string.Empty;
            MessageType_ISC = string.Empty;
            MessageType_IRD = string.Empty;
            MessageType_ISE = string.Empty;
            MessageType_IPR = string.Empty;
            MessageType_ILT = string.Empty;
            MessageType_ITI = string.Empty;
            //MessageType_CSR = string.Empty;
            //MessageType_CST = string.Empty;
            //MessageType_IDR = string.Empty;
            //MessageType_LRQ = string.Empty;
            //MessageType_IRP = string.Empty;
            //MessageType_IER = string.Empty;
            MessageType_MER = string.Empty;
            MessageType_AFAI = string.Empty;
            MessageType_BMAM = string.Empty;
            MessageType_CRAI = string.Empty;
            MessageType_FBTI = string.Empty;
            MessageType_FPTI = string.Empty;
            //MessageType_TPTI = string.Empty;
            MessageType_P1500 = string.Empty;
            //MessageType_BSDI = string.Empty;
            MessageType_DLPS = string.Empty;
            //MessageType_SRQ = string.Empty;
            //MessageType_SRP = string.Empty;
            //MessageType_STR = string.Empty;
            //MessageType_STO = string.Empty;
            //MessageType_PCN = string.Empty;
            //MessageType_PNA = string.Empty;
            //MessageType_IRY = string.Empty;
            //MessageType_IEC = string.Empty;
            //MessageType_IRM = string.Empty;
            MessageType_INTM = string.Empty;
            MessageType_CSNF = string.Empty;
            MessageType_GRNF = string.Empty;

            MessageFormat_GID = null;
            MessageFormat_ICR = null;
            MessageFormat_ISC = null;
            MessageFormat_IRD = null;
            MessageFormat_ISE = null;
            MessageFormat_IPR = null;
            MessageFormat_ILT = null;
            MessageFormat_ITI = null;
            //MessageFormat_CSR = null;
            //MessageFormat_CST = null;
            //MessageFormat_IDR = null;
            //MessageFormat_LRQ = null;
            //MessageFormat_IRP = null;
            //MessageFormat_IER = null;
            MessageFormat_MER = null;
            MessageFormat_AFAI = null;
            MessageFormat_BMAM = null;
            MessageFormat_CRAI = null;
            MessageFormat_FBTI = null;
            MessageFormat_FPTI = null;
            MessageFormat_P1500 = null;
            //MessageFormat_BSDI = null;
            //MessageFormat_SRQ = null;
            //MessageFormat_SRP = null;
            //MessageFormat_STR = null;
            //MessageFormat_STO = null;
            //MessageFormat_PCN = null;
            //MessageFormat_PNA = null;
            //MessageFormat_IRY = null;
            //MessageFormat_IEC = null;
            //MessageFormat_IRM = null;
            MessageFormat_Header = null;
            MessageFormat_INTM = null;
            MessageFormat_CSNF = null;
            MessageFormat_GRNF = null;

            try
            {
              //<configSet name="BHS.Engine.TCPClientChains.Messages.Handlers.MessageHandler">
              //  <!-- INTM message (local) sender, max 8 characters-->
              //  <sender>SORTENGN</sender>
              //  <!-- INTM message (remote) receiver, max 8 characters-->
              //  <afai_Receiver>SAC2PLC1,SAC2PLC2,SAC2PLC3,SAC2PLC4,SAC2PLC5,SAC2PLC6</afai_Receiver>
              //  <crai_Receiver>SAC2PLC1,SAC2PLC2,SAC2PLC3,SAC2PLC4,SAC2PLC5,SAC2PLC6</crai_Receiver>
              //  <fbti_Receiver>SAC2PLC1,SAC2PLC2,SAC2PLC3,SAC2PLC4,SAC2PLC5,SAC2PLC6</fbti_Receiver>
              //  <fpti_Receiver>SAC2PLC1,SAC2PLC2,SAC2PLC3,SAC2PLC4,SAC2PLC5,SAC2PLC6</fpti_Receiver>
              //  <tpti_Receiver>SAC2PLC1,SAC2PLC2,SAC2PLC3,SAC2PLC4,SAC2PLC5,SAC2PLC6</tpti_Receiver>  
              //</configSet>

                Sender = XMLConfig.GetSettingFromInnerText(configSet, "sender", string.Empty);
                if (Sender == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<sender> setting is missing in XML config file or its value is empty! <" + thisMethod + ">.");

                    return false;
                }

                string tempReceiver = string.Empty;

                tempReceiver = XMLConfig.GetSettingFromInnerText(configSet, "afai_Receiver", string.Empty);
                if (tempReceiver == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<afai_Receiver> setting is missing in XML config file or its value is empty! <" + thisMethod + ">.");

                    return false;
                }
                else
                {
                    string[] tempWords;
                    tempWords = tempReceiver.Split(',');
                    AFAI_Receiver = new List<string>();

                    foreach (string word in tempWords)
                    {
                        AFAI_Receiver.Add(word);
                    }                 
                }

                
                tempReceiver = string.Empty;
                tempReceiver = XMLConfig.GetSettingFromInnerText(configSet, "crai_Receiver", string.Empty);
                if (tempReceiver == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<crai_Receiver> setting is missing in XML config file or its value is empty! <" + thisMethod + ">.");

                    return false;
                }
                else
                {
                    string[] tempWords;
                    tempWords = tempReceiver.Split(',');

                    CRAI_Receiver = new List<string>();
                    foreach (string word in tempWords)
                    {
                        CRAI_Receiver.Add(word);
                    }
                }

                tempReceiver = string.Empty;
                tempReceiver = XMLConfig.GetSettingFromInnerText(configSet, "fbti_Receiver", string.Empty);
                if (tempReceiver == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<fbti_Receiver> setting is missing in XML config file or its value is empty! <" + thisMethod + ">.");

                    return false;
                }
                else
                {
                    string[] tempWords;
                    tempWords = tempReceiver.Split(',');
                    FBTI_Receiver = new List<string>();

                    foreach (string word in tempWords)
                    {
                        FBTI_Receiver.Add(word);
                    }
                }

                tempReceiver = string.Empty;
                tempReceiver = XMLConfig.GetSettingFromInnerText(configSet, "fpti_Receiver", string.Empty);
                if (tempReceiver == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<fpti_Receiver> setting is missing in XML config file or its value is empty! <" + thisMethod + ">.");

                    return false;
                }
                else
                {
                    string[] tempWords;
                    tempWords = tempReceiver.Split(',');
                    FPTI_Receiver = new List<string>();
                    foreach (string word in tempWords)
                    {
                        FPTI_Receiver.Add(word);
                    }
                }

                //tempReceiver = string.Empty;
                //tempReceiver = XMLConfig.GetSettingFromInnerText(configSet, "tpti_Receiver", string.Empty);
                //if (tempReceiver == string.Empty)
                //{
                //    if (_logger.IsErrorEnabled)
                //        _logger.Error("<tpti_Receiver> setting is missing in XML config file or its value is empty! <" + thisMethod + ">.");

                //    return false;
                //}
                //else
                //{
                //    string[] tempWords;
                //    tempWords = tempReceiver.Split(',');
                //    TPTI_Receiver = new List<string>();
                //    foreach (string word in tempWords)
                //    {
                //        TPTI_Receiver.Add(word);
                //    }
                //}

                String stringIsEnable = string.Empty;
                stringIsEnable = XMLConfig.GetSettingFromInnerText(configSet, "enable_Sending", string.Empty);
                if (stringIsEnable == String.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<enable_Sending> setting is missing in XML config file or its value is empty! <" + thisMethod + ">.");

                    return false;
                }
                else
                {
                    if ((stringIsEnable == "True") || (stringIsEnable == "TRUE") || (stringIsEnable == "true"))
                    {
                        IsEnable = true;
                    }
                    else if ((stringIsEnable == "False") || (stringIsEnable == "FALSE") || (stringIsEnable == "false"))
                    {
                        IsEnable = false;
                    }

                }

                stringIsEnable = XMLConfig.GetSettingFromInnerText(configSet, "enable_Sending_DLPS", string.Empty).ToUpper();
                if (stringIsEnable == String.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<enable_Sending_DLPS> setting is missing in XML config file or its value is empty! <" + thisMethod + ">.");

                    return false;
                }
                else
                {
                    if (stringIsEnable == "TRUE") 
                    {
                        EnableSendingDLPS = true;
                    }
                    else
                    {
                        EnableSendingDLPS = false;
                    }

                }


                string temp = string.Empty;
                temp = XMLConfig.GetSettingFromInnerText(configSet, "dlps_Max_Records", string.Empty);
                if (temp == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<dlps_Max_Records> setting is missing in XML config file or its value is empty! <" + thisMethod + ">.");

                    return false;
                }
                else
                {
                    DLPSMaxRecords = Convert.ToInt32(temp);
                }

                DLPSStatusSingle = XMLConfig.GetSettingFromInnerText(configSet, "dlps_Status_Single", string.Empty);
                if (temp == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<dlps_Status_Single> setting is missing in XML config file or its value is empty! <" + thisMethod + ">.");

                    return false;
                }

                DLPSStatusDuplicate = XMLConfig.GetSettingFromInnerText(configSet, "dlps_Status_Duplicate", string.Empty);
                if (temp == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<dlps_Status_Duplicate> setting is missing in XML config file or its value is empty! <" + thisMethod + ">.");

                    return false;
                } 
                

                temp = XMLConfig.GetSettingFromInnerText(configSet, "dlps_Subscriber_ATR", string.Empty);
                if (temp == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<dlps_Subscriber_ATR> setting is missing in XML config file or its value is empty! <" + thisMethod + ">.");

                    return false;
                }
                else
                {
                    string[] arrayTemp = null;
                    DLPSSubscriberATR = new List<string>();
                    arrayTemp = temp.Split(',');

                    foreach (string subscriber in arrayTemp)
                    {
                        DLPSSubscriberATR.Add(subscriber);
                    }
                }

                temp = XMLConfig.GetSettingFromInnerText(configSet, "ird_Subscriber", string.Empty);
                if (temp == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<ird_Subscriber>setting is missing in XML config file or its value is empty! <" + thisMethod + ">.");

                    return false;
                }
                else
                {
                    string[] arrayTemp = null;
                    IRDSubscriberATR = new List<string>();
                    arrayTemp = temp.Split(',');

                    foreach (string subscriber in arrayTemp)
                    {
                        IRDSubscriberATR.Add(subscriber);
                    }
                }

                temp = XMLConfig.GetSettingFromInnerText(configSet, "enable_Sending_IRD", string.Empty).ToUpper();
                if (temp == string.Empty)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("<enabled_Sending_IRD>setting is missing in XML config file or its value is empty!<" + thisMethod + ">");

                    return false;
                }
                else
                {
                    if (temp.ToUpper() == "TRUE")
                    {
                        EnabledSendingIRD = true;
                    }
                    else
                    {
                        EnabledSendingIRD = false;
                    } 
                }

                //<telegramSet name="Application_Telegrams">
                //  <header alias="Header" name="App_Header" sequence="False" acknowledge="False">
                //    <field name="Type" offset="0" length="4" default=""/>
                //    <field name="Length" offset="4" length="4" default=""/>
                //    <field name="Sequence" offset="8" length="4" default=""/>
                //  </header>
                //  ...
                //  <telegram alias="INTM" name="Intermediate_Message" sequence="True" acknowledge="True">
                //    <field name="Type" offset="0" length="4" default="48,49,48,51"/>
                //    <field name="Length" offset="4" length="4" default="?"/>
                //    <field name="Sequence" offset="8" length="4" default="?"/>
                //    <field name="Sender" offset="12" length="8" default="?"/>
                //    <field name="Receiver" offset="20" length="8" default="?"/>
                //    <field name="OriginMsgType" offset="28" length="4" default="?"/>
                //    <field name="OriginMsg" offset="32" length="?" default="?"/>
                //  </telegram>
                //  ...
                //</telegramSet>
                XmlNode teleSet = PALS.Utilities.XMLConfig.GetConfigSetElement(ref telegramSet,
                            "telegramSet", "name", APP_TELEGRAMSET);
                if (teleSet == null)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("No <telegramSet> XmlNode whose [name] attribute is " +
                            "\"Application_Telegrams\" in the telegram format XML file! <" + thisMethod + ">.");

                    return false;
                }
                else
                {
                    if (GetMessageFormatSettings(telegramSet, teleSet) == false)
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("Initializing class setting is failed! <" + thisMethod + ">", ex);

                return false;
            }
        }

        private bool GetMessageFormatSettings(XmlNode telegramSet, XmlNode teleSet)
        {
            string thisMethod = _className + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + "()";
            XmlNode node;

            node = XMLConfig.GetConfigSetElement(ref teleSet, "header", "alias", MESSAGE_ALIAS_HEADER);
            if (node == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("The format of " + MESSAGE_ALIAS_HEADER +
                        " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                return false;
            }
            MessageFormat_Header = new PALS.Telegrams.TelegramFormat(ref node);

            node = null;
            node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_INTM);
            if (node == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("The format of " + MESSAGE_ALIAS_INTM +
                        " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                return false;
            }
            MessageFormat_INTM = new PALS.Telegrams.TelegramFormat(ref node);
            MessageType_INTM = Functions.ConvertByteArrayToString(
                    MessageFormat_INTM.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);

            node = null;
            node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_CSNF);
            if (node == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("The format of " + MESSAGE_ALIAS_CSNF +
                        " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                return false;
            }
            MessageFormat_CSNF = new PALS.Telegrams.TelegramFormat(ref node);
            MessageType_CSNF = Functions.ConvertByteArrayToString(
                    MessageFormat_CSNF.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);

            node = null;
            node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_GRNF);
            if (node == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("The format of " + MESSAGE_ALIAS_GRNF +
                        " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                return false;
            }

            MessageFormat_GRNF = new PALS.Telegrams.TelegramFormat(ref node);
            MessageType_GRNF = Functions.ConvertByteArrayToString(
                    MessageFormat_GRNF.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);

            node = null;
            node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_GID);
            if (node == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("The format of " + MESSAGE_ALIAS_GID +
                        " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                return false;
            }
            MessageFormat_GID = new PALS.Telegrams.TelegramFormat(ref node);
            MessageType_GID = Functions.ConvertByteArrayToString(
                    MessageFormat_GID.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);

            node = null;
            node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_ICR);
            if (node == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("The format of " + MESSAGE_ALIAS_ICR +
                        " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                return false;
            }
            MessageFormat_ICR = new PALS.Telegrams.TelegramFormat(ref node);
            MessageType_ICR = Functions.ConvertByteArrayToString(
                    MessageFormat_ICR.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);

            node = null;
            node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_ISC);
            if (node == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("The format of " + MESSAGE_ALIAS_ISC +
                        " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                return false;
            }
            MessageFormat_ISC = new PALS.Telegrams.TelegramFormat(ref node);
            MessageType_ISC = Functions.ConvertByteArrayToString(
                    MessageFormat_ISC.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);

            node = null;
            node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_ISE);
            if (node == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("The format of " + MESSAGE_ALIAS_ISE +
                        " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                return false;
            }
            MessageFormat_ISE = new PALS.Telegrams.TelegramFormat(ref node);
            MessageType_ISE = Functions.ConvertByteArrayToString(
                    MessageFormat_ISE.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);

            node = null;
            node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_IPR);
            if (node == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("The format of " + MESSAGE_ALIAS_IPR +
                        " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                return false;
            }
            MessageFormat_IPR = new PALS.Telegrams.TelegramFormat(ref node);
            MessageType_IPR = Functions.ConvertByteArrayToString(
                    MessageFormat_IPR.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);

            node = null;
            node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_ILT);
            if (node == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("The format of " + MESSAGE_ALIAS_ILT +
                        " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                return false;
            }
            MessageFormat_ILT = new PALS.Telegrams.TelegramFormat(ref node);
            MessageType_ILT = Functions.ConvertByteArrayToString(
                    MessageFormat_ILT.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);

            node = null;
            node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_ITI);
            if (node == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("The format of " + MESSAGE_ALIAS_ITI +
                        " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                return false;
            }
            MessageFormat_ITI = new PALS.Telegrams.TelegramFormat(ref node);
            MessageType_ITI = Functions.ConvertByteArrayToString(
                    MessageFormat_ITI.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);

            node = null;
            node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_MER);
            if (node == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("The format of " + MESSAGE_ALIAS_MER +
                        " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                return false;
            }
            MessageFormat_MER = new PALS.Telegrams.TelegramFormat(ref node);
            MessageType_MER = Functions.ConvertByteArrayToString(
                    MessageFormat_MER.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);

            node = null;
            node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_AFAI);
            if (node == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("The format of " + MESSAGE_ALIAS_AFAI +
                        " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                return false;
            }
            MessageFormat_AFAI = new PALS.Telegrams.TelegramFormat(ref node);
            MessageType_AFAI = Functions.ConvertByteArrayToString(
                    MessageFormat_AFAI.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);

            node = null;
            node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_BMAM);
            if (node == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("The format of " + MESSAGE_ALIAS_BMAM +
                        " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                return false;
            }
            MessageFormat_BMAM = new PALS.Telegrams.TelegramFormat(ref node);
            MessageType_BMAM = Functions.ConvertByteArrayToString(
                    MessageFormat_BMAM.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);

            node = null;
            node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_CRAI);
            if (node == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("The format of " + MESSAGE_ALIAS_CRAI +
                        " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                return false;
            }
            MessageFormat_CRAI = new PALS.Telegrams.TelegramFormat(ref node);
            MessageType_CRAI = Functions.ConvertByteArrayToString(
                    MessageFormat_CRAI.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);

            node = null;
            node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_FBTI);
            if (node == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("The format of " + MESSAGE_ALIAS_FBTI +
                        " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                return false;
            }
            MessageFormat_FBTI = new PALS.Telegrams.TelegramFormat(ref node);
            MessageType_FBTI = Functions.ConvertByteArrayToString(
                    MessageFormat_FBTI.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);

            node = null;
            node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_FPTI);
            if (node == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("The format of " + MESSAGE_ALIAS_FPTI +
                        " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                return false;
            }
            MessageFormat_FPTI = new PALS.Telegrams.TelegramFormat(ref node);
            MessageType_FPTI = Functions.ConvertByteArrayToString(
                    MessageFormat_FPTI.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);

            node = null;
            node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_P1500);
            if (node == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("The format of " + MESSAGE_ALIAS_P1500 +
                        " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                return false;
            }
            MessageFormat_P1500 = new PALS.Telegrams.TelegramFormat(ref node);
            MessageType_P1500 = Functions.ConvertByteArrayToString(
                    MessageFormat_P1500.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);

            node = null;
            node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_IRD);
            if (node == null)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("The format of " + MESSAGE_ALIAS_IRD +
                        " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

            }
            MessageFormat_IRD = new PALS.Telegrams.TelegramFormat(ref node);
            MessageType_IRD = Functions.ConvertByteArrayToString(MessageFormat_IRD.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);

            // ==================================================
            // Need add in codes here to read format settings for other messages needed by other project
            // ...
            // ==================================================

            return true;
        }

        #endregion

    }
}
