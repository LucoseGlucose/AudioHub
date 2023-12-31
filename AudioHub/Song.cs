﻿using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioHub
{
    public struct Song
    {
        public ulong id;
        public string title;
        public string artist;
        public float durationSecs;
        public string thumbnailPath;

        public Song(ulong id, string title, string artist, float durationSecs, string thumbnailPath)
        {
            this.id = id;
            this.title = title;
            this.artist = artist;
            this.durationSecs = durationSecs;
            this.thumbnailPath = thumbnailPath;
        }

        public readonly string GetDurationString()
        {
            return $"{Math.Floor((decimal)(durationSecs / 60))}:{((durationSecs % 60) < 10 ? "0" : "")}{durationSecs % 60}";
        }
    }
}