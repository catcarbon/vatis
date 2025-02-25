﻿using System.Drawing;
using System.Windows.Forms;
using Vatsim.Vatis.Client.Config;
using Vatsim.Vatis.Client.Network;

namespace Vatsim.Vatis.Client.UI
{
    internal class AtisTabPage : TabPage
    {
        private bool mAlternateColor;
        private bool mIsNewAtis;
        private System.Timers.Timer mBlinkTimer;

        public Connection Connection { get; private set; }
        public AtisComposite Composite { get; private set; }
        public CompositePanel CompositeMeta { get; private set; }
        public bool IsNewAtis
        {
            get => mIsNewAtis;
            set
            {
                mIsNewAtis = value;
                mBlinkTimer.Enabled = value;
                CompositeMeta.IsNewAtis = value;
                ForeColor = Color.Aqua;
                Parent?.Invalidate();
            }
        }

        public AtisTabPage(Connection connection, AtisComposite composite, IAppConfig appConfig)
        {
            Connection = connection;
            Composite = composite;

            CompositeMeta = new CompositePanel(Connection, Composite, appConfig)
            {
                Dock = DockStyle.Fill
            };
            CompositeMeta.AtisLetterChanged += (sender, args) =>
            {
                Parent?.Invalidate();
            };
            Controls.Add(CompositeMeta);
            BackColor = Color.FromArgb(50, 50, 50);
            ForeColor = Color.Aqua; // atis letter color

            mBlinkTimer = new System.Timers.Timer();
            mBlinkTimer.Interval = 500;
            mBlinkTimer.Elapsed += (e, v) =>
            {
                ForeColor = mAlternateColor ? Color.Aqua : Color.FromArgb(241, 196, 15);
                mAlternateColor = !mAlternateColor;
                Parent?.Invalidate();
            };
        }
    }
}
