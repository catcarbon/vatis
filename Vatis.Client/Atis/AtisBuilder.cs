﻿using MetarDecoder;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Vatsim.Vatis.Client.AudioForVatsim;
using Vatsim.Vatis.Client.Common;
using Vatsim.Vatis.Client.Config;
using Vatsim.Vatis.Client.Core;
using Vatsim.Vatis.Client.TextToSpeech;

namespace Vatsim.Vatis.Client.Atis
{
    public class AtisBuilder : IAtisBuilder 
    {
        private readonly INavaidDatabase mNavData;
        private readonly ITextToSpeechRequest mTextToSpeechRequest;
        private readonly IAudioManager mAudioManager;
        private Airport mAirport;

        public AtisBuilder(INavaidDatabase airportDatabase, ITextToSpeechRequest textToSpeechRequest, IAudioManager audioManager)
        {
            mNavData = airportDatabase;
            mTextToSpeechRequest = textToSpeechRequest;
            mAudioManager = audioManager;
        }

        public void GenerateAcarsText(AtisComposite composite)
        {
            if (composite == null)
            {
                throw new Exception("Composite is null");
            }

            if (composite.CurrentPreset == null)
            {
                throw new Exception("CurrentPreset is null");
            }

            if (composite.DecodedMetar == null)
            {
                throw new Exception("DecodedMetar is null");
            }

            mAirport = mNavData.GetAirport(composite.Identifier);
            if (mAirport == null)
            {
                throw new Exception($"{composite.Identifier} not found in airport database.");
            }

            DecodedMetar metar;
            string atisLetter;
            List<Variable> variables;
            ParseMetar(composite, out metar, out atisLetter, out variables);

            StringBuilder acarsText = new StringBuilder(composite.CurrentPreset.Template);

            foreach (var variable in variables)
            {
                acarsText.Replace($"[{variable.Find}]", variable.TextReplace);
                acarsText.Replace($"${variable.Find}", variable.TextReplace);

                if (variable.Aliases != null)
                {
                    foreach (var alias in variable.Aliases)
                    {
                        acarsText.Replace($"[{alias}]", variable.TextReplace);
                        acarsText.Replace($"${alias}", variable.TextReplace);
                    }
                }
            }

            if (!metar.IsInternational)
            {
                acarsText.Append($" ...ADVS YOU HAVE INFO { composite.CurrentAtisLetter }.");
            }

            var str = acarsText.ToString();
            str = Regex.Replace(str, @"\s+", " ");
            str = Regex.Replace(str, @"(?<=\*)(-?[\,0-9]+)", "$1");
            str = Regex.Replace(str, @"(?<=\#)(-?[\,0-9]+)", "$1");
            str = Regex.Replace(str, @"(?<=\+)([A-Z]{3})", "$1");
            str = Regex.Replace(str, @"(?<=\+)([A-Z]{4})", "$1");
            composite.AcarsText = str.ToUpper();
        }

