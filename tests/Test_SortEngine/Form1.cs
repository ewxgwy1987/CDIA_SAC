using System;
using System.Windows.Forms;
using BHS;
using PALS;

namespace Test_SortEngine
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

        private BHS.Engine.TCPClientChains.Application.Initializer _init;

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
            mnuComm.BackColor = System.Drawing.Color.LightGray;

            // MessageBox.Show(DateTime.Now.ToString("yyyyMMdd-hh:mm:ss.fff"));

            if (_init != null)
            {
                MessageBox.Show("Application has already been initialized!", "Warnning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string xmlSettingFile = PALS.Utilities.Functions.GetXMLFileFullName("PALS_BASE", "CFG_SortEngine.xml", 5);
                if (xmlSettingFile == null)
                {
                    // Read XML configuration file from \CFG sub folder.
                    xmlSettingFile = PALS.Utilities.Functions.GetXMLFileFullName("PALS_BASE", @"cfg\CFG_SortEngine.xml", 5);

                    if (xmlSettingFile == null)
                        throw new Exception("XML configuration file (CFG_SortEngine.xml) could not be found!");
                }

                string xmlTelegramFile = PALS.Utilities.Functions.GetXMLFileFullName("PALS_BASE", "CFG_Telegrams.xml", 5);
                if (xmlTelegramFile == null)
                {
                    // Read XML configuration file from \CFG sub folder.
                    xmlTelegramFile = PALS.Utilities.Functions.GetXMLFileFullName("PALS_BASE", @"cfg\CFG_Telegrams.xml", 5);

                    if (xmlTelegramFile == null)
                        throw new Exception("XML configuration file (CFG_Telegrams.xml) could not be found!");
                }

                _init = new BHS.Engine.TCPClientChains.Application.Initializer(xmlSettingFile, xmlTelegramFile);
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
                if (e.OpenedChannelCount == 0)
                {
                    ms.Items["mnuComm"].Text = "Closed";
                    ms.Items["mnuComm"].BackColor = System.Drawing.Color.Red;
                }
                else
                {
                    ms.Items["mnuComm"].Text = "Opened";
                    ms.Items["mnuComm"].BackColor = System.Drawing.Color.Lime;
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

        private void mnuClossComm_Click(object sender, EventArgs e)
        {
            _init.MsgHandler.Disconnect(string.Empty);
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            byte[] data;
            data = System.Text.ASCIIEncoding.ASCII.GetBytes(txtData.Text);

            listBox1.Items.Insert(0, "-> [Internal] MSG:<" +
                    PALS.Utilities.Functions.ConvertByteArrayToString(
                        data, -1, PALS.Utilities.HexToStrMode.ToAscPaddedHexString) + ">");

            _init.MsgHandler.Send(data);
        }


    }
}
