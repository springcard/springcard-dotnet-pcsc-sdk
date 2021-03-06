using System;
using System.Collections;
using System.Windows.Forms;
using System.Threading;
using SpringCard.PCSC;
using SpringCard.LibCs;
using SpringCard.LibCs.Windows;
using SpringCard.NfcForum.Ndef;
using SpringCard.NfcForum.Tags;
using SpringCard.LibCs.Windows.Forms;
using SpringCard.PCSC.Forms;

using SpringCardApplication.Controls;

namespace SpringCardApplication
{
	public partial class MainForm : Form
	{		
		public ArrayList ProcessActions = new ArrayList();

        bool ShowSplash = false;
		
		SCardReader reader = null;
		SCardChannel cardchannel = null;
		NfcTag tag = null;
		Thread cardthread;
		
		RtdControl control = null;
		
		TypeSelectButton VCardButton;
		TypeSelectButton SmartPosterButton;
		TypeSelectButton UriButton;
		TypeSelectButton TextButton;
		TypeSelectButton MediaButton;
        TypeSelectButton ExternalTypeButton;
        //TypeSelectButton WifiHandoverButton;

        bool ready = false;
		
		public MainForm()
		{
			InitializeComponent();
			
			Settings s = new Settings();
			if (s.ShowConsole)
			{
				Logger.ConsoleLevel = Logger.Level.All;
				SystemConsole.Show();
			}
			Text = AppUtils.ApplicationTitle(true);
			
			miShowConsole.Checked = s.ShowConsole;
			miEnableLock.Checked = s.EnableLock;
			cbLock.Visible = s.EnableLock;

            /* WIFI HANDOVER */
            /* ------------- */

            /*WifiHandoverButton = new TypeSelectButton("Wifi Handover");
			WifiHandoverButton.Dock = DockStyle.Top;
			pLeft.Controls.Add(WifiHandoverButton);
			WifiHandoverButton.OnSelected = new System.EventHandler(OnWifiHandoverSelected);*/

            /* EXTERNAL TYPE */
            /* -------------- */

            ExternalTypeButton = new TypeSelectButton("External type");
            ExternalTypeButton.Dock = DockStyle.Top;
            pLeft.Controls.Add(ExternalTypeButton);
            ExternalTypeButton.OnSelected = new System.EventHandler(OnExternalTypeSelected);

            /* VCARD */
            /* ----- */

            VCardButton = new TypeSelectButton("vCard");
			VCardButton.Dock = DockStyle.Top;
			pLeft.Controls.Add(VCardButton);
			VCardButton.OnSelected = new System.EventHandler(OnVCardSelected);

			/* MEDIA */
			/* ----- */
			
			MediaButton = new TypeSelectButton("MIME Media");
			MediaButton.Dock = DockStyle.Top;
			pLeft.Controls.Add(MediaButton);
			MediaButton.OnSelected = new System.EventHandler(OnMediaSelected);

			/* TEXT */
			/* ---- */
			
			TextButton = new TypeSelectButton("Text");
			TextButton.Dock = DockStyle.Top;
			pLeft.Controls.Add(TextButton);
			TextButton.OnSelected = new System.EventHandler(OnTextSelected);

			/* URI */
			/* --- */
			
			UriButton = new TypeSelectButton("URI");
			UriButton.Dock = DockStyle.Top;
			pLeft.Controls.Add(UriButton);
			UriButton.OnSelected = new System.EventHandler(OnUriSelected);

			/* SMARTPOSTER */
			/* ----------- */

			SmartPosterButton = new TypeSelectButton("SmartPoster");
			SmartPosterButton.Dock = DockStyle.Top;
			pLeft.Controls.Add(SmartPosterButton);
			SmartPosterButton.OnSelected = new System.EventHandler(OnSmartPosterSelected);


            /* Default is URI */
            SelectUri();
			
			Logger.Trace("Starting up");
			
			ready = true;
		}

		
		private void menuItemQuit_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		private void setEditable(bool yes)
		{
			btnFormatDesfire.Visible = false;
			btnFormatDesfire.Enabled = false;
			btnWrite.Visible = true;
			btnWrite.Enabled = yes;
			cbLock.Enabled = yes;
			if (control != null)
				control.SetEditable(yes);
		}
		

