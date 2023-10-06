using Android.App;
using Android.Content;
using Android.Media;
using Android.Media.Session;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Media;
using Android.Support.V4.Media.Session;
using AndroidX.Media;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioHub
{
    [Service]
    public class MusicService : MediaBrowserServiceCompat
    {
        public MediaSessionCompat mediaSession;
        public PlaybackStateCompat.Builder stateBuilder;

        public override void OnCreate()
        {
            base.OnCreate();

            mediaSession = new MediaSessionCompat(this, "MusicService");
            mediaSession.SetFlags(1 | 2);

            stateBuilder = new PlaybackStateCompat.Builder().SetActions(PlaybackStateCompat.ActionPlay | PlaybackStateCompat.ActionPlayPause
                | PlaybackStateCompat.ActionSeekTo | PlaybackStateCompat.ActionSkipToNext | PlaybackStateCompat.ActionSkipToPrevious |
                PlaybackStateCompat.ActionSetShuffleMode | PlaybackStateCompat.ActionSetRepeatMode | PlaybackStateCompat.ActionPlayFromMediaId);

            mediaSession.SetPlaybackState(stateBuilder.Build());
            mediaSession.SetCallback(new MediaSessionCallback(this));
            SessionToken = mediaSession.SessionToken;

            SongPlayer.Play(PlaylistManager.GetSongsInPlaylist(PlaylistManager.downloadedPlaylistName).First(),
                PlaylistManager.GetDownloadedSongsPlaylist());
        }
        public override BrowserRoot OnGetRoot(string clientPackageName, int clientUid, Bundle rootHints)
        {
            return new BrowserRoot("Playlists", null);
        }
        public override void OnLoadChildren(string parentId, Result result)
        {
            JavaList<MediaBrowserCompat.MediaItem> items = new JavaList<MediaBrowserCompat.MediaItem>();
            MediaDescriptionCompat.Builder descBuilder = new MediaDescriptionCompat.Builder();

            if (parentId == "Playlists")
            {
                foreach (Playlist playlist in PlaylistManager.GetPlaylists())
                {
                    items.Add(new MediaBrowserCompat.MediaItem(
                        descBuilder.SetTitle(playlist.title).SetSubtitle(playlist.songs.Length.ToString()).Build(), 0));
                }
            }
            else if (PlaylistManager.GetPlaylistNames().Any(n => n == parentId))
            {
                foreach (Song song in PlaylistManager.GetSongsInPlaylist(PlaylistManager.GetPlaylistNames().First(n => n == parentId)))
                {
                    items.Add(new MediaBrowserCompat.MediaItem(descBuilder.SetTitle(song.title).SetSubtitle(song.artist)
                        .SetDescription(song.GetDurationString())
                        .SetIconUri(Uri.Parse($"{SongManager.GetSongDirectory(song.id)}/Thumbnail.jpg")).SetMediaId(song.id).Build(), 0));
                }
            }

            result.SendResult(items);
        }
    }
}