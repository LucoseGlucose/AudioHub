using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Media;
using Android.Support.V4.Media.Session;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioHub
{
    public class MediaBrowserCallback : MediaBrowserCompat.ConnectionCallback
    {
        public ListenFragment listenFragment;

        public MediaBrowserCallback(ListenFragment listenFragment)
        {
            this.listenFragment = listenFragment;
        }
        public override void OnConnected()
        {
            MediaControllerCompat mediaController = new MediaControllerCompat(
                MainActivity.activity, MainActivity.activity.mediaBrowser.SessionToken);

            MediaControllerCompat.SetMediaController(MainActivity.activity, mediaController);

            listenFragment.fabPlayPause.Click += (s, e) =>
            {
                MediaControllerCompat mc = MediaControllerCompat.GetMediaController(MainActivity.activity);

                int state = mc.PlaybackState.State;
                if (state == PlaybackStateCompat.StatePlaying) mc.GetTransportControls().Pause();
                else mc.GetTransportControls().Play();
            };
        }
    }
}