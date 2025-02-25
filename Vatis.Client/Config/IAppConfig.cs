﻿using System.Collections.Generic;
using Vatsim.Network;

namespace Vatsim.Vatis.Client.Config
{
    public interface IAppConfig
    {
        string AppPath { get; }
        string VatsimId { get; set; }
        string VatsimPasswordDecrypted { get; set; }
        NetworkRating NetworkRating { get; set; }
        string Name { get; set; }
        List<NetworkServerInfo> CachedServers { get; set; }
        string ServerName { get; set; }
        bool SuppressNotifications { get; set; }
        bool ConfigRequired { get; }
        WindowProperties WindowProperties { get; set; }
        WindowProperties ProfileListWindowProperties { get; set; }
        List<Profile> Profiles { get; set; }
        Profile CurrentProfile { get; set; }
        AtisComposite CurrentComposite { get; set; }
        string MicrophoneDevice { get; set; }
        string PlaybackDevice { get; set; }
        void LoadConfig(string path);
        void SaveConfig();
    }
}