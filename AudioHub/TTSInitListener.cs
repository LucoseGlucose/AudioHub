using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Speech.Tts;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioHub
{
    public class TTSInitListener : Java.Lang.Object, TextToSpeech.IOnInitListener
    {
        private Action onInit;

        public TTSInitListener(Action onInit)
        {
            this.onInit = onInit;
        }
        public void OnInit(OperationResult status)
        {
            onInit();
        }
    }
}