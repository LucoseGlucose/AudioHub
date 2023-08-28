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
        public static Stack<(Song, Playlist)> previousSongs = new Stack<(Song, Playlist)>();

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

            if (song.id != currentSong.id || playlist.title != currentPlaylist.title)
            {
                previousSongs.Push((currentSong, currentPlaylist));

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
        public static void PlayNextSong()
        {
            Play(GetNextSong(), currentPlaylist);
        }
        public static void PlayPreviousSong()
        {
            if (!previousSongs.TryPop(out (Song, Playlist) previous)) return;
            Play(previous.Item1, previous.Item2);
        }
        public static Song GetNextSong()
        {
            if (loop) return currentSong;

            if (shuffle)
            {
                if (!QueueManager.IsEmpty())
                {
                    Song song = GetRandomSongFromPlaylist(PlaylistManager.queuePlaylistName, currentSong.id);
                    QueueManager.songs.Remove(song);
                    return song;
                }

                return GetRandomSongFromPlaylist(currentPlaylist.title, currentSong.id);
            }

            if (!QueueManager.IsEmpty()) return QueueManager.GetNextSong();

            List<Song> songs = PlaylistManager.GetSongsInPlaylist(currentPlaylist.title).ToList();
            if (!songs.Contains(currentSong)) return default;

            int i = songs.IndexOf(currentSong);
            if (i + 1 >= songs.Count) return songs[0];

            return songs[i + 1];
        }
        public static Song GetRandomSongFromPlaylist(string title, string currentId)
        {
            string[] ids = PlaylistManager.GetSongIDsInPlaylist(title);
            string randomId = currentId;

            while (randomId == currentId)
            {
                randomId = ids[new Random().Next(0, ids.Length)];
            }

            return SongManager.GetSongById(randomId);
        }
    }
}