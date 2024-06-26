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
using Android.Content.PM;
using AndroidX.Core.App;

namespace AudioHub
{
    [Service(ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
    public class NextSongService : Service
    {
        public static bool initialized = false;

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            if (!initialized)
            {
                SongPlayer.mediaPlayer.Completion += (s, e) => SongPlayer.PlayNextSong(false);
                initialized = true;
            }

            Intent previousIntent = new Intent(this, typeof(MediaButtonReciever))
                .SetAction(Intent.ActionMediaButton).PutExtra(Intent.ExtraKeyEvent, "Previous");
            PendingIntent previousPIntent = PendingIntent.GetBroadcast(this, 2, previousIntent, PendingIntentFlags.UpdateCurrent);

            Intent resumeIntent = new Intent(this, typeof(MediaButtonReciever))
                .SetAction(Intent.ActionMediaButton).PutExtra(Intent.ExtraKeyEvent, "Resume");
            PendingIntent resumePIntent = PendingIntent.GetBroadcast(this, 3, resumeIntent, PendingIntentFlags.UpdateCurrent);

            Intent pauseIntent = new Intent(this, typeof(MediaButtonReciever))
                .SetAction(Intent.ActionMediaButton).PutExtra(Intent.ExtraKeyEvent, "Pause");
            PendingIntent pausePIntent = PendingIntent.GetBroadcast(this, 4, pauseIntent, PendingIntentFlags.UpdateCurrent);

            Intent nextIntent = new Intent(this, typeof(MediaButtonReciever))
                .SetAction(Intent.ActionMediaButton).PutExtra(Intent.ExtraKeyEvent, "Next");
            PendingIntent nextPIntent = PendingIntent.GetBroadcast(this, 5, nextIntent, PendingIntentFlags.UpdateCurrent);

            Intent reopenIntent = new Intent(this, typeof(MainActivity));
            PendingIntent reopenPIntent = PendingIntent.GetActivity(this, 6, reopenIntent, PendingIntentFlags.UpdateCurrent);

            AndroidX.Media.App.NotificationCompat.MediaStyle mediaStyle = new AndroidX.Media.App.NotificationCompat.MediaStyle();
            mediaStyle.SetMediaSession(SongPlayer.mediaSession.SessionToken);

            NotificationCompat.Builder builder = new NotificationCompat.Builder(this, "Controls");
            builder.SetSmallIcon(Resource.Drawable.round_headphones_24);
            builder.SetVisibility((int)NotificationVisibility.Public);

            builder.AddAction(new NotificationCompat.Action(Resource.Drawable.round_skip_previous_24, "Previous", previousPIntent));

            if (SongPlayer.mediaPlayer.IsPlaying)
            {
                builder.AddAction(new NotificationCompat.Action(Resource.Drawable.round_pause_24, "Pause", pausePIntent));
            }
            else
            {
                builder.AddAction(new NotificationCompat.Action(Resource.Drawable.round_play_arrow_24, "Resume", resumePIntent));
            }

            builder.AddAction(new NotificationCompat.Action(Resource.Drawable.round_skip_next_24, "next", nextPIntent));

            builder.SetContentIntent(reopenPIntent);
            builder.SetStyle(mediaStyle);
            mediaStyle.SetShowActionsInCompactView(0, 1, 2);

            builder.Extras.PutInt("poop", 10);
            StartForeground(2, builder.Build(), ForegroundService.TypeMediaPlayback);

            return StartCommandResult.Sticky;
        }
    }
}