        public async Task BuildAtisAsync(AtisComposite composite, CancellationToken cancellationToken)
        {
            if (composite == null)
            {
                throw new Exception("Composite is null");
            }

            if (composite.CurrentPreset == null)
            {
                throw new Exception("CurrentPreset is null");
            }

            if (composite.DecodedMetar == null)
            {
                throw new Exception("DecodedMetar is null");
            }

            mAirport = mNavData.GetAirport(composite.Identifier);
            if (mAirport == null)
            {
                throw new Exception($"{composite.Identifier} not found in airport database.");
            }

            DecodedMetar metar;
            string atisLetter;
            List<Variable> variables;
            ParseMetar(composite, out metar, out atisLetter, out variables);

            // Build voice string

            var voiceString = new StringBuilder(composite.CurrentPreset.Template);

            foreach (var variable in variables)
            {
                voiceString.Replace($"[{variable.Find}]", variable.VoiceReplace);
                voiceString.Replace($"${variable.Find}", variable.VoiceReplace);

                if (variable.Aliases != null)
                {
                    foreach (var alias in variable.Aliases)
                    {
                        voiceString.Replace($"[{alias}]", variable.VoiceReplace);
                        voiceString.Replace($"${alias}", variable.VoiceReplace);
                    }
                }
            }

            if (!metar.IsInternational)
            {
                voiceString.Append($"ADVISE ON INITIAL CONTACT, YOU HAVE INFORMATION {atisLetter}.");
            }

            // Build Text ATIS

            StringBuilder acarsText = new StringBuilder(composite.CurrentPreset.Template);

            foreach (var variable in variables)
            {
                acarsText.Replace($"[{variable.Find}]", variable.TextReplace);
                acarsText.Replace($"${variable.Find}", variable.TextReplace);

                if (variable.Aliases != null)
                {
                    foreach (var alias in variable.Aliases)
                    {
                        acarsText.Replace($"[{alias}]", variable.TextReplace);
                        acarsText.Replace($"${alias}", variable.TextReplace);
                    }
                }
            }

            if (!metar.IsInternational)
            {
                acarsText.Append($" ...ADVS YOU HAVE INFO { composite.CurrentAtisLetter }.");
            }

            var str = acarsText.ToString();
            str = Regex.Replace(str, @"\s+", " ");
            str = Regex.Replace(str, @"(?<=\*)(-?[\,0-9]+)", "$1");
            str = Regex.Replace(str, @"(?<=\#)(-?[\,0-9]+)", "$1");
            str = Regex.Replace(str, @"(?<=\+)([A-Z]{3})", "$1");
            str = Regex.Replace(str, @"(?<=\+)([A-Z]{4})", "$1");
            composite.AcarsText = str.ToUpper();

            if (composite.AtisVoice.UseTextToSpeech)
            {
                var tts = FormatForTextToSpeech(voiceString.ToString().ToUpper(), composite);
                try
                {
                    await Task.Run(async () =>
                    {
                        await Task.Delay(5000, cancellationToken);
                        var response = mTextToSpeechRequest.RequestSynthesizedText(tts, cancellationToken);
                        if (response.Result != null)
                        {
                            await mAudioManager.AddOrUpdateBot(response.Result, composite.AtisCallsign, composite.AfvFrequency, mAirport.Latitude, mAirport.Longitude);

                            PostIdsUpdate(composite, cancellationToken);
                        }
                    }, cancellationToken);
                }
                catch (OperationCanceledException) { }
            }
            else
            {
                try
                {
                    if (composite.MemoryStream != null)
                    {
                        await mAudioManager.AddOrUpdateBot(composite.MemoryStream.ToArray(), composite.AtisCallsign, composite.AfvFrequency, mAirport.Latitude, mAirport.Longitude);

                        PostIdsUpdate(composite, cancellationToken);
                    }
                }
                catch (OperationCanceledException) { }
            }
        }

