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
using static Android.Icu.Text.CaseMap;

namespace AudioHub
{
    [Service(Permission = "android.permission.BIND_NOTIFICATION_LISTENER_SERVICE", Exported = true)]
    [IntentFilter(new[] { "android.service.notification.NotificationListenerService" })]
    public class MessageListener : NotificationListenerService
    {
        public static bool readMessages;

        public static TextToSpeech tts;
        private Bundle paramsBundle;

        private string prevTitle;
        private string prevText;
        private int prevId;

        private int consecutiveNotifs;
        private int maxConsecutiveNotifs = 1;
        private long lastPostTime;
        private const long consecutivePostTime = 15000;

        private Handler delayHandler;
        private const long ttsDelayMillis = 300;
        private VolumeShaper.Configuration volumeConfig;
        private VolumeShaper volumeShaper;

        private string[] blacklistTitles = new string[] { "charging", "Me" };
        private string[] blacklistTexts = new string[] {  };
        private Queue<string> speakingQueue = new Queue<string>();

        public override void OnNotificationPosted(StatusBarNotification sbn)
        {
            if (!readMessages || sbn.Id == 2 || SongPlayer.mediaPlayer == null || !SongPlayer.mediaPlayer.IsPlaying) return;
            Bundle extras = sbn.Notification.Extras;

            string title = ConvertToASCIIIfNotAllEmojis(extras.GetString(Notification.ExtraTitle));
            string text = ConvertToASCIIIfNotAllEmojis(extras.GetString(Notification.ExtraText));

            if (sbn.Id == prevId) return;

            if (title == prevTitle)
            {
                if (text == prevText) return;
                consecutiveNotifs++;
            }
            else consecutiveNotifs = 0;

            if ((title != null && blacklistTitles.Any(t => title.Contains(t)))
                || (title != null && blacklistTexts.Any(t => text.Contains(t)))) return;

            prevTitle = title;
            prevText = text;
            prevId = sbn.Id;

            int prevCount = speakingQueue.Count;
            long prevPostTime = lastPostTime;

            lastPostTime = sbn.PostTime;

            if (((prevCount < 1 || sbn.PostTime - prevPostTime > consecutivePostTime)
                || consecutiveNotifs == 0) && !string.IsNullOrEmpty(title)) speakingQueue.Enqueue(title);
            if (!string.IsNullOrEmpty(text)
                && (prevCount < 1 || consecutiveNotifs < maxConsecutiveNotifs)) speakingQueue.Enqueue(text);

            if (prevCount < 1)
            {
                volumeShaper ??= SongPlayer.mediaPlayer.CreateVolumeShaper(volumeConfig);

                try
                {
                    volumeShaper.Apply(VolumeShaper.Operation.Play);
                }
                catch
                {
                    volumeShaper.Dispose();
                    volumeShaper = SongPlayer.mediaPlayer.CreateVolumeShaper(volumeConfig);
                    volumeShaper.Apply(VolumeShaper.Operation.Play);
                }

                CheckQueue();
            }
        }
        private bool CheckQueue()
        {
            if (speakingQueue.TryPeek(out string message))
            {
                delayHandler.PostDelayed(() => tts.Speak(message, QueueMode.Add, paramsBundle, message), ttsDelayMillis);
                return true;
            }

            return false;
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
                speakingQueue.Dequeue();

                if (!CheckQueue())
                {
                    volumeShaper.Apply(VolumeShaper.Operation.Reverse);
                    delayHandler.PostDelayed(() =>
                    {
                        if (speakingQueue.Count < 1)
                        {
                            volumeShaper.Dispose();
                            volumeShaper = null;
                        }
                    },
                    ttsDelayMillis);
                }
            }));
        }
        public override void OnListenerDisconnected()
        {
            tts.Shutdown();
        }
    }
}