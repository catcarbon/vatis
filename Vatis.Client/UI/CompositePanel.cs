using MetarDecoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Vatsim.Vatis.Client.Args;
using Vatsim.Vatis.Client.Config;
using Vatsim.Vatis.Client.Core;
using Vatsim.Vatis.Client.Network;

namespace Vatsim.Vatis.Client.UI
{
    internal partial class CompositePanel : UserControl
    {
        private bool mAlternateColor;
        private bool mIsNewAtis;
        private System.Timers.Timer mBlinkTimer;

        private string mCurrentAtisLetter = "A";
        private AtisPreset mSelectedPreset;
        private AtisPreset mPreviousPreset;
        private int mSelectedIndex = -1;
        private ConnectionStatus mConnectionStatus;
        private DecodedMetar mDecodedMetar;
        private string mErrorMessage = "";
        private readonly System.Threading.SynchronizationContext mSyncContext;
        private readonly Connection mConnection;
        private readonly AtisComposite mComposite;
        private readonly IAppConfig mAppConfig;

        public event EventHandler<EventArgs> TransmitButtonClicked;
        public event EventHandler<EventArgs> AtisLetterChanged;
        public event EventHandler<RecordedAtisChangedEventArgs> RecordedAtisMemoryStreamChanged; 

        public bool VoiceRecordEnabled
        {
            get => btnRecord.Enabled;
            set => btnRecord.Enabled = value;
        }

        public bool VoiceRecordedAtisActive
        {
            get => btnRecord.Clicked;
            set => btnRecord.Clicked = value;
        }

        public DecodedMetar DecodedMetar
        {
            get => mDecodedMetar;
            set
            {
                mDecodedMetar = value;
                mSyncContext.Post(o => { atisLetter.Enabled = value != null; }, null);
            }
        }

        public string AtisLetter
        {
            get => mCurrentAtisLetter;
            set
            {
                mCurrentAtisLetter = value;
                mSyncContext.Post(o => { atisLetter.Text = mCurrentAtisLetter; }, null);
            }
        }

        public bool IsNewAtis
        {
            get => mIsNewAtis;
            set
            {
                mIsNewAtis = value;
                mBlinkTimer.Enabled = value;
                atisLetter.ForeColor = Color.White;
            }
        }

        public string Metar
        {
            get => rtbMetar.Text;
            set => mSyncContext.Post(o => { rtbMetar.Text = value; rtbMetar.ForeColor = Color.White; }, null);
        }

        public string Wind
        {
            get => lblWind.Text;
            set => mSyncContext.Post(o => { lblWind.Text = (value ?? "-----"); }, null);
        }

        public string Altimeter
        {
            get => lblAltimeter.Text;
            set => mSyncContext.Post(o => { lblAltimeter.Text = (value ?? "-----"); }, null);
        }

        public ConnectionStatus Status
        {
            get => mConnectionStatus;
            set
            {
                mConnectionStatus = value;
                mSyncContext.Post(o =>
                {
                    switch (value)
                    {
                        case ConnectionStatus.Connected:
                            btnTransmit.Text = "DISCONNECT";
                            break;
                        case ConnectionStatus.Disconnected:
                            btnTransmit.Text = "CONNECT";
                            break;
                        case ConnectionStatus.Connecting:
                            btnTransmit.Text = "CONNECTING...";
                            break;
                    }
                }, null);
            }
        }

        public string Error
        {
            get => mErrorMessage;
            set => mSyncContext.Post(o =>
            {
                rtbMetar.Text = value;
                rtbMetar.ForeColor = Color.FromArgb(230, 80, 0);
            }, null);
        }

