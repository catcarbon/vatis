﻿using Appccelerate.EventBroker;
using Appccelerate.EventBroker.Handlers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vatsim.Vatis.Client.Args;
using Vatsim.Vatis.Client.Atis;
using Vatsim.Vatis.Client.AudioForVatsim;
using Vatsim.Vatis.Client.Common;
using Vatsim.Vatis.Client.Config;
using Vatsim.Vatis.Client.Core;
using Vatsim.Vatis.Client.Network;
using Vatsim.Vatis.Client.UI;

namespace Vatsim.Vatis.Client
{
    internal partial class MainForm : Form
    {
        [EventPublication(EventTopics.SessionStarted)]
        public event EventHandler<EventArgs> RaiseSessionStarted;

        [EventPublication(EventTopics.SessionEnded)]
        public event EventHandler<EventArgs> RaiseSessionEnded;

        [EventPublication(EventTopics.PerformVersionCheck)]
        public event EventHandler<EventArgs> RaisePerformVersionCheck;

        private readonly IEventBroker mEventBroker;
        private readonly IUserInterface mUserInterface;
        private readonly IAppConfig mAppConfig;
        private readonly IAudioManager mAudioManager;
        private readonly IAtisBuilder mAtisBuilder;
        private readonly INavaidDatabase mAirportDatabase;
        private readonly SynchronizationContext mSyncContext;
        private readonly List<Connection> mConnections = new List<Connection>();
        private readonly System.Windows.Forms.Timer mUtcClock;
        private bool mInitializing = true;
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        public MainForm(IEventBroker eventBroker, IUserInterface userInterface, IAppConfig appConfig, IAtisBuilder atisBuilder, INavaidDatabase airportDatabase, IAudioManager audioManager)
        {
            InitializeComponent();

            mUserInterface = userInterface;
            mAppConfig = appConfig;
            mAtisBuilder = atisBuilder;
            mAirportDatabase = airportDatabase;
            mAudioManager = audioManager;
            mEventBroker = eventBroker;
            mSyncContext = SynchronizationContext.Current;

            utcClock.Text = DateTime.UtcNow.ToString("HH:mm/ss");
            mUtcClock = new System.Windows.Forms.Timer
            {
                Interval = 500
            };
            mUtcClock.Tick += (s, e) =>
            {
                utcClock.Text = DateTime.UtcNow.ToString("HH:mm/ss");
            };
            mUtcClock.Start();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams handleParam = base.CreateParams;
                handleParam.ExStyle |= 0x02000000;
                return handleParam;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);
            Rectangle rect = new Rectangle(base.ClientRectangle.Left, base.ClientRectangle.Top, base.ClientRectangle.Width - 1, base.ClientRectangle.Height - 1);
            using (Pen pen = new Pen(Color.FromArgb(0, 0, 0)))
            {
                pevent.Graphics.DrawRectangle(pen, rect);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            mEventBroker.Register(this);

            DownloadServerList();

            RaiseSessionStarted?.Invoke(this, EventArgs.Empty);
            if (mAppConfig.WindowProperties == null)
            {
                mAppConfig.WindowProperties = new WindowProperties();
                mAppConfig.WindowProperties.Location = ScreenUtils.CenterOnScreen(this);
                mAppConfig.SaveConfig();
            }
            ScreenUtils.ApplyWindowProperties(mAppConfig.WindowProperties, this);
            mInitializing = false;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (mAppConfig.ConfigRequired)
            {
                DialogResult dialogResult = MessageBox.Show(this, "It looks like this may be the first time you've run vATIS on this computer. Some configuration items are required before you can connect to the network. Would you like to configure vATIS now?", "Configuration Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                {
                    using (var dlg = mUserInterface.CreateSettingsForm())
                    {
                        dlg.TopMost = mAppConfig.WindowProperties.TopMost;
                        dlg.ShowDialog(this);
                    }
                }
            }

            RefreshAtisComposites();

            if (atisTabs.TabPages.Count > 0)
            {
                mAppConfig.CurrentComposite = (atisTabs.TabPages[0].Tag as AtisComposite);
            }

            RaisePerformVersionCheck?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);

