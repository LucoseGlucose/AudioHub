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

namespace AudioHub
{
    [BroadcastReceiver]
    public class MediaButtonReciever : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action != Intent.ActionMediaButton || !intent.HasExtra(Intent.ExtraKeyEvent)) return;
            string action = intent.GetStringExtra(Intent.ExtraKeyEvent);

            if (action == "Previous") SongPlayer.PlayPreviousSong();
            if (action == "Resume") SongPlayer.Resume();
            if (action == "Pause") SongPlayer.Pause(true);
            if (action == "Next") SongPlayer.PlayNextSong();
        }
    }
}