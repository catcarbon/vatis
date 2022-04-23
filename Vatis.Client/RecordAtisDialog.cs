using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Vatsim.Vatis.Client.Common;
using Vatsim.Vatis.Client.Config;
using Vatsim.Vatis.Client.Core;

namespace Vatsim.Vatis.Client
{
    [IgnoreFormRegistration]
    public partial class RecordAtisDialog : Form
    {
        private readonly System.Threading.SynchronizationContext mSyncContext;
        private Stopwatch mRecordingStopwatch;
        private System.Timers.Timer mElapsedTimeUpdateTimer;
        private System.Timers.Timer mMaxDurationTimer;
        private MemoryStream mAtisStream;
        private WaveInEvent mAtisAudioIn;
        private WaveOutEvent mAtisAudioOut;
        private bool mIsRecording = false;
        private bool mPlaybackStarted = false;
        private readonly IAppConfig mAppConfig;

        public MemoryStream AtisMemoryStream => mAtisStream;
        public bool HasAtisStream => mAtisStream.Length > 0;

        public RecordAtisDialog(IAppConfig appConfig, AtisComposite mComposite)
        {
            Console.WriteLine(mComposite.AcarsText);
            InitializeComponent();

            mAppConfig = appConfig;

            mSyncContext = System.Threading.SynchronizationContext.Current;

            mAtisStream = new MemoryStream(20000000);

            var inputDevices = ClientAudioUtilities.GetInputDevices().ToArray();
            ddlInputDeviceName.DataSource = inputDevices;

            var outputDevices = ClientAudioUtilities.GetOutputDevices().ToArray();
            ddlOutputDeviceName.DataSource = outputDevices;

            if (!string.IsNullOrEmpty(mAppConfig.MicrophoneDevice))
                ddlInputDeviceName.SelectedIndex = ddlInputDeviceName.FindStringExact(mAppConfig.MicrophoneDevice);

            if (!string.IsNullOrEmpty(mAppConfig.PlaybackDevice))
                ddlOutputDeviceName.SelectedIndex = ddlOutputDeviceName.FindStringExact(mAppConfig.PlaybackDevice);

            richTextBoxCompiledAtis.Lines = new string[] { mComposite.AcarsText };
            
            SetInputDevice();
            SetOutputDevice();

            mRecordingStopwatch = new Stopwatch();
            mElapsedTimeUpdateTimer = new System.Timers.Timer();
            mElapsedTimeUpdateTimer.Interval = 500;
            mElapsedTimeUpdateTimer.Elapsed += ElapsedTimeUpdateTimer_Elapsed;

            mMaxDurationTimer = new System.Timers.Timer();
            mMaxDurationTimer.Interval = 180000; // 3 minutes
            mMaxDurationTimer.AutoReset = false;
            mMaxDurationTimer.Elapsed += delegate
            {
                if (mAtisAudioIn == null)
                    return;

                mMaxDurationTimer.Stop();
                mAtisAudioIn.StopRecording();

                if (mRecordingStopwatch.IsRunning)
                    mRecordingStopwatch.Stop();

                mSyncContext.Post(o =>
                {
                    if (MessageBox.Show(this, "Maximum ATIS duration reached (3 minutes). Recording stopped.", "Record ATIS", MessageBoxButtons.OK) == DialogResult.OK)
                    {
                        btnRecord.Enabled = true;
                        btnStop.Enabled = false;
                        btnListen.Enabled = true;
                    }
                }, null);
            };
        }

        private void ElapsedTimeUpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            mSyncContext.Post(o =>
            {
                recordingLength.Text = mRecordingStopwatch.Elapsed.ToString("hh\\:mm\\:ss");
            }, null);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (mIsRecording)
            {
                if (MessageBox.Show(this, "You are currently recording an ATIS. Are you sure you want to cancel the operation?", "Cancel ATIS Recording", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    mAtisAudioIn.StopRecording();
                    mAtisStream.SetLength(0);
                    Close();
                }
                else
                {
                    return;
                }
            }
            else
            {
                if (HasAtisStream)
                {
                    if (MessageBox.Show(this, "Are you sure you want to discard your recorded ATIS?", "Discard ATIS?", MessageBoxButtons.YesNo) == DialogResult.No)
                    {
                        return;
                    }
                }

                Close();
            }
        }

