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
    public static class SongPlayer
    {
        public static MediaPlayer mediaPlayer { get; private set; }
        public static Song currentSong { get; private set; }
        public static Playlist currentPlaylist { get; private set; }
        public static List<Song> currentSongs { get; private set; }
        public static int currentSongIndex { get; private set; }

        public static bool loop;
        public static bool shuffle;

        public static event Action<Song, Playlist> OnPlay;
        public static event Action OnResume;
        public static event Action OnPause;
        public static event Action<int> OnSeek;

        public static void Init()
        {
            mediaPlayer = new MediaPlayer();
            mediaPlayer.Completion += (s, e) => PlayNextSong();
        }
        public static void Cleanup()
        {
            mediaPlayer.Release();
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