		public void setReader(string readerName)
		{
			Logger.Trace("Selecting reader: " + readerName);
			
			eReaderName.Text = readerName;
			eReaderStatus.Text = "";
			eCardAtr.Text = "";
			
			reader = new SCardReader(readerName);
			reader.StartMonitor(new SCardReader.StatusChangeCallback(ReaderStatusChanged));
		}

		
		delegate void ReaderStatusChangedInvoker(uint ReaderState, CardBuffer CardAtr);
		void ReaderStatusChanged(uint ReaderState, CardBuffer CardAtr)
		{
			/* The ReaderStatusChanged function is called as a delegate (callback) by the SCardReader object    */
			/* within its backgroung thread. Therefore we must use the BeginInvoke syntax to switch back from   */
			/* the context of the background thread to the context of the application's main thread. Overwise   */
			/* we'll get a security violation when trying to access the window's visual components (that belong */
			/* to the application's main thread and can't be safely manipulated by background threads).         */
			if (InvokeRequired)
			{
				this.BeginInvoke(new ReaderStatusChangedInvoker(ReaderStatusChanged), ReaderState, CardAtr);
				return;
			}
			
			eReaderStatus.Text = SCARD.ReaderStatusToString(ReaderState);
			
			if (CardAtr != null)
			{
				eCardAtr.Text = CardAtr.AsString(" ");
			} else
			{
				eCardAtr.Text = "";
			}
			
			if (ReaderState == SCARD.STATE_UNAWARE)
			{
				if (cardchannel != null)
				{
					cardchannel.Disconnect();
					cardchannel = null;
				}
				
				MessageBox.Show(this,
								"The reader we were working on has disappeared from the system. This application will terminate now.",
				                "The reader has been removed",
				                MessageBoxButtons.OK,
				                MessageBoxIcon.Exclamation);
				
				Application.Exit();
				
			} else if ((ReaderState & SCARD.STATE_EMPTY) != 0)
			{
				Logger.Trace("Reader: EMPTY");
				
				if (cardchannel != null)
				{
					cardchannel.Disconnect();
					cardchannel = null;
				}

				/* Hide the toasts that are related to this card */
				ToastForm.Clear();

				/* No card -> leave edit mode */
				setEditable(false);
				
			} else if ((ReaderState & SCARD.STATE_UNAVAILABLE) != 0)
			{
				Logger.Trace("Reader: UNAVAILABLE");
			} else if ((ReaderState & SCARD.STATE_MUTE) != 0)
			{
				Logger.Trace("Reader: MUTE");
			} else if ((ReaderState & SCARD.STATE_INUSE) != 0)
			{
				Logger.Trace("Reader: INUSE");
			} else if ((ReaderState & SCARD.STATE_PRESENT) != 0)
			{
				Logger.Trace("Reader: PRESENT");
				
				if (cardchannel == null)
				{
					/* New card -> leave edit mode */
					setEditable(false);
					
					cardchannel = new SCardChannel(reader);
					
					if (cardchannel.Connect())
					{
						Logger.Trace("Connected to the card");
						cardthread = new Thread(TagReadProc);
						cardthread.Start();
						
					} else
					{
						Logger.Trace("Connection to the card failed");

						ToastForm.Display(this,
							"NfcTagTool failed to connect to the card.\nCheck that you don't have another application running in background that tries to work with the smartcards in the same time.",
							"Connection failed",
							MessageBoxIcon.Information,
							3);

						cardchannel = null;
					}
				}
				
				
				//card_status = 1;
			}
		}
		
