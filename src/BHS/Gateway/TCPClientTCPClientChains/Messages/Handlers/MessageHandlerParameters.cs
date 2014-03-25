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

namespace BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers
{
    /// <summary>
    /// Parameter class used to store parameters of MessageHandler class.
    /// </summary>
    public class MessageHandlerParameters: PALS.Common.IParameters, IDisposable
    {
        #region Class Fields Declaration

        private const string APP_TELEGRAMSET = "Application_Telegrams";
        private const string MESSAGE_ALIAS_INTM = "INTM"; //Code: 0103, Intermediate message
        private const string MESSAGE_ALIAS_CSNF = "CSNF"; //Code: 0108, Connection Status Notification Message
        private const string MESSAGE_ALIAS_GRNF = "GRNF"; //Code: 0109, Gateway Ready Notification message
        private const string MESSAGE_ALIAS_SRQ = "SRQ";
        private const string MESSAGE_ALIAS_SRP = "SRP";

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
        /// <summary>
        /// Bag Event Message (remote) sender name.
        /// </summary>
        public string Receiver { get; set; }

        /// <summary>
        /// INTM Message type.
        /// </summary>
        public string MessageType_INTM { get; set; }
        /// <summary>
        /// INTM Message format.
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

        /// <summary>
        /// Status Request Message type.
        /// </summary>
        public string MessageType_SRQ { get; set; }        
        /// <summary>
        /// Status Request Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_SRQ { get; set; }

        /// <summary>
        /// Status Reply Message type.
        /// </summary>
        public string MessageType_SRP { get; set; }
        /// <summary>
        /// Status Reply Message format.
        /// </summary>
        public PALS.Telegrams.TelegramFormat MessageFormat_SRP { get; set; }

        #endregion

        #region Class Constructor & Destructor

        /// <summary>
        /// Class constructer.
        /// </summary>
        public MessageHandlerParameters(XmlNode configSet, XmlNode telegramSet)
        {
            if (telegramSet == null)
                throw new Exception("Constractor parameter can not be null! Creating class " + _className +
                    " object fail! <BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.MessageHandlerParameters.Constructor()>");

            if (configSet == null)
                throw new Exception("Constractor parameter can not be null! Creating class " + _className +
                    " object fail! <BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.MessageHandlerParameters.Constructor()>");

            if (Init(ref configSet, ref telegramSet) == false)
                    throw new Exception("Instantiate class object failure! " +
                        "<BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.MessageHandlerParameters.Constructor()>");
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
            MessageFormat_INTM = null;
            MessageFormat_CSNF = null;
            MessageFormat_GRNF = null;
            MessageFormat_SRQ = null;
            MessageFormat_SRP = null;
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

            MessageType_INTM = string.Empty;
            MessageType_CSNF = string.Empty;
            MessageType_GRNF = string.Empty;
            MessageType_SRQ = string.Empty;
            MessageType_SRP = string.Empty;
            MessageFormat_INTM = null;
            MessageFormat_SRQ = null;
            MessageFormat_SRP = null;

            // <configSet name="BHS.Gateway.TCPClientTCPClientChains.Messages.Handlers.MessageHandler">
            //   <!-- BEV message (local) sender, max 8 characters-->
            //   <sender>MDSGW</sender>
            //   <!-- BEV message (remote) receiver, max 8 characters-->
            //   <receiver>CCTVENGN</receiver>
            // </configSet>
            Sender = XMLConfig.GetSettingFromInnerText(configSet, "sender", string.Empty);
            if (Sender == string.Empty)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("<sender> setting is missing in XML config file or its value is empty! <" + thisMethod + ">.");

                return false;
            }

            Receiver = XMLConfig.GetSettingFromInnerText(configSet, "receiver", string.Empty);
            if (Receiver == string.Empty)
            {
                if (_logger.IsErrorEnabled)
                    _logger.Error("<receiver> setting is missing in XML config file or its value is empty! <" + thisMethod + ">.");

                return false;
            }
                        
            //<telegramSet name="Application_Telegrams">
            //  <telegram alias="SRQ" name="Status_Request_Message" sequence="True" acknowledge="False">
            //    <field name="Type" offset="0" length="4" default="48,49,48,49"/>
            //    <field name="Length" offset="4" length="4" default="?"/>
            //    <field name="Sequence" offset="8" length="4" default="?"/>
            //    <field name="Class" offset="12" length="?" default="?"/>
            //  </telegram>
            //  <telegram alias="SRP" name="Status_Reply_Message" sequence="False" acknowledge="False">
            //    <field name="Type" offset="0" length="4" default="48,49,48,50"/>
            //    <field name="Length" offset="4" length="4" default="?"/>
            //    <field name="Sequence" offset="8" length="4" default="?"/>
            //    <field name="Status" offset="12" length="?" default="?"/>
            //  </telegram>
            //  <telegram alias="INTM" name="Intermediate_Message" sequence="True" acknowledge="True">
            //    <field name="Type" offset="0" length="4" default="48,49,48,51"/>
            //    <field name="Length" offset="4" length="4" default="?"/>
            //    <field name="Sequence" offset="8" length="4" default="?"/>
            //    <field name="Sender" offset="12" length="8" default="?"/>
            //    <field name="Receiver" offset="20" length="8" default="?"/>
            //    <field name="OriginMsgType" offset="28" length="4" default="?"/>
            //    <field name="OriginMsg" offset="32" length="?" default="?"/>
            //  </telegram>
            //  <telegram alias="GRNF" name="Gateway_Ready_Notification_Message" sequence="True" acknowledge="False">
            //    <field name="Type" offset="0" length="4" default="48,49,48,57"/>
            //    <field name="Length" offset="4" length="4" default="48,48,50,48"/>
            //    <field name="Sequence" offset="8" length="4" default="?"/>
            //    <field name="AppCode" offset="12" length="8" default="?"/>
            //  </telegram>
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
                XmlNode node;
                node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_SRQ);
                if (node == null)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("The format of " + MESSAGE_ALIAS_SRQ +
                            " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                    return false;
                }
                MessageFormat_SRQ = new PALS.Telegrams.TelegramFormat(ref node);
                MessageType_SRQ = Functions.ConvertByteArrayToString(
                        MessageFormat_SRQ.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);

                node = null;
                node = XMLConfig.GetConfigSetElement(ref teleSet, "telegram", "alias", MESSAGE_ALIAS_SRP);
                if (node == null)
                {
                    if (_logger.IsErrorEnabled)
                        _logger.Error("The format of " + MESSAGE_ALIAS_SRP +
                            " message is missing in the config set <" + telegramSet.Name + ">! <" + thisMethod + ">.");

                    return false;
                }
                MessageFormat_SRP = new PALS.Telegrams.TelegramFormat(ref node);
                MessageType_SRP = Functions.ConvertByteArrayToString(
                        MessageFormat_SRP.Field("Type").DefaultValue, -1, HexToStrMode.ToAscString);
                
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
            }
            
            return true;
        }

        #endregion

    }
}
