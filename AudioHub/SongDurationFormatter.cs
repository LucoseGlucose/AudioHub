using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Google.Android.Material.Slider;
using Java.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioHub
{
    public class SongDurationFormatter : Java.Lang.Object, ILabelFormatter
    {
        public JniManagedPeerStates JniManagedPeerState => JniManagedPeerStates.None;

        public string GetFormattedValue(float progress)
        {
            return Song.GetDurationString((int)Math.Floor(progress * SongPlayer.currentSong.durationSecs));
        }
        public void Disposed()
        {

        }
        public void DisposeUnlessReferenced()
        {

        }
        public void Finalized()
        {

        }
        public void SetJniIdentityHashCode(int value)
        {

        }
        public void SetJniManagedPeerState(JniManagedPeerStates value)
        {

        }
        public void SetPeerReference(JniObjectReference reference)
        {

        }
    }
}