		delegate void OnErrorInvoker(string text, string caption);
		void OnError(string text, string caption)
		{
			Logger.Trace("Error: " + text);

			ToastForm.Display(this,
				text,
				caption,
				MessageBoxIcon.Error,
				6);
		}
		
		delegate void OnTagWriteInvoker(NfcTag _tag);
		void OnTagWrite(NfcTag _tag)
		{
			ToastForm.Display(this,
				"Writing tag OK",
				"Operation succesful",
				MessageBoxIcon.Information,
				3);

			Logger.Trace("Write OK");
		}
		
		delegate void OnTagReadInvoker(NfcTag _tag);
		void OnTagRead(NfcTag _tag)
		{
			tag = _tag;
			
			Logger.Trace("Read terminated");
			
			if (tag == null)
			{
				MessageBox.Show(this, "Internal error, tag is null!");
				return;
			}
			
			if ((tag.Content == null) || (tag.Content.Count == 0))
			{
				if (!tag.IsLocked())
				{
					ToastForm.Display(this,
						"The Tag has no valid content yet. You may create your own content and write it onto the tag",
						"This NFC Tag is empty",
						MessageBoxIcon.Information,
						3);
					setEditable(true);
				} else
				{
					ToastForm.Display(this,
						"The Tag has no valid content, but is not writable",
						"This NFC Tag is empty",
						MessageBoxIcon.Warning,
						3);
				}
			} else
			{
				Unselect();
				
				for (int i=0; i<tag.Content.Count; i++)
				{
					/* Display the first record we support in the tag's content */
					NdefObject ndef = tag.Content[i];
					
					if (ndef is RtdSmartPoster)
					{
						SelectSmartPoster();
					}
                    else if (ndef is RtdUri)
					{
						SelectUri();
					}
                    else if (ndef is RtdText)
					{
						SelectText();
					}
                    else if (ndef is RtdVCard)
					{
						SelectVCard();
					}
                    else if (ndef is RtdMedia)
					{
						SelectMedia();
					}
                    else if (ndef is RtdExternalType)
                    {
                        SelectExternalType();
                    }
                    else if (ndef is RtdHandoverSelector)
					{
						SelectWifiHandover();
					}
					
					if (control != null)
					{
						control.SetContent(ndef);
						break;
					}
					
				}
				
				if (!tag.IsLocked())
				{
					/* It will be possible to rewrite the tag */
					setEditable(true);
				}
				
				if (control == null)
				{
					/* No supported record has been found */
					Unselect();

					ToastForm.Display(this,
						"This Tag contains a valid content, but NfcTagTool doesn't know how to display it.",
						"The content of this NFC Tag is not supported",
						MessageBoxIcon.Warning,
						3);
				}
			}
			
		}

		delegate void SetTagTypeDelegate(string tagType);
		void SetTagType(string tagType)
		{
			lbTagType.Text = tagType;
		}

