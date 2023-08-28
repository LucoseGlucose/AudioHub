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

        public static event Action<Song> OnPlay;
        public static event Action OnResume;
        public static event Action OnPause;
        public static event Action<int> OnSeek;

        public static void Init()
        {
            mediaPlayer = new MediaPlayer();
        }
        public static void Cleanup()
        {
            mediaPlayer.Release();
        }
        public static void Play(Song song)
        {
            if (song.id != currentSong.id)
            {
                currentSong = song;

                mediaPlayer.Reset();
                mediaPlayer.SetDataSource($"{SongManager.GetSongDirectory(song.id)}/Audio.mp3");
            }
            else mediaPlayer.Stop();

            mediaPlayer.Prepare();
            mediaPlayer.Start();

            OnPlay?.Invoke(song);
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
    }
}