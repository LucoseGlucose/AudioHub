using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AudioHub
{
    [Service(ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
    public class SongPlayer : Service
    {
        public static bool loop;
        public static bool shuffle;

        public static MediaPlayer mediaPlayer;
        public static Song currentSong;
        public static Playlist currentPlaylist;
        public static List<Song> currentSongs;
        public static int currentSongIndex;

        public static event Action<Song, Playlist> OnPlay;
        public static event Action OnResume;
        public static event Action OnPause;
        public static event Action<int> OnSeek;

        public static SongPlayer Instance { get; private set; }

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            Instance = this;

            mediaPlayer = new MediaPlayer();
            mediaPlayer.Completion += (s, e) => PlayNextSong();

            UpdateNotification();
            return StartCommandResult.Sticky;
        }
        public override void OnDestroy()
        {
            mediaPlayer.Release();
            mediaPlayer = null;
            StopSelf();
        }
        private void UpdateNotification()
        {
            Notification.Builder builder = new Notification.Builder(this, "Running")
                .SetContentTitle("MusicService is running")
                .SetContentIntent(PendingIntent.GetActivity(MainActivity.activity, 1,
                new Intent(MainActivity.activity, typeof(MainActivity)), PendingIntentFlags.UpdateCurrent))
                .SetSmallIcon(Resource.Drawable.round_headphones_24);

            StartForeground(2, builder.Build(), ForegroundService.TypeMediaPlayback);
        }
        public static void Play(Song song, Playlist playlist)
        {
            if (string.IsNullOrWhiteSpace(song.id)) return;
            currentSongs ??= PlaylistManager.GetSongsInPlaylist(playlist.title).ToList();

            if (currentSongs.Count > 1) currentSongIndex = currentSongs.IndexOf(song);
            else currentSongIndex = 0;

            if (song.id != currentSong.id || playlist.title != currentPlaylist.title)
            {
                currentSong = song;
                currentPlaylist = playlist;

                mediaPlayer.Reset();
                mediaPlayer.SetDataSource(SongManager.IsSongDownloaded(song.id) ? $"{SongManager.GetSongDirectory(song.id)}/Audio.mp3"
                    : $"{SongManager.SongCacheDirectory}/{song.id}/Audio.mp3");
            }
            else mediaPlayer.Stop();

            mediaPlayer.Prepare();
            mediaPlayer.Start();

            OnPlay?.Invoke(song, playlist);
        }
        public static void Pause()
        {
            mediaPlayer.Pause();
            OnPause?.Invoke();
        }
        public static void Resume()
        {
            mediaPlayer.Start();
            OnResume?.Invoke();
        }
        public static void Seek(int secs)
        {
            mediaPlayer.SeekTo(secs * 1000);
            OnSeek?.Invoke(secs);
        }
        public static void ToggleShuffle()
        {
            shuffle = !shuffle;
            if (shuffle) ShuffleList(currentSongs);
            else currentSongs = PlaylistManager.GetSongsInPlaylist(currentPlaylist.title).ToList();

            currentSongIndex = currentSongs.IndexOf(currentSong);
        }
        public static void ShuffleList<T>(IList<T> list)
        {
            int i = list.Count;

            while (i > 1)
            {
                i--;
                int random = MainActivity.activity.shuffleSeed.Next(i + 1);
                (list[i], list[random]) = (list[random], list[i]);
            }
        }
        public static void PlayNextSong()
        {
            Play(GetNextSong(), currentPlaylist);
        }
        public static void PlayPreviousSong()
        {
            int prev = currentSongIndex - 1;
            if (prev < 0) prev = currentSongs.Count - 1;

            Play(currentSongs[prev], currentPlaylist);
        }
        public static Song GetNextSong()
        {
            if (currentSongs == null || currentSongs.Count == 1) return default;

            if (loop) return currentSong;
            if (!QueueManager.IsEmpty()) return QueueManager.GetNextSong();

            int next = currentSongIndex + 1;
            if (next >= currentSongs.Count) next = 0;

            return currentSongs[next];
        }
    }
}