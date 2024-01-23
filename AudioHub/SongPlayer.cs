using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.Media.Session;
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
    public class SongPlayer : MediaSession.Callback
    {
        public static MediaPlayer mediaPlayer { get; private set; }
        public static Song currentSong { get; private set; }
        public static Playlist currentPlaylist { get; private set; }
        public static List<Song> currentSongs { get; private set; }
        public static int currentSongIndex { get; private set; }
        public static MediaSession mediaSession { get; private set; }

        public static AudioFocusListener audioFocusListener;
        public static bool loop;
        public static bool shuffle;

        public static event Action<Song, Playlist> onPlay;
        public static event Action onResume;
        public static event Action onPause;
        public static event Action<int> onSeek;

        public static void Init()
        {
            mediaPlayer = new MediaPlayer();
            audioFocusListener = new AudioFocusListener();

            mediaSession = new MediaSession(MainActivity.activity, "AudioHub");
            mediaSession.SetCallback(new SongPlayer());
            mediaSession.SetFlags(MediaSessionFlags.HandlesMediaButtons | MediaSessionFlags.HandlesTransportControls);
        }
        public static void Cleanup()
        {
            mediaPlayer.Release();
            mediaPlayer = null;

            currentSong = default;
            currentPlaylist = default;
            currentSongs = null;
            currentSongIndex = 0;
        }
        public static void Play(Song song, Playlist playlist)
        {
            if (string.IsNullOrWhiteSpace(song.id)) return;

            currentSongs ??= PlaylistManager.GetSongsInPlaylist(playlist.title).ToList();
            currentSongIndex = currentSongs.IndexOf(song);

            if (song.id != currentSong.id || playlist.title != currentPlaylist.title)
            {
                currentSong = song;
                currentPlaylist = playlist;

                mediaPlayer.Reset();
                mediaPlayer.SetDataSource(SongManager.IsSongDownloaded(song.id) ? $"{SongManager.GetSongDirectory(song.id)}/Audio.mp3"
                    : $"{SongManager.SongCacheDirectory}/{song.id}/Audio.mp3");
            }
            else mediaPlayer.Stop();

            MediaMetadata.Builder metadataBuilder = new MediaMetadata.Builder();
            metadataBuilder.PutString(MediaMetadata.MetadataKeyTitle, song.title);
            metadataBuilder.PutString(MediaMetadata.MetadataKeyArtist, song.artist);
            metadataBuilder.PutLong(MediaMetadata.MetadataKeyDuration, song.durationSecs * 1000);

            string thumbnailPath = SongManager.IsSongDownloaded(currentSong.id) ?
                $"{SongManager.GetSongDirectory(currentSong.id)}/Thumbnail.jpg"
                : $"{SongManager.SongCacheDirectory}/{currentSong.id}/Thumbnail.jpg";

            metadataBuilder.PutBitmap(MediaMetadata.MetadataKeyAlbumArt, BitmapFactory.DecodeFile(thumbnailPath));
            mediaSession.SetMetadata(metadataBuilder.Build());

            mediaPlayer.Prepare();
            mediaPlayer.Start();

            UpdatePlaybackState();
            mediaSession.Active = true;
            onPlay?.Invoke(song, playlist);
        }
        public static void Pause()
        {
            mediaPlayer.Pause();

            UpdatePlaybackState();
            onPause?.Invoke();
        }
        public static void Resume()
        {
            mediaSession.Active = true;
            mediaPlayer.Start();

            UpdatePlaybackState();
            onResume?.Invoke();
        }
        public static void Seek(int secs)
        {
            mediaPlayer.SeekTo(secs * 1000);

            UpdatePlaybackState();
            onSeek?.Invoke(secs);
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
        public static void RequestAudioFocus()
        {
            AudioFocusRequestClass focusRequest = new AudioFocusRequestClass.Builder(AudioFocus.Gain)
                .SetAudioAttributes(new AudioAttributes.Builder().SetContentType(AudioContentType.Music).Build())
                .SetOnAudioFocusChangeListener(audioFocusListener).Build();

            AudioManager audioManager = (AudioManager)MainActivity.activity.GetSystemService(Context.AudioService);
            audioManager.RequestAudioFocus(focusRequest);
        }
        public static void UpdatePlaybackState()
        {
            PlaybackState.Builder builder = new PlaybackState.Builder();
            PlaybackStateCode playbackState = mediaPlayer.IsPlaying ? PlaybackStateCode.Playing : PlaybackStateCode.Paused;

            builder.SetState(playbackState, long.Parse(mediaPlayer.CurrentPosition.ToString()), 1f);

            long a = mediaPlayer.IsPlaying ? PlaybackState.ActionPause : PlaybackState.ActionPlay;
            builder.SetActions(a | PlaybackState.ActionSeekTo | PlaybackState.ActionSkipToNext | PlaybackState.ActionSkipToPrevious);

            mediaSession.SetPlaybackState(builder.Build());
            MainActivity.UpdateService();
        }
        public override void OnPlay()
        {
            Resume();
        }
        public override void OnPlayFromMediaId(string mediaId, Bundle extras)
        {
            Play(SongManager.GetSongById(mediaId), PlaylistManager.GetPlaylistByTitle(extras.GetString("Playlist")));
        }
        public override void OnPause()
        {
            Pause();
        }
        public override void OnSeekTo(long pos)
        {
            Seek((int)Math.Floor(pos / 1000f));
        }
        public override void OnSkipToNext()
        {
            PlayNextSong();
        }
        public override void OnSkipToPrevious()
        {
            PlayPreviousSong();
        }
        public override void OnStop()
        {
            Pause();
        }
    }
}