using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpringCard.LibCs;
using SpringCard.LibCs.Windows;
using SpringCard.LibCs.Windows.Forms;
using SpringCard.PCSC;
using SpringCard.GoogleVas;
using SpringCard.AppleVas;
using System.Threading;
using SpringCard.Licenses;
using SpringCard.Licenses.Forms;
using SmartTapRdr;
using PassKitRdr;
using System.Text.RegularExpressions;

namespace SpringPassRdr
{
    public partial class MainForm : Form
    {
        bool ShowSplash = true;

        SCardReader activeReader;
        GoogleVasConfig googleConfig;
        AppleVasTerminalConfig appleConfig;

        public MainForm(string[] args)
        {
            Logger.Trace("SpringPass starting");

			InitializeComponent();

            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string s = args[i].ToLower();
                    if ((s == "--no_splash") || (s == "-q"))
                    {
                        ShowSplash = false;
                    }
                }
            }

            Text = AppUtils.ApplicationTitle(true);
		}

        public bool LoadGoogleConfigFromFile(string FileName)
        {
            googleConfig = GoogleVasConfig.LoadFromJsonFile(FileName);
            return true;
        }

        public bool LoadAppleConfigFromFile(string FileName)
        {
            appleConfig = AppleVasTerminalConfig.LoadFromJsonFile(FileName);
            return true;
        }

        private void MainForm_Load(object sender, EventArgs e)
		{
            /* Load configurations */
            GoogleVASConfigForm googleConfigForm = new GoogleVASConfigForm();
            googleConfig = googleConfigForm.GetConfig();
            AppleVASConfigForm appleConfigForm = new AppleVASConfigForm();
            appleConfig = appleConfigForm.GetConfig();

            /* Load readers list */
            lkRefresh_LinkClicked(sender, null);
        }

        private bool CanPlay()
        {
            // TODO check apple and google config
            if (cbReaders.SelectedIndex >= 0)
                if (GoogleVasLicense.Loaded() && AppleVasLicense.Loaded())
                    return true;
            return false;
        }


        private void lkRefresh_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			string oldReader = AppConfig.ReadSettingString("Reader");
			string[] readers = SCARD.Readers;
			cbReaders.Items.Clear();
			if (readers != null)
				foreach (string reader in readers)
				{
					cbReaders.Items.Add(reader);
					if (reader == oldReader)
						cbReaders.SelectedIndex = cbReaders.Items.Count - 1;
				}
			btnPlay.Enabled = CanPlay();
		}

        private void cbReaders_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbReaders.SelectedIndex >= 0)
            {
                string reader = cbReaders.Text;
                Logger.Trace("Reader={0}", reader);
				AppConfig.WriteSettingString("Reader", reader);
            }
            btnPlay.Enabled = CanPlay();
        }

		private void EnableControls(bool yesno)
		{
			lkRefresh.Enabled = yesno;
            lkSetAppleConfig.Enabled = yesno;
            lkSetGoogleConfig.Enabled = yesno;
            lkLicense.Enabled = yesno;
			lkAbout.Enabled = yesno;
            lkSubscribe.Enabled = yesno;
			cbReaders.Enabled = yesno;
		}

		private void btnPlay_Click(object sender, EventArgs e)
        {
            if (cbReaders.SelectedIndex >= 0)
            {
				btnPlay.Enabled = false;
				EnableControls(false);

				string reader = cbReaders.Text;
                Logger.Trace("Starting...");

                ShowResult("Waiting for NFC mobile...", null, true);

                activeReader = new SCardReader(reader);
                activeReader.StartWaitCard(new SCardReader.CardConnectedCallback(CardConnectedCallback), new SCardReader.CardRemovedCallback(CardRemovedCallback));
                btnPause.Enabled = true;
            }
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            if (activeReader != null)
            {
                btnPause.Enabled = false;

				Logger.Trace("Stopping...");
                activeReader.StopWaitCard();
                activeReader = null;

                ShowResult("Press the \"Play\" button when ready.", null, false);

                EnableControls(true);
                btnPlay.Enabled = CanPlay();
            }
        }


        delegate void ShowResultInvoker(string StatusMsg, string DataMsg, bool CleanData);
        void ShowResult(string StatusMsg, string DataMsg, bool CleanData)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new ShowResultInvoker(ShowResult), StatusMsg, DataMsg, CleanData);
                return;
            }

            toolStripStatusLabel1.Text = StatusMsg;
            if (DataMsg != null)
            {
                eSpringPassData.Text = DataMsg;
            }
            else if (CleanData)
            {
                eSpringPassData.Text = "";
            }
        }

        void CardRemovedCallback()
        {
            ShowResult("Waiting for NFC mobile...", null, false);
        }

        void CardConnectedCallback(SCardChannel cardChannel)
        {
            /* The CardConnected function is called as a delegate (callback) by the SCardReader object */
            /* within its backgroung thread. Therefore it is not allowed to use the UI objects.        */

            Logger.Trace("Card connected - Trying to run the transaction");


            /* Apple exhange */

            if (!AppleVasLicense.ReadDeviceId(cardChannel))
            {
                Logger.Error("Read device ID error");
                ShowResult("Not a SpringCard device?", null, true);
                return;
            }

            if (!AppleVasLicense.Allowed(out string msg2))
            {
                Logger.Error("Not allowed to execute");
                ShowResult(string.Format("Not allowed to execute ({0})", msg2), null, true);
                return;
            }

            AppleVasTerminal appleTerminal = new AppleVasTerminal();
            foreach (AppleVasConfig merchConfig in appleConfig.Merchants)
            {
                appleTerminal.AddConfig(merchConfig);
            }

            if (appleTerminal.DoTransaction(cardChannel, out AppleVasData data2, out AppleVasError error2, out RAPDU selectOseResponseApple))
            {
                ShowResult(null, data2.Text, true);
                return;
            }

            /* Google exhange */

            if (!GoogleVasLicense.ReadDeviceId(cardChannel))
            {
                Logger.Error("Read device ID error");
                ShowResult("Not a SpringCard device?", null, true);
                return;
            }

            if (!GoogleVasLicense.LoadCollectorId(googleConfig.CollectorId_4))
            {
                Logger.Error("Wrong Collector ID");
                ShowResult("Wrong Collector ID?", null, true);
                return;
            }

            if (!GoogleVasLicense.Allowed(out string msg1))
            {
                Logger.Error("Not allowed to execute");
                ShowResult(string.Format("Not allowed to execute ({0})", msg1), null, true);
                return;
            }


            GoogleVasTerminal googleTerminal = new GoogleVasTerminal(googleConfig);

            /* Provide selectOseResponseApple to speed up the transaction */
            if (googleTerminal.DoTransaction(cardChannel, out GoogleVasData data1, out GoogleVasError error1, selectOseResponseApple))
            {
                /* Convert JSON to SpringPass data with regex */
                Regex rx = new Regex(@"[0-9a-zA-Z]+\|[a-z0-9.-_]+@[a-z.]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                MatchCollection matches = rx.Matches(data1.GetJsonString());

                if(matches.Count >= 1)
                {
                    ShowResult("Message is OK", matches[0].Value, true); 
                }
                else
                {
                    ShowResult("read succeded but no SpringPass data found", null, true);
                }
                return;
            }

         


            ShowResult("Read message failed, reason: ", null, false);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (activeReader != null)
            {
                activeReader.StopWaitCard();
                activeReader = null;
            }
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

		private void lkSubscribe_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("https://springpass.springcard.com");
		}

		private void lkAbout_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			AboutForm.DoShowDialog(this, FormStyle.ModernRed);
		}

        private void lkSetGoogleConfiguration_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            GoogleVASConfigForm f = new GoogleVASConfigForm();
            f.ShowDialog();
            googleConfig = f.GetConfig();
            btnPlay.Enabled = (cbReaders.SelectedIndex >= 0) ? googleConfig.IsValid() : false;
        }

        private void lkSetAppleConfiguration_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            AppleVASConfigForm f = new AppleVASConfigForm();
            f.ShowDialog();
            appleConfig = f.GetConfig();
            // TODO check apple config
            //btnPlay.Enabled = (cbReaders.SelectedIndex >= 0) ? configApple.IsValid() : false;
        }

        private void lkClear_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            eSpringPassData.Text = "";
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (ShowSplash)
            {
                Logger.Trace("Showing splash form");
                SplashForm.DoShowDialog(this, FormStyle.ModernRed);
            }

            LicenseData licenseDataGoogle = GoogleVasLicense.Get();
            LicenseData licenseDataApple = AppleVasLicense.Get();
            ApplyLicense(licenseDataGoogle);
            ApplyLicense(licenseDataApple);
        }

        private void ApplyLicense(LicenseData licenseData)
        {
            if (licenseData == null)
            {
                MessageBox.Show(this, "Please click the \"License\" link to request a new license, or enter your licence data.", "No license found.", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            else
            {
                lbEvaluation.Visible = licenseData.IsEvaluation;
                if (licenseData.HasTimeLimit)
                {
                    string title = licenseData.IsEvaluation ? "Evaluation license" : "Restricted license";
                    MessageBox.Show(this, string.Format("This software will stop running after {0} minutes.", licenseData.TimeLimit), title, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void lkLicense_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            LicenseInfoForm.DoShowDialog(this, GoogleVasLicense.Get());
        }
    }
}
