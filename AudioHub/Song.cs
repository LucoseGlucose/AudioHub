using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YoutubeReExplode.Videos;

namespace AudioHub
{
    public struct Song
    {
        public string id;
        public string title;
        public string artist;
        public string fullTitle;
        public string fullArtist;
        public int durationSecs;

        public Song(string id, string fullTitle, string fullArtist, int durationSecs)
        {
            this.id = id;
            this.fullTitle = fullTitle;
            this.fullArtist = fullArtist;
            this.durationSecs = durationSecs;

            title = GetSimpleTitle(fullTitle);
            artist = GetSimpleArtist(fullTitle, fullArtist);
        }
        public readonly string GetDurationString()
        {
            return $"{Math.Floor((decimal)(durationSecs / 60))}:{((durationSecs % 60) < 10 ? "0" : "")}{durationSecs % 60}";
        }
        public static string GetSimpleTitle(string fullTitle)
        {
            while (RemoveBetweenChars(fullTitle, '(', ')') != fullTitle)
            {
                fullTitle = RemoveBetweenChars(fullTitle, '(', ')');
            }
            while (RemoveBetweenChars(fullTitle, '[', ']') != fullTitle)
            {
                fullTitle = RemoveBetweenChars(fullTitle, '[', ']');
            }

            return fullTitle.Split(" - ").Last().Trim();
        }
        public static string RemoveBetweenChars(string str, char start, char end)
        {
            if (!str.Contains(start) || !str.Contains(end)) return str;

            int startI = str.IndexOf(start);
            int endI = str.IndexOf(end);

            return str.Remove(startI, endI - startI + 1);
        }
        public static string GetSimpleArtist(string fullTitle, string fullArtist)
        {
            if (fullTitle.Contains(" - ")) return fullTitle.Split(" - ", StringSplitOptions.RemoveEmptyEntries).First().Trim();
            return fullArtist;
        }
    }
}