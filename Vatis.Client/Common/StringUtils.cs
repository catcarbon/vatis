﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Vatsim.Vatis.Client.Common
{
    public static class StringUtils
    {
        public static string PrintUnit(this MetarDecoder.Value.Unit unit, double value)
        {
            switch (unit)
            {
                case MetarDecoder.Value.Unit.KT:
                    return value > 1 ? "knots" : "knot";
                case MetarDecoder.Value.Unit.MeterPerSecond:
                    return value > 1 ? "meters per second" : "meter per second";
                case MetarDecoder.Value.Unit.KilometerPerHour:
                    return value > 1 ? "kilometers per hour" : "kilometer per hour";
            }

            return unit.ToString();
        }

        public static string PrintUnitShort(this MetarDecoder.Value.Unit unit)
        {
            switch (unit)
            {
                case MetarDecoder.Value.Unit.KilometerPerHour:
                    return "KPH";
                case MetarDecoder.Value.Unit.M:
                    return "M";
                case MetarDecoder.Value.Unit.MeterPerSecond:
                    return "MPS";
                case MetarDecoder.Value.Unit.KT:
                    return "KT";
                case MetarDecoder.Value.Unit.Feet:
                    return "FT";
                case MetarDecoder.Value.Unit.StatuteMile:
                    return "SM";
                default:
                    return unit.ToString();
            }
        }

        /// <summary>
        /// Strips away multiple spaces.
        /// </summary>
        /// <param name="input">String that needs extra spaces stripped.</param>
        /// <returns></returns>
        public static string RemoveExtraSpaces(this string input)
        {
            Regex trim = new Regex(@"\s+");
            return trim.Replace(input, " ");
        }

        /// <summary>
        /// Converts alphanumeric strings to human readable format. Useful for translating
        /// taxiways.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ConvertAlphaNumericToWordGroup(this string s)
        {
            var alpha = new string(s.Where(x => char.IsLetter(x)).ToArray());
            var num = new string(s.Where(x => char.IsNumber(x)).ToArray());
            int numOut;

            if (num.Length > 0)
            {
                int.TryParse(num, out numOut);
                return $"{string.Join(" ", alpha.Select(x => Alphabet[x]).ToArray())} {numOut.NumbersToWordsGroup()}";
            }
            else
            {
                return string.Join(" ", alpha.Select(x => Alphabet[x]).ToArray());
            }
        }

        /// <summary>
        /// Translates single letter to phonetic alphabet variant.
        /// For example, the character C would translate to Charlie.
        /// </summary>
        /// <param name="x">Single alpha character A-Z</param>
        /// <returns></returns>
        public static string LetterToPhonetic(this char x)
        {
            if (Alphabet.ContainsKey(x))
            {
                return Alphabet[x];
            }
            return "";
        }

        /// <summary>
        /// Translates runway numbers to human readable format for the voice synthesizer.
        /// For example, RWY 25R would translate to "Runway Two Five Right"
        /// </summary>
        /// <param name="number">The runway number 1-360</param>
        /// <param name="identifier">The runway position identifier (L, R, C, null)</param>
        /// <param name="prefix"></param>
        /// <param name="plural"></param>
        /// <returns></returns>
        public static string RwyNumbersToWords(int number, string identifier, bool prefix = false, bool plural = false)
        {
            string words = "";
            string result = "";

            if (number >= 1 && number <= 36)
            {
                var unitsMap = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "niner", "one zero", "one one", "one two", "one three", "one four", "one five", "one six", "one seven", "one eight", "one niner", "two zero", "two one", "two two", "two three", "two four", "two five", "two six", "two seven", "two eight", "two niner", "three zero", "three one", "three two", "three three", "three four", "three five", "three six" };

                words = unitsMap[number];
            }

            string ident;
            switch (identifier)
            {
                case "L":
                    ident = "left";
                    break;
                case "R":
                    ident = "right";
                    break;
                case "C":
                    ident = "center";
                    break;
                default:
                    ident = "";
                    break;
            }

            if (prefix && plural)
                result = string.Format(" runways {0} {1}", words, ident);
            if (prefix && !plural)
                result = string.Format(" runway {0} {1}", words, ident);
            else if (!prefix && !plural)
                result = string.Format(" {0} {1}", words, ident);

            return result.ToUpper();
        }

        /// <summary>
        /// This method safely finds and replaces a string.
        /// </summary>
        /// <param name="input">The string to search</param>
        /// <param name="find">The string to find</param>
        /// <param name="replace">The string to replace the find with</param>
        /// <param name="matchWholeWord">Should match whole world?</param>
        /// <returns></returns>
        public static string SafeReplace(this string input, string find, string replace, bool matchWholeWord)
        {
            string textToFind = matchWholeWord ? string.Format(@"\b{0}\b", find) : find;
            return Regex.Replace(input, textToFind, replace);
        }

        /// <summary>
        /// Normalizes frequency to 25Khz format for AFV
        /// </summary>
        /// <param name="freq"></param>
        /// <returns></returns>
        public static uint Normalize25KhzFrequency(this string freq)
        {
            uint freqint = Convert.ToUInt32(Convert.ToDouble(freq) * 1000000);
            freqint /= 1000;

            if (((freqint % 100) == 20) || ((freqint % 100) == 70))
            {
                freqint += 5;
            }

            return freqint * 1000;
        }

        /// <summary>
        /// Phoentic Alphabet
        /// </summary>
        private static Dictionary<char, string> Alphabet => new Dictionary<char, string>
        {
            { 'A', "Alpha" },
            { 'B', "Bravo" },
            { 'C', "Charlie" },
            { 'D', "Delta" },
            { 'E', "Echo" },
            { 'F', "Foxtrot" },
            { 'G', "Golf" },
            { 'H', "Hotel" },
            { 'I', "India" },
            { 'J', "Juliet" },
            { 'K', "Kilo" },
            { 'L', "Lima" },
            { 'M', "Mike" },
            { 'N', "November" },
            { 'O', "Oscar" },
            { 'P', "Papa" },
            { 'Q', "Quebec" },
            { 'R', "Romeo" },
            { 'S', "Sierra" },
            { 'T', "Tango" },
            { 'U', "Uniform" },
            { 'V', "Victor" },
            { 'W', "Whiskey" },
            { 'X', "X-Ray" },
            { 'Y', "Yankee" },
            { 'Z', "Zulu" }
        };
    }
}