            if (!mInitializing)
            {
                ScreenUtils.SaveWindowProperties(mAppConfig.WindowProperties, this);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            RaiseSessionEnded?.Invoke(this, EventArgs.Empty);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            if (mAppConfig.Profiles.Any(x => x.Composites.Any(y => y.Presets.Any(z => z.IsNotamsDirty || z.IsAirportConditionsDirty))))
            {
                if (MessageBox.Show(this, "There are unsaved Airport Conditions or NOTAMs. Are you sure you want to exit anyways?", "Confirm Close", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    return;
                }
            }

            if (mConnections.Any(x => x.IsConnected))
            {
                if (MessageBox.Show(this, "You still have active ATIS connections. Are you sure you want to exit?", "Confirm Close", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    return;
                }
            }

            foreach (var connection in mConnections)
            {
                if (connection.IsConnected)
                {
                    mAudioManager.RemoveBot(connection.Callsign);
                    connection.Disconnect();
                }
            }

            Close();
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
        }

        [EventSubscription(EventTopics.AppConfigUpdated, typeof(OnPublisher))]
        public void OnAppConfigUpdated(object sender, EventArgs e)
        {
            ScreenUtils.ApplyWindowProperties(mAppConfig.WindowProperties, this);
        }

        private void btnManageProfile_Click(object sender, EventArgs e)
        {
            using (var dlg = mUserInterface.CreateProfileConfigurationForm())
            {
                dlg.TopMost = mAppConfig.WindowProperties.TopMost;
                dlg.ShowDialog(this);
            }
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            using (var form = mUserInterface.CreateSettingsForm())
            {
                form.TopMost = mAppConfig.WindowProperties.TopMost;
                form.ShowDialog();
            }
        }

        [EventSubscription(EventTopics.RefreshAtisComposites, typeof(OnUserInterfaceAsync))]
        public void OnRefreshAtisComposites(object sender, EventArgs e)
        {
            RefreshAtisComposites();
        }

        [EventSubscription(EventTopics.AtisCompositeDeleted, typeof(OnUserInterfaceAsync))]
        public void OnAtisCompositeDeleted(object sender, AtisCompositeDeletedEventArgs e)
        {
            if (atisTabs.TabPages.ContainsKey(e.Identifier))
            {
                var connection = mConnections.FirstOrDefault(x => x.AirportIcao == e.Identifier);
                if (connection != null)
                {
                    connection.Disconnect();
                    mConnections.Remove(connection);
                }

                atisTabs.TabPages.RemoveByKey(e.Identifier);
                atisTabs.Invalidate();
            }
        }

        private void RefreshAtisComposites()
        {
            if (mAppConfig.CurrentProfile == null)
            {
                return;
            }

            foreach (var composite in mAppConfig.CurrentProfile.Composites.OrderBy(x => x.Identifier).Take(Constants.MAX_COMPOSITES))
            {
                var tab = atisTabs.TabPages[composite.Identifier] as AtisTabPage;
                if (tab != null)
                {
                    tab.Connection.Frequency = composite.AtisFrequency;
                    tab.CompositeMeta.BindPresets(composite.Presets.Select(x => x.Name).ToList());
                }
                else
                {
                    var connection = new Connection(mAppConfig, mAirportDatabase)
                    {
                        Frequency = composite.AtisFrequency,
                        AirportIcao = composite.Identifier,
                        Composite = composite
                    };

                    composite.AtisCallsign = connection.Callsign;

                    var cancellationToken = new CancellationTokenSource();

                    var tabPage = new AtisTabPage(connection, composite, mAppConfig)
                    {
                        Name = composite.Identifier,
                        Text = composite.Identifier,
                        Tag = composite
                    };
                    tabPage.CompositeMeta.ConnectButtonClicked += (sender, args) =>
                    {
                        if (connection.IsConnected)
                        {
                            // If there's a previous request, cancel it.
                            if (cancellationToken != null)
                                cancellationToken.Cancel();

                            cancellationToken = new CancellationTokenSource();

                            connection.Disconnect();

                            tabPage.CompositeMeta.DecodedMetar = null;
                            tabPage.CompositeMeta.Metar = null;
                            tabPage.CompositeMeta.Wind = null;
                            tabPage.CompositeMeta.Altimeter = null;
                            tabPage.CompositeMeta.Status = ConnectionStatus.Disconnected;
                            tabPage.Parent?.Invalidate();
                        }
                        else
                        {
                            if (mConnections.Count(x => x.IsConnected) >= Constants.MAX_CONNECTIONS)
                            {
                                tabPage.CompositeMeta.Error = "Maximum ATIS connections exceeded.";
                                return;
                            }
                            connection.Connect();
                        }
                    };
                    tabPage.Connection.MetarResponseReceived += (sender, args) =>
                    {
                        var metar = MetarDecoder.MetarDecoder.ParseWithMode(args.Metar);

                        tabPage.CompositeMeta.Error = null;
                        tabPage.CompositeMeta.Metar = args.Metar;
                        tabPage.CompositeMeta.DecodedMetar = metar;
                        if (metar.SurfaceWind != null)
                            tabPage.CompositeMeta.Wind = metar.SurfaceWind.ToString();
                        if (metar.Pressure != null)
                            tabPage.CompositeMeta.Altimeter = metar.Pressure.ToString();
                        tabPage.CompositeMeta.Status = ConnectionStatus.Connected;
                        composite.DecodedMetar = metar;

                        tabPage.Parent?.Invalidate();

                        if (composite.AtisVoice.UseTextToSpeech)
                        {
                            try
                            {
                                // If there's a previous request, cancel it.
                                if (cancellationToken != null)
                                    cancellationToken.Cancel();

                                cancellationToken = new CancellationTokenSource();

                                mAtisBuilder.BuildAtisAsync(composite, cancellationToken.Token).ContinueWith(t =>
                                {
                                    tabPage.Connection.SendSubscriberNotification();
                                }, cancellationToken.Token);
                            }
                            catch (AggregateException ex)
                            {
                                tabPage.CompositeMeta.Error = "Error: " + string.Join(", ", ex.Flatten().InnerExceptions.Select(t => t.Message));
                                connection.Disconnect();
                            }
                        }
                        else
                        {
                            mAtisBuilder.GenerateAcarsText(composite);
                            mSyncContext.Post(o =>
                            {
                                tabPage.CompositeMeta.VoiceRecordEnabled = !composite.AtisVoice.UseTextToSpeech;
                            }, null);
                        }

                        if (args.IsUpdated)
                        {
                            tabPage.CompositeMeta.IncrementAtisLetter();
                            tabPage.Connection.SendSubscriberNotification();

                            if (!mAppConfig.SuppressNotifications)
                            {
                                var sound = new SoundPlayer(Properties.Resources.NewUpdate);
                                sound.Play();
                            }

                            mSyncContext.Post(o =>
                            {
                                FlashTaskbar.Flash(this);
                            }, null);
                        }
                    };
                    tabPage.CompositeMeta.PresetChanged += (sender, args) =>
                    {
                        if (composite.DecodedMetar == null)
                            return;

                        if (composite.AtisVoice.UseTextToSpeech)
                        {
                            if (!connection.IsConnected)
                                return;

                            try
                            {
                                // If there's a previous request, cancel it.
                                if (cancellationToken != null)
                                    cancellationToken.Cancel();

                                cancellationToken = new CancellationTokenSource();

                                mAtisBuilder.BuildAtisAsync(composite, cancellationToken.Token);
                            }
                            catch (AggregateException ex)
                            {
                                tabPage.CompositeMeta.Error = "Error: " + string.Join(", ", ex.Flatten().InnerExceptions.Select(t => t.Message));
                                connection.Disconnect();
                            }
                        }
                        else
                        {
                            mAtisBuilder.GenerateAcarsText(composite);
                        }
                    };
                    tabPage.CompositeMeta.AtisLetterChanged += (sender, args) =>
                    {
                        if (connection.IsConnected && composite.DecodedMetar != null && composite.AtisVoice.UseTextToSpeech)
                        {
                            try
                            {
                                // If there's a previous request, cancel it.
                                if (cancellationToken != null)
                                    cancellationToken.Cancel();

                                cancellationToken = new CancellationTokenSource();

                                mAtisBuilder.BuildAtisAsync(composite, cancellationToken.Token).ContinueWith(t =>
                                {
                                    tabPage.Connection.SendSubscriberNotification();
                                }, cancellationToken.Token);
                            }
                            catch (AggregateException ex)
                            {
                                tabPage.CompositeMeta.Error = "Error: " + string.Join(", ", ex.Flatten().InnerExceptions.Select(t => t.Message));
                                connection.Disconnect();
                            }
                        }
                    };
                    tabPage.CompositeMeta.RecordedAtisMemoryStreamChanged += (sender, args) =>
                    {
                        if (!connection.IsConnected)
                            return;

                        try
                        {
                            // If there's a previous request, cancel it.
                            if (cancellationToken != null)
                                cancellationToken.Cancel();

                            cancellationToken = new CancellationTokenSource();

                            composite.MemoryStream = args.AtisMemoryStream;

                            mAtisBuilder.BuildAtisAsync(composite, cancellationToken.Token).ContinueWith(t =>
                            {
                                mSyncContext.Post(o =>
                                {
                                    tabPage.CompositeMeta.VoiceRecordedAtisActive = true;
                                    tabPage.Connection.SendSubscriberNotification();
                                }, null);
                            }, cancellationToken.Token);
                        }
                        catch (AggregateException ex)
                        {
                            tabPage.CompositeMeta.Error = "Error: " + string.Join(", ", ex.Flatten().InnerExceptions.Select(t => t.Message));
                            connection.Disconnect();
                        }
                    };
                    connection.NetworkErrorReceived += (sender, args) =>
                    {
                        tabPage.CompositeMeta.Error = "Network Error: " + args.Error;
                    };
                    connection.KillRequestReceived += (sender, args) =>
                    {
                        tabPage.CompositeMeta.DecodedMetar = null;
                        tabPage.CompositeMeta.Error = !string.IsNullOrEmpty(args.Reason) ? $"Forcfully disconnected from network: {args.Reason}" : "Forcfully disconnected from network.";
                        tabPage.CompositeMeta.Wind = null;
                        tabPage.CompositeMeta.Altimeter = null;

                        tabPage.CompositeMeta.Status = ConnectionStatus.Disconnected;
                        tabPage.Parent?.Invalidate();

                        mAudioManager.RemoveBot(connection.Callsign);
                    };
                    connection.NetworkDisconnectedChanged += (sender, args) =>
                    {
                        tabPage.CompositeMeta.DecodedMetar = null;
                        if (!string.IsNullOrEmpty(tabPage.CompositeMeta.Error))
                            tabPage.CompositeMeta.Metar = null;
                        tabPage.CompositeMeta.Wind = null;
                        tabPage.CompositeMeta.Altimeter = null;

                        mSyncContext.Post(o =>
                        {
                            tabPage.CompositeMeta.VoiceRecordEnabled = false;
                            tabPage.CompositeMeta.VoiceRecordedAtisActive = false;
                        }, null);

                        tabPage.CompositeMeta.Status = ConnectionStatus.Disconnected;
                        tabPage.Parent?.Invalidate();

                        mAudioManager.RemoveBot(connection.Callsign);
                    };
                    connection.NetworkConnectedChanged += (sender, args) =>
                    {
                        mAudioManager.RemoveBot(connection.Callsign);

                        tabPage.CompositeMeta.Error = null;
                        tabPage.CompositeMeta.Status = ConnectionStatus.Connecting;
                        if (composite.AtisVoice != null)
                        {
                            mSyncContext.Post(o =>
                            {
                                tabPage.CompositeMeta.VoiceRecordedAtisActive = false;
                            }, null);
                        }
                    };

                    tabPage.CompositeMeta.BindPresets(composite.Presets.Select(x => x.Name).ToList());

                    mAppConfig.CurrentComposite = composite;
                    atisTabs.TabPages.Add(tabPage);
                    mConnections.Add(connection);
                }
            }
        }

        private void atisTabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (atisTabs.SelectedTab != null)
            {
                mAppConfig.CurrentComposite = (atisTabs.SelectedTab.Tag as AtisComposite);
            }
        }

        private void DownloadServerList()
        {
            Task.Run(() =>
            {
                try
                {
                    var servers = Vatsim.Network.NetworkInfo.GetServerList("https://status.vatsim.net");
                    if (servers.Count > 0)
                    {
                        mAppConfig.CachedServers.Clear();
                        mAppConfig.CachedServers.AddRange(servers);
                        mAppConfig.SaveConfig();
                    }
                }
                catch { }
            });
        }
    }
}