        private void ParseMetar(AtisComposite composite, out DecodedMetar metar, out string atisLetter, out List<Variable> variables)
        {
            metar = composite.DecodedMetar;
            var time = DoParse(metar, new ObservationTimeMeta(composite));
            var surfaceWind = DoParse(metar, new SurfaceWindMeta(composite));
            var rvr = DoParse(metar, new RunwayVisualRangeMeta());
            var visibility = DoParse(metar, new VisibilityMeta());
            var presentWeather = DoParse(metar, new PresentWeatherMeta());
            var clouds = DoParse(metar, new CloudsMeta());
            var temp = DoParse(metar, new TemperatureMeta());
            var dew = DoParse(metar, new DewpointMeta());
            var pressure = DoParse(metar, new PressureMeta());

            atisLetter = char.Parse(composite.CurrentAtisLetter).LetterToPhonetic();
            var completeWxStringVoice = $"{surfaceWind.TextToSpeech} {visibility.TextToSpeech} {rvr.TextToSpeech} {presentWeather.TextToSpeech} {clouds.TextToSpeech} {temp.TextToSpeech} {dew.TextToSpeech} {pressure.TextToSpeech}";
            var completeWxStringAcars = $"{surfaceWind.Acars} {visibility.Acars} {rvr.Acars} {presentWeather.Acars} {clouds.Acars} {temp.Acars}/{dew.Acars} {pressure.Acars}";

            var airportConditions = "";
            if (!string.IsNullOrEmpty(composite.CurrentPreset.AirportConditions) || composite.AirportConditionDefinitions.Any(t => t.Enabled))
            {
                if (composite.AirportConditionsBeforeFreeText)
                {
                    airportConditions = string.Join(" ", new[] { string.Join(". ", composite.AirportConditionDefinitions.Where(t => t.Enabled).Select(t => t.Text)), composite.CurrentPreset.AirportConditions });
                }
                else
                {
                    airportConditions = string.Join(" ", new[] { composite.CurrentPreset.AirportConditions, string.Join(". ", composite.AirportConditionDefinitions.Where(t => t.Enabled).Select(t => t.Text)) });
                }
            }

            var notamVoice = "";
            var notamText = "";
            if (!string.IsNullOrEmpty(composite.CurrentPreset.Notams) || composite.NotamDefinitions.Any(t => t.Enabled))
            {
                notamVoice = metar.IsInternational ? "Notices to airmen. " : "Notices to air missions. ";
                if (composite.NotamsBeforeFreeText)
                {
                    notamText = "NOTAMS... " + string.Join(" ", new[] { string.Join(". ", composite.NotamDefinitions.Where(t => t.Enabled).Select(t => t.Text)), composite.CurrentPreset.Notams });
                    notamVoice += string.Join(" ", new[] { string.Join(". ", composite.NotamDefinitions.Where(t => t.Enabled).Select(t => t.Text)), composite.CurrentPreset.Notams });
                }
                else
                {
                    notamText = "NOTAMS..." + string.Join(" ", new[] { composite.CurrentPreset.Notams, string.Join(". ", composite.NotamDefinitions.Where(t => t.Enabled).Select(t => t.Text)) });
                    notamVoice += string.Join(" ", new[] { composite.CurrentPreset.Notams, string.Join(". ", composite.NotamDefinitions.Where(t => t.Enabled).Select(t => t.Text)) });
                }
            }

            var transitionLevelVoice = "";
            var transitionLevelText = "";
            if (metar.IsInternational)
            {
                transitionLevelText = "TL N/A";
                transitionLevelVoice = "Transition level not determined";
                if (composite.TransitionLevels != null)
                {
                    var myMetar = metar;
                    var tlValue = composite.TransitionLevels.FirstOrDefault(t =>
                    {
                        return myMetar.Pressure.ActualValue >= t.Low && myMetar.Pressure.ActualValue <= t.High;
                    });

                    if (tlValue != null)
                    {
                        transitionLevelText = "Transition level FL " + tlValue.Altitude;
                        transitionLevelVoice = "Transition level flight level " + tlValue.Altitude.NumberToSingular();
                    }
                }
            }

            variables = new List<Variable>
            {
                new Variable("FACILITY", mAirport.ID, mAirport.Name),
                new Variable("ATIS_LETTER", composite.CurrentAtisLetter, atisLetter,  new [] {"LETTER","ATIS_CODE","ID"}),
                new Variable("TIME", time.Acars, time.TextToSpeech, new []{"OBS_TIME","OBSTIME"}),
                new Variable("WIND", surfaceWind.Acars, surfaceWind.TextToSpeech, new[]{"SURFACE_WIND"}),
                new Variable("RVR", rvr.Acars, rvr.TextToSpeech),
                new Variable("VIS", visibility.Acars, visibility.TextToSpeech, new[]{"PREVAILING_VISIBILITY"}),
                new Variable("PRESENT_WX", presentWeather.Acars, presentWeather.TextToSpeech, new[]{"PRESENT_WEATHER"}),
                new Variable("CLOUDS", clouds.Acars, clouds.TextToSpeech),
                new Variable("TEMP", temp.Acars, temp.TextToSpeech),
                new Variable("DEW", dew.Acars, dew.TextToSpeech),
                new Variable("PRESSURE", pressure.Acars, pressure.TextToSpeech, new[]{"QNH"}),
                new Variable("WX", completeWxStringAcars, completeWxStringVoice, new[]{"FULL_WX_STRING"}),
                new Variable("ARPT_COND", airportConditions, airportConditions, new[]{"ARRDEP"}),
                new Variable("NOTAMS", notamText, notamVoice),
                new Variable("TL", transitionLevelText, transitionLevelVoice)
            };
        }

