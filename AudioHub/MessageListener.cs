using Android.Accounts;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Service.Notification;
using Android.Speech.Tts;
using Android.Views;
using Android.Widget;
using Java.Util;
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

        private Handler delayHandler;
        private const long ttsDelayMillis = 500;
        private VolumeShaper.Configuration volumeConfig;
        private VolumeShaper volumeShaper;

        public override void OnNotificationPosted(StatusBarNotification sbn)
        {
            if (!readMessages || SongPlayer.mediaPlayer == null || !SongPlayer.mediaPlayer.IsPlaying || sbn.Id == 2) return;
            Bundle extras = sbn.Notification.Extras;

            string title = ConvertToASCIIIfNotAllEmojis(extras.GetString(Notification.ExtraTitle));
            string text = ConvertToASCIIIfNotAllEmojis(extras.GetString(Notification.ExtraText));

            if (title.Contains("Cable charging")) return;
            if (title == prevTitle && text == prevText) return;

            prevTitle = title;
            prevText = text;

            volumeShaper ??= SongPlayer.mediaPlayer.CreateVolumeShaper(volumeConfig);
            volumeShaper.Apply(VolumeShaper.Operation.Play);

            if (title != null) delayHandler.PostDelayed(() => tts.Speak(title, QueueMode.Add, paramsBundle, title), ttsDelayMillis);
            if (text != null) delayHandler.PostDelayed(() => tts.Speak(text, QueueMode.Add, paramsBundle, text), ttsDelayMillis * 2);
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
            volumeConfig = new VolumeShaper.Configuration.Builder()
                .SetCurve(new[] { 0f, 1f }, new[] { 1f, .1f }).SetDuration(ttsDelayMillis)
                .SetInterpolatorType(VolumeInterpolatorType.Linear).Build();

            delayHandler = new Handler(Looper.MyLooper());

            paramsBundle = new Bundle();
            paramsBundle.PutString(TextToSpeech.Engine.KeyParamUtteranceId, "");

            tts = new TextToSpeech(this, new TTSInitListener(() =>
            {
                tts.SetSpeechRate(.8f);
                tts.SetPitch(.9f);
            }),
            "com.google.android.tts");

            tts.SetOnUtteranceProgressListener(new TTSProgressListener(id => 
            {
                bool text = prevText != null && id == prevText;
                bool title = prevTitle != null && prevText == null && id == prevTitle;

                if (title || text) delayHandler.PostDelayed(() => volumeShaper.Apply(VolumeShaper.Operation.Reverse), ttsDelayMillis);
            }));
        }
        public override void OnListenerDisconnected()
        {
            tts.Shutdown();
        }
    }
}