        /*
		 * TagReadProc
		 * -----------
		 * This is the core function to try read a card, and maybe recognize it as an NFC tag
		 * The function is executed in a background thread, so the application's window keeps
		 * responding during the dialog
		 */
        private void TagReadProc()
		{
			Logger.Trace("Is the card a NFC Forum Tag ???");

			/*
			 * 1st step, is the card a NFC Forum Tag ?
			 * ---------------------------------------
			 */

			NfcTag.RecognizeOptions options = new NfcTag.RecognizeOptions();
			options.RecognizeDesfireEv1 = true;
			options.Read = true;

			if (!NfcTag.Recognize(cardchannel, options, out NfcTag nfcTag, out NfcTag.RecognizeResult result))
				Logger.Trace("Unrecognized or unsupported tag");

			/*
				* 2nd step, tell the application we've got something, and die
				* -----------------------------------------------------------
				*/

			if (nfcTag != null)
			{
				this.BeginInvoke(new SetTagTypeDelegate(SetTagType), string.Format("NFC Forum type {0} Tag", nfcTag.Type));
				this.BeginInvoke(new OnTagReadInvoker(OnTagRead), nfcTag);
			}
			else
			{
				if (result.IsDesfireEv1)
				{
					this.BeginInvoke(new SetTagTypeDelegate(SetTagType), "Desfire EV1");
					this.BeginInvoke(new EnableFormatButtonInvoker(EnableFormatButton));
					this.BeginInvoke(new OnErrorInvoker(OnError),
						"This card is a Desfire EV1 (or EV2/EV3).\n" +
						"It may be formatted to become a NFC Forum type 4 Tag, but it is not currently formatted as though.\n" +
						"Click the 'Format' button to format the card.",
						"Desfire card - Not NFC Forum formatted"
					);
				}
				else
				{
					switch (result.ErrorReason)
                    {
						case NfcTag.RecognizeResult.ErrorReasonE.UnsupportedATR:
							this.BeginInvoke(new OnErrorInvoker(OnError),
								"This card is not supported.\n" +
								"Please retry with a valid NFC Forum Tag.",
								"Not an NFC Forum Tag"
							);
							break;

						case NfcTag.RecognizeResult.ErrorReasonE.InvalidContent:
							this.BeginInvoke(new OnErrorInvoker(OnError),
								string.Format("This card could be used as a NFC Forum type {0} Tag, but its content is not compliant with NFC Forum requirements.\n", result.Type) +
								"You need to erase its content (if it is not locked) before using it as a Tag.",
								"Tag's content is not NFC Forum compliant"
							);
							break;

						default:
							this.BeginInvoke(new OnErrorInvoker(OnError),
								"Failed to recognize a NFC Forum Tag.",
								"An error has occured"
							);
							break;
					}
				}
			}
		}

        /*
		 * TagWriteProc
		 * ------------
		 * This is the core function to try read a card, and maybe recognize it as an NFC tag
		 * The function is executed in a background thread, so the application's window keeps
		 * responding during the dialog
		 */
        private void TagWriteProc(object _tag)
		{
			if (_tag == null)
				return; /* Oups */
			
			if (!(_tag is NfcTag))
				return; /* Oups */
			
			NfcTag tag = _tag as NfcTag;

			if (!tag.IsFormatted() && !tag.Format())
			{
				this.BeginInvoke(new OnErrorInvoker(OnError), "This application has been unable to format the Tag", "Failed to encode the NFC Tag");
				return;
			}

			if (!tag.Write())
			{
				this.BeginInvoke(new OnErrorInvoker(OnError), "This application has been unable to write onto the Tag", "Failed to encode the NFC Tag");
				return;
			}
			
			if (cbLock.Visible && cbLock.Checked)
			{
				/* Try to lock the Tag */
				if (tag.IsLockable())
				{
					if (!tag.Lock())
					{
						this.BeginInvoke(new OnErrorInvoker(OnError), "This application has been unable to lock the Tag in read-only state", "Failed to encode the NFC Tag");
						return;
					}
				} else
				{
					/* This Tag is not locackable */
					this.BeginInvoke(new OnErrorInvoker(OnError), "The Tag has been successfully written, but there's no method to put it in read-only state", "This NFC Tag can't be locked");
					return;
				}
			}

			/* Done */
			this.BeginInvoke(new OnTagWriteInvoker(OnTagWrite), tag);
		}
		
		delegate void EnableFormatButtonInvoker();
		void EnableFormatButton()
		{
			btnFormatDesfire.Visible = true;
			btnFormatDesfire.Enabled = true;
			btnWrite.Visible = false;
			btnWrite.Enabled = false;
		}
		
