﻿using GeoVR.Connection;
using GeoVR.Shared;
using System.Threading.Tasks;
using Vatsim.Vatis.Client.Common;
using Vatsim.Vatis.Client.Config;

namespace Vatsim.Vatis.Client.AudioForVatsim
{
    public class AudioManager : IAudioManager
    {
        private readonly IAppConfig mAppConfig;
        private readonly ApiServerConnection mApiServerConnection;
        private string VoiceServerUrl = "https://voice1.vatsim.uk";

        public AudioManager(IAppConfig appConfig)
        {
            mAppConfig = appConfig;
            mApiServerConnection = new ApiServerConnection(VoiceServerUrl);
        }

        public async Task AddOrUpdateBot(byte[] audio, string callsign, uint frequency, double lat, double lon)
        {
            if (!mApiServerConnection.Authenticated)
            {
                await mApiServerConnection.Connect(mAppConfig.VatsimId, 
                    mAppConfig.VatsimPasswordDecrypted, "vATIS");
            }

            await mApiServerConnection.RemoveBot(callsign).AwaitTimeout(5000);

            PutBotRequestDto dto = AtisBotHelper.AddBotRequest(audio, frequency, lat, lon, 100.0);

            await mApiServerConnection.AddOrUpdateBot(callsign, dto).AwaitTimeout(5000);
        }

        public async Task RemoveBot(string callsign)
        {
            await mApiServerConnection.RemoveBot(callsign).AwaitTimeout(5000);
        }
    }
}