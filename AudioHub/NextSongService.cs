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
using Android.Content.PM;

namespace AudioHub
{
    [Service(ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
    public class NextSongService : Service
    {
        public override IBinder OnBind(Intent intent)
        {
            return null;
        }
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            SongPlayer.mediaPlayer.Completion += (s, e) => SongPlayer.PlayNextSong();

            Intent resumeIntent = new Intent(MainActivity.activity, typeof(MainActivity));
            PendingIntent pendingIntent =
                PendingIntent.GetBroadcast(MainActivity.activity, 1, resumeIntent, PendingIntentFlags.UpdateCurrent);

            Notification.MediaStyle mediaStyle = new Notification.MediaStyle();
            mediaStyle.SetMediaSession(SongPlayer.mediaSession.SessionToken);

            Notification.Builder builder = new Notification.Builder(this, "Running");
            builder.SetContentTitle("MusicService is running");
            builder.SetContentText("Running");
            builder.SetSmallIcon(Resource.Drawable.round_headphones_24);
            builder.SetContentIntent(pendingIntent);
            builder.SetStyle(mediaStyle);

            StartForeground(2, builder.Build(), ForegroundService.TypeMediaPlayback);

            return StartCommandResult.Sticky;
        }
    }
}