        private string FormatForTextToSpeech(string input, AtisComposite composite)
        {
            // parse zulu times
            input = Regex.Replace(input, @"([0-9])([0-9])([0-9])([0-8])Z",
                m => string.Format($"{ int.Parse(m.Groups[1].Value).NumberToSingular() } { int.Parse(m.Groups[2].Value).NumberToSingular() } { int.Parse(m.Groups[3].Value).NumberToSingular() } { int.Parse(m.Groups[4].Value).NumberToSingular() } zulu"));

            // format vhf frequencies
            input = Regex.Replace(input, @"(1\d\d\.\d\d?\d?)", m => Convert.ToDouble(m.Groups[1].Value).DecimalToWordString(composite.DecodedMetar.IsInternational));

            // read numbers as singular
            input = Regex.Replace(input, @"\#(-?[\,0-9]+)", m => int.Parse(m.Groups[1].Value.Replace(",", "")).NumberToSingular());

            // format numbers to singluar words
            input = Regex.Replace(input, @"\*(-?[\,0-9]+)", m => int.Parse(m.Groups[1].Value.Replace(",", "")).NumbersToWordsGroup());

            // letters
            input = Regex.Replace(input, @"\*([A-Z]{1,2}[0-9]{0,2})", m => string.Format("{0}", m.Value.ConvertAlphaNumericToWordGroup())).Trim();

            // parse taxiways
            input = Regex.Replace(input, @"\bTWY ([A-Z]{1,2}[0-9]{0,2})\b", m => $"TWY { m.Groups[1].Value.ConvertAlphaNumericToWordGroup() }");
            input = Regex.Replace(input, @"\bTWYS ([A-Z]{1,2}[0-9]{0,2})\b", m => $"TWYS { m.Groups[1].Value.ConvertAlphaNumericToWordGroup() }");

            // parse runways
            input = Regex.Replace(input, @"\b(RY|RWY|RWYS)?\s?([0-9]{1,2})([LRC]?)\b", m => StringUtils.RwyNumbersToWords(int.Parse(m.Groups[2].Value), m.Groups[3].Value,
                (!string.IsNullOrEmpty(m.Groups[1].Value) ? true : false),
                (!string.IsNullOrEmpty(m.Groups[1].Value) && m.Groups[1].Value == "RWYS") ? true : false));

            // user defined contractions
            foreach (var x in composite.Contractions)
            {
                input = input.SafeReplace(x.String.ToUpper(), x.Spoken.ToUpper(), true);
            }

            // default contractions
            foreach (var word in Translations)
            {
                input = input.SafeReplace(word.Key.ToUpper(), word.Value.ToUpper(), true);
            }

            // format navaids identifiers
            var navaids = Regex.Matches(input, @"(?<=\+)([A-Z]{3})");
            if (navaids.Count > 0)
            {
                foreach (Match m in navaids)
                {
                    if (m.Success)
                    {
                        try
                        {
                            var find = mNavData.GetNavaid(m.Value);
                            if (find != null)
                            {
                                input = Regex
                                    .Replace(input, $@"\b(?<=\+){m.Value}\b", x => find.Name)
                                    .Replace("+", "");
                            }
                        }
                        catch { }
                    }
                }
            }

            // format airport identifiers
            var airports = Regex.Matches(input, @"(?<=\+)([A-Z0-9]{4})");
            if (airports.Count > 0)
            {
                foreach (Match m in airports)
                {
                    if (m.Success)
                    {
                        try
                        {
                            var find = mNavData.GetAirport(m.Value);
                            if (find != null)
                            {
                                input = Regex
                                    .Replace(input, $@"\b(?<=\+){ m.Value }\b", x => find.Name)
                                    .Replace("+", "");
                            }
                        }
                        catch { }
                    }
                }
            }

            input = Regex.Replace(input, @"(?<=\*)(-?[\,0-9]+)", "$1");
            input = Regex.Replace(input, @"(?<=\#)(-?[\,0-9]+)", "$1");
            input = Regex.Replace(input, @"(?<=\+)([A-Z]{3})", "$1");
            input = Regex.Replace(input, @"(?<=\+)([A-Z]{4})", "$1");
            input = Regex.Replace(input, @"\s+", " ");
            input = Regex.Replace(input, @"\.+", ".");
            input = Regex.Replace(input, @"\s\,", ",");
            input = Regex.Replace(input, @"\&", "and");
            input = Regex.Replace(input, @"\*", "");

            return input;
        }

