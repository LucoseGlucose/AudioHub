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

            Notification.Builder builder = new Notification.Builder(this, "Running")
                .SetContentTitle("MusicService is running")
                .SetSmallIcon(Resource.Drawable.round_headphones_24);

            StartForeground(2, builder.Build(), ForegroundService.TypeMediaPlayback);

            return StartCommandResult.Sticky;
        }
    }
}