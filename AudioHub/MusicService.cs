using Android.App;
using Android.Content;
using Android.Content.PM;
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
{/*
    [Service(ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
    public class MusicService : Service
    {
        public override IBinder OnBind(Intent intent)
        {
            return null;
        }
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            if (SongPlayer.mediaPlayer == null)
            {
                SongPlayer.mediaPlayer = new MediaPlayer();
                SongPlayer.mediaPlayer.Completion += (s, e) => PlayNextSong();
            }

            string command = intent.GetStringExtra("Command");
            if (command == null) return StartCommandResult.Sticky;

            switch (command)
            {
                case "Start":
                    break;
                case "Play":
                    Play(SongManager.GetSongById(intent.GetStringExtra("SongId")),
                        PlaylistManager.GetPlaylistByTitle(intent.GetStringExtra("PlaylistTitle")));
                    break;
                case "Pause":
                    Pause();
                    break;
                case "Resume":
                    Resume();
                    break;
                case "Seek":
                    Seek(intent.GetIntExtra("SeekSecs", SongPlayer.mediaPlayer.CurrentPosition / 1000));
                    break;
                case "ToggleShuffle":
                    ToggleShuffle();
                    break;
                case "PlayNextSong":
                    PlayNextSong();
                    break;
                case "PlayPreviousSong":
                    PlayPreviousSong();
                    break;
                default:
                    return StartCommandResult.Sticky;
            }

            UpdateNotification();
            return StartCommandResult.Sticky;
        }
        public override void OnDestroy()
        {
            SongPlayer.mediaPlayer.Release();
            SongPlayer.mediaPlayer = null;
            StopSelf();
        }
        private Notification.Builder GetUpdatedNotification()
        {
            return new Notification.Builder(this, "Running")
                .SetContentTitle("MusicService is running")
                .SetContentIntent(PendingIntent.GetActivity(MainActivity.activity, 1,
                new Intent(MainActivity.activity, typeof(MainActivity)), PendingIntentFlags.UpdateCurrent))
                .SetSmallIcon(Resource.Drawable.round_headphones_24);
        }
        private void UpdateNotification()
        {
            StartForeground(2, GetUpdatedNotification().Build(), ForegroundService.TypeMediaPlayback);
        }
        private void Play(Song song, Playlist playlist)
        {
            if (string.IsNullOrWhiteSpace(song.id)) return;
            SongPlayer.currentSongs ??= PlaylistManager.GetSongsInPlaylist(playlist.title).ToList();

            if (SongPlayer.currentSongs.Count > 1) SongPlayer.currentSongIndex = SongPlayer.currentSongs.IndexOf(song);
            else SongPlayer.currentSongIndex = 0;

            if (song.id != SongPlayer.currentSong.id || playlist.title != SongPlayer.currentPlaylist.title)
            {
                SongPlayer.currentSong = song;
                SongPlayer.currentPlaylist = playlist;

                SongPlayer.mediaPlayer.Reset();
                SongPlayer.mediaPlayer.SetDataSource(SongManager.IsSongDownloaded(song.id) ? $"{SongManager.GetSongDirectory(song.id)}/Audio.mp3"
                    : $"{SongManager.SongCacheDirectory}/{song.id}/Audio.mp3");
            }
            else SongPlayer.mediaPlayer.Stop();

            SongPlayer.mediaPlayer.Prepare();
            SongPlayer.mediaPlayer.Start();

            SongPlayer.OnPlay?.Invoke(song, playlist);
        }
        private void Pause()
        {
            SongPlayer.mediaPlayer.Pause();
            SongPlayer.OnPause?.Invoke();
        }
        private void Resume()
        {
            SongPlayer.mediaPlayer.Start();
            SongPlayer.OnResume?.Invoke();
        }
        private void Seek(int secs)
        {
            SongPlayer.mediaPlayer.SeekTo(secs * 1000);
            SongPlayer.OnSeek?.Invoke(secs);
        }
        private void ToggleShuffle()
        {
            SongPlayer.shuffle = !SongPlayer.shuffle;
            if (SongPlayer.shuffle) ShuffleList(SongPlayer.currentSongs);
            else SongPlayer.currentSongs = PlaylistManager.GetSongsInPlaylist(SongPlayer.currentPlaylist.title).ToList();
        }
        private void ShuffleList<T>(IList<T> list)
        {
            int i = list.Count;

            while (i > 1)
            {
                i--;
                int random = MainActivity.activity.shuffleSeed.Next(i + 1);
                (list[i], list[random]) = (list[random], list[i]);
            }
        }
        private void PlayNextSong()
        {
            Play(GetNextSong(), SongPlayer.currentPlaylist);
        }
        private void PlayPreviousSong()
        {
            int prev = SongPlayer.currentSongIndex - 1;
            if (prev < 0) prev = SongPlayer.currentSongs.Count - 1;

            Play(SongPlayer.currentSongs[prev], SongPlayer.currentPlaylist);
        }
        private Song GetNextSong()
        {
            if (SongPlayer.currentSongs == null || SongPlayer.currentSongs.Count == 1) return default;

            if (SongPlayer.loop) return SongPlayer.currentSong;
            if (!QueueManager.IsEmpty()) return QueueManager.GetNextSong();

            int next = SongPlayer.currentSongIndex + 1;
            if (next >= SongPlayer.currentSongs.Count) next = 0;

            return SongPlayer.currentSongs[next];
        }
    }*/
}