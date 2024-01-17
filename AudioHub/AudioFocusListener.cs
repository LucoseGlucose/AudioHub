using Android.App;
using Android.Content;
using Android.Media;
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
    public class AudioFocusListener : Java.Lang.Object, AudioManager.IOnAudioFocusChangeListener
    {
        public void OnAudioFocusChange(AudioFocus focusChange)
        {
            if (focusChange == AudioFocus.Loss)
            {
                SongPlayer.Pause();
            }
        }
    }
}