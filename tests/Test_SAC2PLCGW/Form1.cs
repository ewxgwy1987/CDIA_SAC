using System;
using System.Windows.Forms;
using BHS;
using PALS;

namespace Test_SAC2PLCGW
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private BHS.Gateway.TCPClientTCPClientChains.Application.Initializer _init;

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            listBox1.Items.Add("Application is being closed...");

            if (_init != null)
            {
                _init.Dispose();

                //MessageBox.Show("Application initializer has been destoryed!",
                //            "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            mnuInternal.BackColor = System.Drawing.Color.LightGray;
            mnuExternal.BackColor = System.Drawing.Color.LightGray;

            if (_init != null)
            {
                MessageBox.Show("Application has already been initialized!", "Warnning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string xmlSettingFile = PALS.Utilities.Functions.GetXMLFileFullName("PALS_BASE", "CFG_SCPLCEM1GW.xml", 5);
                if (xmlSettingFile == null)
                {
                    // Read XML configuration file from \CFG sub folder.
                    xmlSettingFile = PALS.Utilities.Functions.GetXMLFileFullName("PALS_BASE", @"cfg\CFG_SCPLCEM1GW.xml", 5);

                    if (xmlSettingFile == null)
                        throw new Exception("XML configuration file (CFG_SCPLCEM1GW.xml) could not be found!");
                }

                string xmlTelegramFile = PALS.Utilities.Functions.GetXMLFileFullName("PALS_BASE", "CFG_Telegrams.xml", 5);
                if (xmlTelegramFile == null)
                {
                    // Read XML configuration file from \CFG sub folder.
                    xmlTelegramFile = PALS.Utilities.Functions.GetXMLFileFullName("PALS_BASE", @"cfg\CFG_Telegrams.xml", 5);

                    if (xmlTelegramFile == null)
                        throw new Exception("XML configuration file (CFG_Telegrams.xml) could not be found!");
                }

                _init = new BHS.Gateway.TCPClientTCPClientChains.Application.Initializer(xmlSettingFile, xmlTelegramFile);
                if (_init.Init())
                {
                    _init.OnConnected += new EventHandler<BHS.MessageEventArgs>(Initializer_OnConnected);
                    _init.OnDisconnected += new EventHandler<BHS.MessageEventArgs>(Initializer_OnDisconnected);
                    _init.OnReceived += new EventHandler<BHS.MessageEventArgs>(Initializer_OnReceived);

                    listBox1.Items.Add("Application has been successfully initialized.");
                }
                else
                    throw new Exception("Application initialization failure!");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Class1 _cls1, _cls2, _cls3, _cls4;
        private void testing1byValueAndByReferenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _cls1 = new Class1("test1");
            _cls2 = _cls1;
            _cls3 = _cls2;
            _cls4 = _cls3;

            MessageBox.Show(string.Format("Code:\n_cls1 = new Class1(\"test1\");\n_cls2 = _cls1;\n" + 
                            "_cls3 = _cls2;\n_cls4 = _cls3;\n\n" + 
                            "Result:\ncls1.Name = {0} \ncls2.Name = {1}\ncls3.Name = {2}\ncls4.Name = {3}",
                            _cls1.Name, _cls2.Name, _cls3.Name, _cls4.Name));

            _cls1.Name = "test2";
            MessageBox.Show(string.Format("Code:\n_cls1 = new Class1(\"test1\");\n_cls2 = _cls1;\n" +
                            "_cls3 = _cls2;\n_cls4 = _cls3;\n_cls1.Name = \"test2\";\n\n" +
                            "Result:\ncls1.Name = {0} \ncls2.Name = {1}\ncls3.Name = {2}\ncls4.Name = {3}",
                            _cls1.Name, _cls2.Name, _cls3.Name, _cls4.Name));

            Class1 cls = new Class1("test3");
            _cls1 = cls;
            MessageBox.Show(string.Format("Code:\n_cls1 = new Class1(\"test1\");\n_cls2 = _cls1;\n" +
                            "_cls3 = _cls2;\n_cls4 = _cls3;\nClass1 cls = new Class1(\"test3\");\n_cls1 = cls;\n\n" +
                            "Result:\ncls1.Name = {0} \ncls2.Name = {1}\ncls3.Name = {2}\ncls4.Name = {3}",
                            _cls1.Name, _cls2.Name, _cls3.Name, _cls4.Name));
        }

        private void mnuClossEngineCom_Click(object sender, EventArgs e)
        {
            _init.MsgHandler.DisconnectToInternal(string.Empty);
        }

        private void mnuCloseCCTVCom_Click(object sender, EventArgs e)
        {
            _init.MsgHandler.DisconnectToExternal(string.Empty);
        }

        private void btnToEnge_Click(object sender, EventArgs e)
        {
            byte[] data;
            data = System.Text.ASCIIEncoding.ASCII.GetBytes(txtData.Text);

            listBox1.Items.Insert(0, "-> [Internal] MSG:<" +
                    PALS.Utilities.Functions.ConvertByteArrayToString(
                        data, -1, PALS.Utilities.HexToStrMode.ToAscPaddedHexString) + ">");

            _init.MsgHandler.SentToInternal(data);
        }

        private void btnToCCTV_Click(object sender, EventArgs e)
        {
            byte[] data;
            data = System.Text.ASCIIEncoding.ASCII.GetBytes(txtData.Text);

            listBox1.Items.Insert(0, "-> [External] MSG:<" +
                    PALS.Utilities.Functions.ConvertByteArrayToString(
                        data, -1, PALS.Utilities.HexToStrMode.ToAscPaddedHexString) + ">");

            _init.MsgHandler.SentToExternal(data);
        }

        private void Initializer_OnConnected(object sender, MessageEventArgs e)
        {
            DisplayCommStatus(menuStrip1, e);
        }

        private void Initializer_OnDisconnected(object sender, MessageEventArgs e)
        {
            DisplayCommStatus(menuStrip1, e);
        }

        private void Initializer_OnReceived(object sender, MessageEventArgs e)
        {
            DisplayIncomingMessage(listBox1, e);
        }

        public delegate void DisplayCommStatusDelegate(MenuStrip ms, MessageEventArgs e);
        private void DisplayCommStatus(MenuStrip ms, MessageEventArgs e)
        {
            if (ms.InvokeRequired)
            {
                DisplayCommStatusDelegate deleg = new DisplayCommStatusDelegate(DisplayCommStatus);

                this.Invoke(deleg, new object[] { ms, e });
            }
            else
            {
                if (e.ChainName.CompareTo("GW2INTERNAL") == 0)
                {
                    mnuInternal.Text = "Internal <" + e.OpenedChannelCount + ">";

                    if (e.OpenedChannelCount == 0)
                        ms.Items["mnuInternal"].BackColor = System.Drawing.Color.Red;
                    else
                        ms.Items["mnuInternal"].BackColor = System.Drawing.Color.Lime;
                }
                else
                {
                    mnuExternal.Text = "External <" + e.OpenedChannelCount + ">";

                    if (e.OpenedChannelCount == 0)
                        ms.Items["mnuExternal"].BackColor = System.Drawing.Color.Red;
                    else
                        ms.Items["mnuExternal"].BackColor = System.Drawing.Color.Lime;
                }
            }
        }
        
        public delegate void DisplayIncomingMessageDelegate(ListBox lstBox, MessageEventArgs e);
        private void DisplayIncomingMessage(ListBox lstBox, MessageEventArgs e)
        {
            if (lstBox.InvokeRequired)
            {
                DisplayIncomingMessageDelegate deleg = new DisplayIncomingMessageDelegate(DisplayIncomingMessage);

                this.Invoke(deleg, new object[] { lstBox, e });
            }
            else
            {
                if (lstBox.Items.Count > 100)
                    lstBox.Items.Clear();

                lstBox.Items.Insert(0, "<- [" + e.ChannelName + "] MSG:<" +
                        PALS.Utilities.Functions.ConvertByteArrayToString(
                            e.Message.RawData, -1, PALS.Utilities.HexToStrMode.ToAscPaddedHexString) + ">");
            }
        }
    }
}
