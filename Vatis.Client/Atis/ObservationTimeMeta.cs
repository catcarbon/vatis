﻿using MetarDecoder;
using System;
using Vatsim.Vatis.Client.Common;
using Vatsim.Vatis.Client.Config;

namespace Vatsim.Vatis.Client.Atis
{
    public class ObservationTimeMeta : AtisMeta
    {
        private AtisComposite mComposite;

        public ObservationTimeMeta(AtisComposite composite)
        {
            mComposite = composite;
        }

        public override void Parse(DecodedMetar metar)
        {
            var minute = metar.Time.Substring(Math.Max(0, metar.Time.Length - 2));

            var isSpecial = mComposite.ObservationTime != null && mComposite.ObservationTime.Enabled && mComposite.ObservationTime.Time.ToString() != minute;

            if (metar.IsInternational)
            {
                TextToSpeech = string.Join(" ", metar.Time.NumberToSingular(), isSpecial ? "special" : "").Trim(' ');
                Acars = metar.Time;
            }
            else
            {
                TextToSpeech = string.Join(" ", string.Join(" ", metar.Time.NumberToSingular(), "zulu"), isSpecial ? "special" : "");
                Acars = $"{metar.Time}Z";
            }
        }
    }
}