		void BtnWriteClick(object sender, EventArgs e)
		{
			if (tag == null)
				return; /* Oups */
			
			if (control == null)
				return; /* Oups */
						
			if (!control.ValidateUserContent())
			{
				/* The user did not provide a valid content */
				return;
			}
			
			NdefObject ndef = control.GetContent();
			
			if (ndef == null)
			{
				MessageBox.Show(this, "Failed to create the NDEF message to be written onto the Tag");
				return;
			}
			
			tag.Content.Clear();
			tag.Content.Add(ndef);
			
			long content_size = tag.ContentSize();
			long tag_capacity = tag.Capacity();
			
			if (content_size > tag_capacity)
			{
				MessageBox.Show(this, "The capacity of the Tag is " + tag_capacity + "B, but the content you're trying to write makes " + content_size + "B", "This Tag is too small");
				return;
			}
			
			if (!tag.IsEmpty())
			{
				/* Ask for confirmation before overwriting */
				if (MessageBox.Show(this, "The Tag already contains data. Do you really want to overwrite its content?", "Confirm overwrite", MessageBoxButtons.YesNo) != DialogResult.Yes)
				{
					return;
				}
			}

			if (!tag.IsFormatted())
			{
				/* Ask for confirmation before formatting */
				if (MessageBox.Show(this, "The Tag is not yet formatted. Do you really want to format it?", "Confirm formatting", MessageBoxButtons.YesNo) != DialogResult.Yes)
				{
					return;
				}
			}

			if (cbLock.Visible && cbLock.Checked)
			{
				if (!tag.IsLockable())
				{
					/* The Tag can't be locked */
				} else
				{
					/* Ask for confirmation before locking */
					if (MessageBox.Show(this, "Locking the Tag in read-only state is permanent and can't be cancelled. Are you really sure you want to do this?", "Confirm locking", MessageBoxButtons.YesNo) != DialogResult.Yes)
					{
						return;
					}
				}
			}
			
			cardthread = new Thread(new ParameterizedThreadStart(TagWriteProc));
			cardthread.Start(tag);

		}

        public void OnExternalTypeSelected(object sender, EventArgs e)
        {
            SelectExternalType();
        }

        public void OnSmartPosterSelected(object sender, EventArgs e)
		{
			SelectSmartPoster();
		}
		
		public void OnUriSelected(object sender, EventArgs e)
		{
			SelectUri();
		}

		public void OnTextSelected(object sender, EventArgs e)
		{
			SelectText();
		}

		public void OnVCardSelected(object sender, EventArgs e)
		{
			SelectVCard();
		}

		public void OnMediaSelected(object sender, EventArgs e)
		{
			SelectMedia();
		}
		
		public void  OnWifiHandoverSelected(object sender, EventArgs e)
		{
			SelectWifiHandover();
		}
		
		void Panel1Paint(object sender, PaintEventArgs e)
		{
			
		}
		
		void Unselect()
		{
			if (control != null)
			{
				pMain.Controls.Remove(control);
				control.Dispose();
				control = null;
			}

			SmartPosterButton.SetSelected(false);
			UriButton.SetSelected(false);
			TextButton.SetSelected(false);
			VCardButton.SetSelected(false);
			MediaButton.SetSelected(false);
            ExternalTypeButton.SetSelected(false);
			//WifiHandoverButton.SetSelected(false);
		}
		
		void SelectUri()
		{
			Unselect();
			UriButton.SetSelected(true);
			control = new RtdUriControl();
			control.Dock = DockStyle.Fill;
			pMain.Controls.Add(control);
		}

		void SelectText()
		{
			Unselect();
			TextButton.SetSelected(true);
			control = new RtdTextControl();
			control.Dock = DockStyle.Fill;
			pMain.Controls.Add(control);
		}

		void SelectSmartPoster()
		{
			Unselect();
			SmartPosterButton.SetSelected(true);
			control = new RtdSmartPosterControl();
			control.Dock = DockStyle.Fill;
			pMain.Controls.Add(control);
		}
		
		void SelectVCard()
		{
			Unselect();
			VCardButton.SetSelected(true);
			control = new RtdVCardControl();
			control.Dock = DockStyle.Fill;
			pMain.Controls.Add(control);
		}
		