        private ParsedData DoParse(DecodedMetar metar, AtisMeta meta)
        {
            meta.Parse(metar);
            return new ParsedData
            {
                Acars = meta.Acars,
                TextToSpeech = $"{meta.TextToSpeech}."
            };
        }

        private async void PostIdsUpdate(AtisComposite composite, CancellationToken token)
        {
            if (Debugger.IsAttached)
                return;

            if (string.IsNullOrEmpty(composite.IDSEndpoint) || composite.CurrentPreset == null)
                return;

            try
            {
                var client = new RestClient(composite.IDSEndpoint)
                {
                    Timeout = 5000
                };
                var request = new RestRequest
                {
                    Method = Method.POST
                };
                var json = new IdsRestRequest
                {
                    Facility = composite.Identifier,
                    Preset = composite.CurrentPreset.Name,
                    AtisLetter = composite.CurrentAtisLetter,
                    AirportConditions = composite.CurrentPreset.AirportConditions,
                    Notams = composite.CurrentPreset.Notams,
                    Timestamp = DateTime.UtcNow,
                    Version = "4.0.0"
                };
                request.AddParameter("application/json", JsonConvert.SerializeObject(json), ParameterType.RequestBody);
                await client.ExecuteAsync(request, token);
            }
            catch { }
        }