        private void ddlInputDeviceName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!ddlInputDeviceName.Focused)
                return;

            SetInputDevice();
        }

        private void ddlOutputDeviceName_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!ddlOutputDeviceName.Focused)
                return;

            SetOutputDevice();
        }

        private void SetInputDevice()
        {
            if (mAtisAudioIn != null)
            {
                mAtisAudioIn.Dispose();
                mAtisAudioIn = null;
            }
            mAtisAudioIn = new WaveInEvent();
            mAtisAudioIn.DeviceNumber = ClientAudioUtilities.MapInputDevice(ddlInputDeviceName.Text);
            mAtisAudioIn.WaveFormat = new WaveFormat(48000, 1);
            mAtisAudioIn.DataAvailable += (s, args) =>
            {
                mAtisStream.Write(args.Buffer, 0, args.BytesRecorded);
                if (mAtisStream.Position > mAtisAudioIn.WaveFormat.AverageBytesPerSecond * 180)
                {
                    mAtisAudioIn.StopRecording();
                }
            };
            mAtisAudioIn.RecordingStopped += delegate
            {
                btnStop.Enabled = false;
                btnRecord.Enabled = true;
                btnListen.Enabled = HasAtisStream && ddlOutputDeviceName.SelectedIndex != -1;
                btnSave.Enabled = HasAtisStream && mRecordingStopwatch.ElapsedMilliseconds >= 5000;
                lblMinRecordingLength.Visible = mRecordingStopwatch.ElapsedMilliseconds < 5000;
                mMaxDurationTimer.Stop();
                mAtisStream.Position = 0;
                mIsRecording = false;

                mRecordingStopwatch.Reset();
                mElapsedTimeUpdateTimer.Stop();
            };

            btnRecord.Enabled = ddlInputDeviceName.SelectedIndex != -1;

            if (ddlOutputDeviceName.SelectedValue != null && ddlInputDeviceName.SelectedValue.ToString() != mAppConfig.MicrophoneDevice)
            {
                mAppConfig.MicrophoneDevice = ddlInputDeviceName.SelectedValue.ToString();
                mAppConfig.SaveConfig();
            }
        }

        private void SetOutputDevice()
        {
            if (mAtisAudioOut != null)
            {
                mAtisAudioOut.Dispose();
                mAtisAudioOut = null;
            }
            mAtisAudioOut = new WaveOutEvent();
            mAtisAudioOut.DeviceNumber = ClientAudioUtilities.MapOutputDevice(ddlOutputDeviceName.Text);
            mAtisAudioOut.PlaybackStopped += delegate
            {
                btnListen.Text = "Listen";
                btnRecord.Enabled = true;
                mPlaybackStarted = false;
            };

            if (ddlOutputDeviceName.SelectedValue != null && ddlOutputDeviceName.SelectedValue.ToString() != mAppConfig.PlaybackDevice)
            {
                mAppConfig.PlaybackDevice = ddlOutputDeviceName.SelectedValue.ToString();
                mAppConfig.SaveConfig();
            }
        }

        private void btnRecord_Click(object sender, EventArgs e)
        {
            if (mAtisAudioIn == null)
                return;

            mAtisStream.SetLength(0);
            mAtisAudioIn.StartRecording();
            mMaxDurationTimer.Start();

            recordingLength.Text = "00:00:00";
            recordingLength.Visible = true;
            mElapsedTimeUpdateTimer.Start();
            mRecordingStopwatch.Start();

            mIsRecording = true;
            btnRecord.Enabled = false;
            btnStop.Enabled = true;
            btnListen.Enabled = false;
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            mAtisAudioIn.StopRecording();
        }

        private void btnListen_Click(object sender, EventArgs e)
        {
            if (mAtisAudioOut == null)
                return;

            if (!mPlaybackStarted)
            {
                mAtisStream.Position = 0;
                mPlaybackStarted = true;
                btnListen.Text = "Stop Playback";
                btnRecord.Enabled = false;

                IWaveProvider provider = new RawSourceWaveStream(mAtisStream, new WaveFormat(48000, 1));
                mAtisAudioOut.Init(provider);
                mAtisAudioOut.Play();
            }
            else
            {
                mAtisAudioOut.Stop();
                mPlaybackStarted = false;
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void RecordAtisDialog_Load(object sender, EventArgs e)
        {

        }
    }
}