		void SelectMedia()
		{
			Unselect();
			MediaButton.SetSelected(true);
			control = new RtdMediaControl();
			control.Dock = DockStyle.Fill;
			pMain.Controls.Add(control);
		}

        void SelectExternalType()
        {
            Unselect();
            ExternalTypeButton.SetSelected(true);
            control = new RtdExternalTypeControl();
            control.Dock = DockStyle.Fill;
            pMain.Controls.Add(control);
        }

        void SelectWifiHandover()
		{
			Unselect();
			//WifiHandoverButton.SetSelected(true);
			control = new RtdWifiHandoverControl();
			control.Dock = DockStyle.Fill;
			pMain.Controls.Add(control);
			
		}

		void AboutToolStripMenuItem1Click(object sender, EventArgs e)
		{
            AboutForm.DoShowDialog(this, FormStyle.ModernRed);
		}
		
		void QuitToolStripMenuItemClick(object sender, EventArgs e)
		{
			Close();
		}
		
		void MainFormLoad(object sender, EventArgs e)
		{
		}
		
		void MainFormShown(object sender, EventArgs e)
		{
            if (ShowSplash)
            {
                Logger.Trace("Showing splash form");
                SplashForm.DoShowDialog(this, FormStyle.ModernMarroon);
            }

            ReaderSelectForm readerSelect = new ReaderSelectForm(this.imgHeader.BackColor);
			
			if (readerSelect.SelectedReader == null)
			{
				readerSelect.Preselect("SpringCard|ontactless");
				for (;;)
				{
					readerSelect.ShowDialog();
					if (readerSelect.SelectedReader != null)
						break;
					
					if (MessageBox.Show("This application can't run without a reader. Do you want to leave the application now ?",
					                    "No reader selected",
					                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
					{
						Application.Exit();
						return;
					}
				}
			}
			
			setReader(readerSelect.SelectedReader);
		}
		
		void MainFormClosed(object sender, FormClosedEventArgs e)
		{
			if (cardchannel != null)
			{
				cardchannel.Disconnect();
				cardchannel = null;
			}

			if (reader != null)
			{
				reader.StopMonitor();
				reader = null;
			}
		}
		
		void ImgHeaderClick(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("https://www.springcard.com");
		}
		
		void MiReaderClick(object sender, EventArgs e)
		{
			ReaderSelectForm readerSelect = new ReaderSelectForm(this.imgHeader.BackColor);
			readerSelect.SelectedReader = eReaderName.Text;
			readerSelect.ShowDialog();

			if (readerSelect.SelectedReader != null)
				setReader(readerSelect.SelectedReader);
		}
		
		void MiEnableLockCheckedChanged(object sender, EventArgs e)
		{
			if (!ready)
				return;

			Settings s = new Settings();
			s.EnableLock = miEnableLock.Checked;
			s.Save();
			cbLock.Visible = miEnableLock.Checked;
		}
		
		void MiShowConsoleCheckedChanged(object sender, EventArgs e)
		{
			if (!ready)
				return;
			
			Settings s = new Settings();
			s.ShowConsole = miShowConsole.Checked;
			s.Save();
		}
		
		void BtnFormatDesfireClick(object sender, EventArgs e)
		{
			DesfireFormatForm f = new DesfireFormatForm(cardchannel);
			if (f.ShowDialog(this) == DialogResult.OK)
			{
				Logger.Trace("Formating ok");
				cardthread = new Thread(TagReadProc);
				cardthread.Start();
			} else
			{
				Logger.Trace("Formating ko");
			}
		}
		void PictureBox1Click(object sender, EventArgs e)
		{
	
		}

        private void MainForm_Resize(object sender, EventArgs e)
        {
			ToastForm.Relocate(this);
		}

        private void MainForm_Move(object sender, EventArgs e)
        {
			ToastForm.Relocate(this);
        }
    }

}