        public void IncrementAtisLetter()
        {
            mSyncContext.Post(o =>
            {
                (Parent as AtisTabPage).IsNewAtis = false;
                atisLetter_MouseUp(null, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
                (Parent as AtisTabPage).IsNewAtis = true;
            }, null);
        }

        public void BindPresets(List<string> presets)
        {
            foreach (var preset in presets)
            {
                if (!ddlPresets.Items.Contains(preset))
                {
                    ddlPresets.Items.Add(preset);
                }
            }

            List<string> toRemove = new List<string>();

            foreach (var preset in ddlPresets.Items)
            {
                if (!presets.Contains(preset))
                {
                    toRemove.Add(preset.ToString());
                }
            }

            foreach (var preset in toRemove)
            {
                ddlPresets.Items.Remove(preset);
            }

            if (ddlPresets.Items.Count == 0)
            {
                btnTransmit.Enabled = false;
            }
        }

        public CompositePanel(Connection connection, AtisComposite composite, IAppConfig appConfig)
        {
            InitializeComponent();

            mConnection = connection;
            mComposite = composite;
            mAppConfig = appConfig;
            mSyncContext = System.Threading.SynchronizationContext.Current;

            mConnection.NetworkConnectedChanged += OnNetworkConnectedChanged;
            mConnection.NetworkDisconnectedChanged += OnNetworkDisconnectChanged;

            mBlinkTimer = new System.Timers.Timer();
            mBlinkTimer.Interval = 500;
            mBlinkTimer.Elapsed += (e, v) =>
            {
                atisLetter.ForeColor = mAlternateColor ? Color.White : Color.FromArgb(241, 196, 15);
                mAlternateColor = !mAlternateColor;
            };

            mComposite.CurrentAtisLetter = AtisLetter;
        }

        private void OnNetworkDisconnectChanged(object sender, EventArgs e)
        {
            mSyncContext.Post(o =>
            {
                btnTransmit.Clicked = false;
                mBlinkTimer.Enabled = false;
                atisLetter.ForeColor = Color.White;
            }, null);
        }

        private void OnNetworkConnectedChanged(object sender, EventArgs e)
        {
            mSyncContext.Post(o => { btnTransmit.Clicked = true; }, null);
        }

        private void atisLetter_MouseUp(object sender, MouseEventArgs e)
        {
            if (IsNewAtis)
            {
                if (Parent != null)
                {
                    (Parent as AtisTabPage).IsNewAtis = false;
                    return;
                }
            }

            char letter = Convert.ToChar(AtisLetter);
            switch (e.Button)
            {
                case MouseButtons.Right:
                    if (letter == 'A')
                    {
                        AtisLetter = "Z";
                    }
                    else
                    {
                        letter--;
                        AtisLetter = letter.ToString();
                    }
                    break;
                case MouseButtons.Left:
                    if (letter == 'Z')
                    {
                        AtisLetter = "A";
                    }
                    else
                    {
                        letter++;
                        AtisLetter = letter.ToString();
                    }
                    break;
            }

            mComposite.CurrentAtisLetter = AtisLetter;
            AtisLetterChanged?.Invoke(this, EventArgs.Empty);
        }

        private void btnTransmit_Click(object sender, EventArgs e)
        {
            TransmitButtonClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ddlPresets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ddlPresets.SelectedIndex == mSelectedIndex)
                return;

            if (ddlPresets.SelectedItem != null)
            {
                if (mSelectedPreset != null)
                {
                    if (mSelectedPreset.IsNotamsDirty || mSelectedPreset.IsAirportConditionsDirty)
                    {
                        if (MessageBox.Show(this, "There are unsaved Airport Conditions or NOTAMs. Do you want to save these changes first before switching presets?", "Save Changes", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            ddlPresets.SelectedIndex = mSelectedIndex;
                            return;
                        }
                        else
                        {
                            saveNotams.Visible = false;
                            saveAirportConditions.Visible = false;
                            mSelectedPreset.IsNotamsDirty = false;
                            mSelectedPreset.IsAirportConditionsDirty = false;
                        }
                    }
                }

                if (mComposite != null)
                {
                    var preset = mComposite.Presets.FirstOrDefault(x => x.Name == ddlPresets.SelectedItem.ToString());
                    if (preset != null)
                    {
                        mSelectedPreset = preset;
                        mComposite.CurrentPreset = preset;

                        txtArptCond.ReadOnly = false;
                        txtNotams.ReadOnly = false;

                        txtArptCond.Text = mSelectedPreset.AirportConditions;
                        txtNotams.Text = mSelectedPreset.Notams;

                        btnTransmit.Enabled = true;
                    }
                }

                mSelectedIndex = ddlPresets.SelectedIndex;
                mPreviousPreset = mSelectedPreset;
            }
        }

        private void txtArptCond_TextChanged(object sender, EventArgs e)
        {
            if (mSelectedPreset != null)
            {
                if (txtArptCond.Text == mSelectedPreset.AirportConditions)
                {
                    saveAirportConditions.Visible = false;
                    mSelectedPreset.IsAirportConditionsDirty = false;
                }
                else
                {
                    if (mSelectedPreset == mPreviousPreset && mSelectedPreset.AirportConditions != txtArptCond.Text)
                    {
                        saveAirportConditions.Visible = true;
                        mSelectedPreset.IsAirportConditionsDirty = true;
                    }
                }
            }
        }

        private void txtNotams_TextChanged(object sender, EventArgs e)
        {
            if (mSelectedPreset != null)
            {
                if (txtNotams.Text == mSelectedPreset.Notams)
                {
                    saveNotams.Visible = false;
                    mSelectedPreset.IsNotamsDirty = false;
                }
                else
                {
                    if (mSelectedPreset == mPreviousPreset && mSelectedPreset.Notams != txtNotams.Text)
                    {
                        saveNotams.Visible = true;
                        mSelectedPreset.IsNotamsDirty = true;
                    }
                }
            }
        }

        private void saveAirportConditions_Click(object sender, EventArgs e)
        {
            if (mSelectedPreset != null)
            {
                mSelectedPreset.IsAirportConditionsDirty = false;
                mSelectedPreset.AirportConditions = txtArptCond.Text;
                mAppConfig.SaveConfig();
                saveAirportConditions.Visible = false;
            }
        }

        private void saveNotams_Click(object sender, EventArgs e)
        {
            if (mSelectedPreset != null)
            {
                mSelectedPreset.IsNotamsDirty = false;
                mSelectedPreset.Notams = txtNotams.Text;
                mAppConfig.SaveConfig();
                saveNotams.Visible = false;
            }
        }

        private void ToUppercase(object sender, KeyPressEventArgs e)
        {
            e.KeyChar = char.ToUpper(e.KeyChar);
        }

        private void ClearFormatting(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                ((RichTextBox)sender).Paste(DataFormats.GetFormat("Text"));
                e.Handled = true;
            }
        }

        private void btnRecord_Click(object sender, EventArgs e)
        {
            using (var dlg = new RecordAtisDialog(mAppConfig, mComposite))
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (dlg.AtisMemoryStream != null)
                    {
                        RecordedAtisMemoryStreamChanged?.Invoke(this, new RecordedAtisChangedEventArgs(dlg.AtisMemoryStream));
                    }
                }
            }
        }

        private void btnAirportConditions_Click(object sender, EventArgs e)
        {
            using (var dlg = new AirportConditionsDialog(mComposite))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    mAppConfig.SaveConfig();
                }
            }
        }

        private void btnNotams_Click(object sender, EventArgs e)
        {
            using (var dlg = new NotamDefinitionsDialog(mComposite))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    mAppConfig.SaveConfig();
                }
            }
        }
    }
}