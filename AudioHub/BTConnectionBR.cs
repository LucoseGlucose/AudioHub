using Android.App;
using Android.Bluetooth;
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
    public class BTConnectionBR : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent.Action != BluetoothAdapter.ActionConnectionStateChanged) return;
            int state = intent.GetIntExtra(BluetoothAdapter.ExtraConnectionState, BluetoothAdapter.Error);

            if (state == (int)State.Connected)
            {
                SongPlayer.Resume();
            }
            else if (state == (int)State.Disconnected)
            {
                SongPlayer.Pause();
            }
        }
    }
}