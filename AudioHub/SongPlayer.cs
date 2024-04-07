using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.Media.Session;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Media.Session;
using Android.Support.V4.Media;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Android.Support.V4.Media.Session.PlaybackStateCompat;

namespace AudioHub
{
    public class SongPlayer : MediaSessionCompat.Callback
    {
        public static MediaPlayer mediaPlayer { get; private set; }
        public static Song currentSong { get; private set; }
        public static Playlist currentPlaylist { get; private set; }
        public static List<Song> currentSongs { get; private set; }
        public static int currentSongIndex { get; set; }
        public static MediaSessionCompat mediaSession { get; private set; }

        public static AudioFocusListener audioFocusListener;
        public static AudioFocusRequestClass audioFocusRequest;

        public static bool loop;
        public static bool shuffle;

        public static event Action<Song, Playlist> onPlay;
        public static event Action onResume;
        public static event Action onPause;
        public static event Action<int> onSeek;
        public static event Action<bool> onToggleShuffle;
        public static event Action<bool> onToggleLoop;

        public static void Init()
        {
            loop = false;
            shuffle = false;

            currentSong = default;
            currentPlaylist = default;
            currentSongs = null;
            currentSongIndex = 0;

            mediaPlayer = new MediaPlayer();
            audioFocusListener = new AudioFocusListener();

            audioFocusRequest = new AudioFocusRequestClass.Builder(AudioFocus.Gain)
                .SetAudioAttributes(new AudioAttributes.Builder().SetContentType(AudioContentType.Music).Build())
                .SetOnAudioFocusChangeListener(audioFocusListener).Build();

            mediaSession = new MediaSessionCompat(MainActivity.activity, "AudioHub");
            mediaSession.SetCallback(new SongPlayer());
            mediaSession.SetFlags((int)MediaSessionFlags.HandlesMediaButtons | (int)MediaSessionFlags.HandlesTransportControls);
        }
        public static void Cleanup()
        {
            mediaPlayer.Release();
            mediaPlayer = null;

            mediaSession.Release();
            mediaSession = null;
        }
        public static void Play(Song song, Playlist playlist)
        {
            if (string.IsNullOrWhiteSpace(song.id)) return;

            if (currentSongs == null)
            {
                currentSongs = PlaylistManager.GetSongsInPlaylist(playlist.title).ToList();
                if (shuffle) ShuffleList(currentSongs);
            }
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

            ((AudioManager)MainActivity.activity.GetSystemService(Context.AudioService)).RequestAudioFocus(audioFocusRequest);
            
            MediaMetadataCompat.Builder metadataBuilder = new MediaMetadataCompat.Builder();
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
        public static void Pause(bool user)
        {
            mediaPlayer.Pause();

            if (user)
            {
                AudioManager am = MainActivity.activity.GetSystemService(Context.AudioService) as AudioManager;
                am.AbandonAudioFocusRequest(audioFocusRequest);
            }

            UpdatePlaybackState();
            onPause?.Invoke();
        }
        public static void Resume()
        {
            ((AudioManager)MainActivity.activity.GetSystemService(Context.AudioService)).RequestAudioFocus(audioFocusRequest);

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

            onToggleShuffle?.Invoke(shuffle);
            mediaSession.SetShuffleMode(Convert.ToInt32(shuffle));
        }
        public static void ToggleLoop()
        {
            loop = !loop;
            onToggleLoop?.Invoke(loop);
        }
        public static void ShuffleList<T>(IList<T> list)
        {
            if (list == null) return;
            int i = list.Count;

            while (i > 1)
            {
                i--;
                int random = MainActivity.activity.shuffleSeed.Next(i + 1);
                (list[i], list[random]) = (list[random], list[i]);
            }
        }
        public static void PlayNextSong(bool user)
        {
            if (user && MessageListener.tts.IsSpeaking)
            {
                MessageListener.tts.Stop();
                return;
            }

            Song s = GetNextSong();
            if (string.IsNullOrEmpty(s.id)) return;
            Play(s, currentPlaylist);
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
        public static void UpdatePlaybackState()
        {
            PlaybackStateCompat.Builder builder = new PlaybackStateCompat.Builder();
            PlaybackStateCode playbackState = mediaPlayer.IsPlaying ? PlaybackStateCode.Playing : PlaybackStateCode.Paused;

            builder.SetState((int)playbackState, long.Parse(mediaPlayer.CurrentPosition.ToString()), 1f);

            long a = mediaPlayer.IsPlaying ? PlaybackState.ActionPause : PlaybackState.ActionPlay;
            builder.SetActions(a | PlaybackState.ActionSeekTo | PlaybackState.ActionSkipToNext | PlaybackState.ActionSkipToPrevious
                | PlaybackStateCompat.ActionSetShuffleMode | PlaybackStateCompat.ActionSetRepeatMode);

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
            Pause(true);
        }
        public override void OnSeekTo(long pos)
        {
            Seek((int)Math.Floor(pos / 1000f));
        }
        public override void OnSkipToNext()
        {
            PlayNextSong(true);
        }
        public override void OnSkipToPrevious()
        {
            PlayPreviousSong();
        }
        public override void OnStop()
        {
            Pause(false);
        }
        public override void OnSetShuffleMode(int shuffleMode)
        {
            if (shuffleMode > 1) mediaSession.SetShuffleMode(0);
            if (shuffleMode == Convert.ToInt32(shuffle)) return;

            ToggleShuffle();
        }
        public override void OnSetRepeatMode(int repeatMode)
        {
            if (repeatMode > 1) mediaSession.SetRepeatMode(0);
            if (repeatMode == Convert.ToInt32(loop)) return;

            ToggleLoop();
        }
    }
}