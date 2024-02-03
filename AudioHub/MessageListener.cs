using Android.Accounts;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Service.Notification;
using Android.Speech.Tts;
using Android.Views;
using Android.Widget;
using Javax.Security.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioHub
{
    [Service(Permission = "android.permission.BIND_NOTIFICATION_LISTENER_SERVICE", Exported = true)]
    [IntentFilter(new[] { "android.service.notification.NotificationListenerService" })]
    public class MessageListener : NotificationListenerService
    {
        public static bool readMessages;

        private TextToSpeech tts;
        private Bundle paramsBundle;

        private string prevTitle;
        private string prevText;
        private string prevSubText;

        public override void OnNotificationPosted(StatusBarNotification sbn)
        {
            if (!readMessages || sbn.Id == 2) return;
            Bundle extras = sbn.Notification.Extras;

            string title = ConvertToASCIIIfNotAllEmojis(extras.GetString(Notification.ExtraTitle));
            string text = ConvertToASCIIIfNotAllEmojis(extras.GetString(Notification.ExtraText));
            string subText = ConvertToASCIIIfNotAllEmojis(extras.GetString(Notification.ExtraSubText));

            if (title.Contains("Cable charging")) return;
            if (title == prevTitle && text == prevText && subText == prevSubText) return;

            prevTitle = title;
            prevText = text;
            prevSubText = subText;

            SongPlayer.mediaPlayer.SetVolume(.1f, .1f);

            if (title != null) tts.Speak(title, QueueMode.Add, paramsBundle, title);
            if (text != null) tts.Speak(text, QueueMode.Add, paramsBundle, text);
            if (subText != null) tts.Speak(subText, QueueMode.Add, paramsBundle, subText);
        }
        private string ConvertToASCII(string str)
        {
            return string.Join("", str.ToCharArray().Where(x => x < 127));
        }
        private string ConvertToASCIIIfNotAllEmojis(string str)
        {
            if (str != null && !str.All(c => c > 126)) return ConvertToASCII(str);
            return str;
        }
        public override void OnListenerConnected()
        {
            paramsBundle = new Bundle();
            paramsBundle.PutString(TextToSpeech.Engine.KeyParamUtteranceId, "");

            tts = new TextToSpeech(this, null);
            tts.SetOnUtteranceProgressListener(new TTSProgressListener(id => 
            {
                bool subText = prevSubText != null && id == prevSubText;
                bool text = prevText != null && prevSubText == null && id == prevText;
                bool title = prevTitle != null && prevText == null && prevSubText == null && id == prevTitle;

                if (title || text || subText) SongPlayer.mediaPlayer.SetVolume(1f, 1f);
            }));
        }
        public override void OnListenerDisconnected()
        {
            tts.Shutdown();
        }
    }
}