using Android.App;
using Android.Content;
using Android.Media.Session;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Media.Session;
using Android.Views;
using Android.Widget;
using AndroidX.Core.Content;
using AndroidX.Media.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using YoutubeReExplode.Channels;
using Android.Support.V4.Media;
using Android.Graphics.Drawables;
using Android.Graphics;
using AndroidX.Core.App;
using Android.Content.PM;

namespace AudioHub
{
    public class MediaSessionCallback : MediaSessionCompat.Callback
    {
        public MusicService service;

        public MediaSessionCallback(MusicService service)
        {
            this.service = service;
        }
        public override void OnPlay()
        {
            MediaControllerCompat controller = service.mediaSession.Controller;
            MediaMetadataCompat metadata = controller.Metadata;
            MediaDescriptionCompat description = metadata.Description;

            NotificationCompat.Builder builder = new NotificationCompat.Builder(service, string.Empty);
            Notification notif = builder.SetContentTitle(description.Title).SetContentText(description.Subtitle).SetSubText(description.Description)
                .SetContentIntent(controller.SessionActivity)
                .SetDeleteIntent(MediaButtonReceiver.BuildMediaButtonPendingIntent(service, PlaybackStateCompat.ActionStop))
                .SetVisibility((int)NotificationVisibility.Public).SetStyle(new AndroidX.Media.App.NotificationCompat.MediaStyle()
                .SetMediaSession(service.mediaSession.SessionToken)).Build();

            MainActivity.activity.VolumeControlStream = Android.Media.Stream.Music;
            service.StartForeground(1, notif, ForegroundService.TypeMediaPlayback);
        }
    }
}