        private Dictionary<string, string> Translations => new Dictionary<string, string>
        {
            {"ACFT", "AIRCRAFT"},
            {"ADVS", "ADVISE"},
            {"ADVSD", "ADVISED"},
            {"ADVZY", "ADVISORY"},
            {"ADVZYS", "ADVISORIES"},
            {"ALS", "APPROACH LIGHTING SYSTEM"},
            {"ALT", "ALTITUDE"},
            {"ALTS", "ALTITUDES"},
            {"APCH", "APPROACH"},
            {"APCHS", "APPROACHES"},
            {"APP", "APPROACH"},
            {"APPR", "APPROACH"},
            {"APPRS", "APPROACHES"},
            {"APPS", "APPROACHES"},
            {"ARPT", "AIRPORT"},
            {"ARPTS", "AIRPORTS"},
            {"ARR", "ARRIVAL"},
            {"ARRS", "ARRIVALS"},
            {"ATTN", "ATTENTION"},
            {"AUTH", "AUTHORIZED"},
            {"AVBL", "AVAILABLE"},
            {"BA", "BRAKING ACTION"},
            {"BAA", "BRAKING ACTION ADVISORIES"},
            {"BC", "BACKCOURSE"},
            {"BTWN", "BETWEEN"},
            {"CAUT", "CAUTION"},
            {"CLNC", "CLEARANCE"},
            {"CLR", "CLEAR"},
            {"CLRD", "CLEARED"},
            {"CLSD", "CLOSED"},
            {"CMSN", "COMMISSION"},
            {"CMSND", "COMMISSIONED"},
            {"CTC", "CONTACT"},
            {"CTL", "CONTROL"},
            {"CTLD", "CONTROLLED"},
            {"CD", "CLEARANCE DELIVERY"},
            {"DCMSN", "DE-COMISSIONED"},
            {"DCMSND", "DE-COMISSIONED"},
            {"DEP", "DEPARTURE"},
            {"DEPS", "DEPARTURES"},
            {"DEPTS", "DEPARTURES"},
            {"DEPTG", "DEPARTING"},
            {"DIST", "DISTANCE"},
            {"DRCTN", "DIRECTION"},
            {"DURG", "DURING"},
            {"DURN", "DURATION"},
            {"EFCT", "EFFECT"},
            {"EXPC", "EXPECT"},
            {"EFF", "EFFECTIVE"},
            {"EQPT", "EQUIPMENT"},
            {"EXPT", "EXPECT"},
            {"FLT", "FLIGHT"},
            {"FREQ", "FREQUENCY"},
            {"FT", "FEET"},
            {"GND", "GROUND"},
            {"GS", "GLIDE-SLOPE"},
            {"HDG", "HEADING"},
            {"HDGS", "HEADINGS"},
            {"HELI", "HELICOPTER"},
            {"HS", "HOLD SHORT"},
            {"HAZD WX INFO", "HAZARDOUS WEATHER INFORMATION"},
            {"INBD", "INBOUND"},
            {"INTXN", "INTERSECTION"},
            {"INST", "INSTRUMENT"},
            {"INVOF", "IN VICINITY OF"},
            {"LAHSO", "LAND AND HOLD SHORT OPERATIONS"},
            {"LDG", "LANDING"},
            {"LGT", "LIGHT"},
            {"LGTD", "LIGHTED"},
            {"LGTS", "LIGHTS"},
            {"LLWS", "LOW LEVEL WIND SHEAR"},
            {"LLZ", "LOCALIZER"},
            {"LOC", "LOCALIZER"},
            {"MOD", "MODERATE"},
            {"MULTI", "MULTIPLE"},
            {"NA", "NOT AUTHORIZED"},
            {"NOTAM", "NO-TAM"},
            {"NOTAMS", "NO-TAMS"},
            {"OPER", "OPERATE"},
            {"OPS", "OPERATIONS"},
            {"OTS", "OUT OF SERVICE"},
            {"OUBD", "OUTBOUND"},
            {"PIREP", "PILOT WEATHER REPORT"},
            {"PROC", "PROCEDURE"},
            {"PROG", "PROGRESS"},
            {"RMNG", "REMAINING"},
            {"RMVL", "REMOVAL"},
            {"RQST", "REQUEST"},
            {"RQSTD", "REQUESTED"},
            {"RWY", "RUNWAY"},
            {"RWYS", "RUNWAYS"},
            {"SIMUL", "SIMULTANEOUS"},
            {"SVC", "SERVICE"},
            {"SVCS", "SERVICES"},
            {"SVR", "SEVERE"},
            {"TFC", "TRAFFIC"},
            {"TFR", "TEMPORARY FLIGHT RESTRICTION"},
            {"TURB", "TURBULENCE"},
            {"TWY", "TAXIWAY"},
            {"TWYS", "TAXIWAYS"},
            {"US", "UNSERVICEABLE"},
            {"UNCTLD", "UNCONTROLLED"},
            {"UNUSBL", "UNUSABLE"},
            {"USBL", "USABLE"},
            {"VFY", "VERIFY"},
            {"VCTR", "VECTOR"},
            {"VCTRS", "VECTORS"},
            {"VIS", "VISUAL"},
            {"XPDR", "TRANSPONDER"},
            {"XPDRS", "TRANSPONDERS"},
            {"XPNDR", "TRANSPONDER"},
            {"XPNDRS", "TRANSPONDERS"},
            {"XTM", "EXTREME"},
            {"N", "NORTH"},
            {"NNE", "NORTH NORTHEAST"},
            {"NE", "NORTHEAST"},
            {"ENE", "EAST NORTHEAST"},
            {"E", "EAST"},
            {"ESE", "EAST SOUTHEAST"},
            {"SE", "SOUTHEAST"},
            {"SSE", "SOUTH SOUTHEAST"},
            {"S", "SOUTH"},
            {"SSW", "SOUTH SOUTHWEST"},
            {"SW", "SOUTHWEST"},
            {"WSW", "WEST SOUTHWEST"},
            {"W", "WEST"},
            {"WNW", "WEST NORTHWEST"},
            {"NW", "NORTHWEST"},
            {"NNW", "NORTH NORTHWEST"},
            {"READBACK", "REEDBACK"},
            {"READBACKS", "REEDBACKS"},
            {"ATIS", "ATE-IS"},
            {"RNAV", "AREA NAVIGATION"},
            {"INFO", "INFORMATION"},
            {"WIND", "WEND"},
            {"CALLSIGN", "CALL-SIGN"},
            {"ILS", "EYE EL ESS"},
            {"DEPG", "DEPARTING"},
            {"ARVNG", "ARRIVING"},
            {"VOR", "V-O-R"},
            {"HIWAS", "HIGH-WAZ"},
            {"FM", "FROM"},
            {"RY", "RUNWAY"},
            {"WX", "WEATHER"},
            {"FSS", "FLIGHT SERVICE STATION"},
            {"HAZD", "HAZARDOUS"},
            {"WPT", "WAYPOINT"},
            {"BTN", "BETWEEN" },
            {"TWR", "TOWER" },
            {"ADZ", "ADVISE" },
            {"MSL", "MEAN SEA LEVEL" },
            {"RLS", "RELEASE" },
            {"LNDG", "LANDING" },
            {"NUM", "NUMEROUS" },
            {"OAT", "OUTSIDE AIR TEMPERATURE" },
            {"RPTD", "REPORTED" },
            {"PAPI", "PRECISION APPROACH PATH INDICATOR" },
            {"NM", "NAUTICAL MILE" },
            {"LND", "LAND" },
            {"SERV", "SERVICE" },
            {"BIV", "BIRD ACTIVITY VICINITY" },
            {"AVLB", "AVAILABLE" },
            {"VCNTY", "VICINITY" },
            {"ACKMTS", "ACKNOWLEDGE-MENTS" },
            {"APRT", "AIRPORT" },
            {"RDBKS", "READBACKS" },
            {"SHRT", "SHORT" },
            {"EXP", "EXPECT" },
            {"GC", "GROUND CONTROL" },
            {"CLSGN", "CALL-SIGN" },
            {"VA", "VISUAL APPROACH" },
            {"INCL", "INCLUDE" },
            {"TRNSPNDR", "TRANSPONDER" },
            {"RQRD", "REQUIRED" },
            {"ACTVT", "ACTIVATE" },
            {"TYS", "TAXIWAYS" },
            {"INT", "INTERSECTION" },
            {"MI", "MILE" },
            {"UFN", "UNTIL FURTHER NOTICE" },
            {"SFC", "SURFACE" },
            {"TODA", "TAKE-OFF DISTANCE" },
            {"VC", "VICINITY" },
            {"INSTR", "INSTRUMENT" },
            {"CNTD", "CONDUCTED" },
            {"INTL", "INTERNATIONAL" },
            {"ASDEX", "AS-DEX" },
            {"ASDE-X", "AS-DEX" }
        };
    }

    [Serializable]
    internal class IdsRestRequest
    {
        public string Facility { get; set; }
        public string Preset { get; set; }
        public string AtisLetter { get; set; }
        public string AirportConditions { get; set; }
        public string Notams { get; set; }
        public DateTime Timestamp { get; set; }
        public string Version { get; set; }
    }

    internal class Variable
    {
        public string Find { get; set; }
        public string TextReplace { get; set; }
        public string VoiceReplace { get; set; }
        public string[] Aliases { get; set; }

        public Variable(string find, string textReplace, string voiceReplace, string[] aliases = null)
        {
            Find = find;
            TextReplace = textReplace;
            VoiceReplace = voiceReplace;
            Aliases = aliases;
        }
    }
}
