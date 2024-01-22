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

            KeyEvent ke = (KeyEvent)intent.GetParcelableExtra(Intent.ExtraKeyEvent, Java.Lang.Class.FromType(typeof(KeyEvent)));
            SongPlayer.mediaSession.Controller.DispatchMediaButtonEvent(ke);
        }
    }
}