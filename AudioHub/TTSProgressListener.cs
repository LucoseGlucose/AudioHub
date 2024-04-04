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
    public class TTSProgressListener : UtteranceProgressListener
    {
        private Action<string> doneAction;

        public TTSProgressListener(Action<string> doneAction)
        {
            this.doneAction = doneAction;
        }
        public override void OnDone(string utteranceId)
        {
            doneAction(utteranceId);
        }

        [Obsolete]
        public override void OnError(string utteranceId)
        {

        }

        public override void OnStart(string utteranceId)
        {

        }

        public override void OnStop(string utteranceId, bool interrupted)
        {
            doneAction(utteranceId);
        }
    }
}