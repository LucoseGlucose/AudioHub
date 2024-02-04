using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Xml;

namespace AudioHub
{
    public static class PlaylistManager
    {
        public static string PlaylistDirectory => $"{MainActivity.activity.GetExternalFilesDir(null).AbsolutePath}/Playlists";
        public const string downloadedPlaylistName = "Downloaded";
        public const string queuePlaylistName = "Queue";
        public const string tempPlaylistName = "Temporary";

        public static string[] GetPlaylistNames()
        {
            string[] dirs = Directory.GetDirectories(PlaylistDirectory);
            for (int i = 0; i < dirs.Length; i++)
            {
                DirectoryInfo info = new DirectoryInfo(dirs[i]);
                dirs[i] = info.Name;
            }
            return dirs;
        }
        public static Playlist[] GetPlaylists()
        {
            if (!Directory.Exists(PlaylistDirectory))
            {
                Directory.CreateDirectory(PlaylistDirectory);
                return Array.Empty<Playlist>();
            }

            string[] playlistNames = GetPlaylistNames();
            Playlist[] playlists = new Playlist[playlistNames.Length];

            for (int i = 0; i < playlists.Length; i++)
            {
                playlists[i] = new Playlist(playlistNames[i], GetSongIDsInPlaylist(playlistNames[i]));
            }

            return playlists;
        }
        public static void CreatePlaylist(string title)
        {
            if (!Directory.Exists(PlaylistDirectory)) Directory.CreateDirectory(PlaylistDirectory);

            string playlistDir = $"{PlaylistDirectory}/{title}";
            if (!Directory.Exists(playlistDir)) Directory.CreateDirectory(playlistDir);
        }
        public static void DeletePlaylist(string title)
        {
            string playlistDir = $"{PlaylistDirectory}/{title}";
            if (Directory.Exists(playlistDir)) Directory.Delete(playlistDir, true);
        }
        public static string[] GetSongIDsInPlaylist(string title)
        {
            if (title == downloadedPlaylistName) return GetDownloadedSongsPlaylist().songs;
            if (title == tempPlaylistName) return GetTemporarySongsPlaylist().songs;
            if (title == queuePlaylistName)
            {
                string[] ids = new string[QueueManager.songs.Count];
                int i = 0;

                foreach (Song song in QueueManager.songs)
                {
                    ids[i] = song.id;
                    i++;
                }

                return ids;
            }

            string[] files = Directory.EnumerateFiles($"{PlaylistDirectory}/{title}")
                .OrderBy(f => new FileInfo(f).CreationTimeUtc.Ticks).ToArray();

            for (int i = 0; i < files.Length; i++)
            {
                files[i] = Path.GetFileNameWithoutExtension(files[i]);
            }
            return files;
        }
        public static Song[] GetSongsInPlaylist(string title)
        {
            if (title == downloadedPlaylistName) return SongManager.GetSongsFromIDs(GetDownloadedSongsPlaylist().songs);
            if (title == queuePlaylistName) return QueueManager.songs.ToArray();
            if (title == tempPlaylistName) return SongManager.GetSongsFromIDs(GetTemporarySongsPlaylist().songs);

            if (string.IsNullOrWhiteSpace(title)) return Array.Empty<Song>();

            string[] songIDs = GetSongIDsInPlaylist(title);
            Song[] songs = new Song[songIDs.Length];

            for (int i = 0; i < songs.Length; i++)
            {
                songs[i] = SongManager.GetSongById(songIDs[i]);
            }
            return songs;
        }
        public static void AddSongToPlaylist(string playlist, string songId)
        {
            string dir = $"{PlaylistDirectory}/{playlist}";
            if (Directory.Exists(dir)) File.Create($"{dir}/{songId}.song");
        }
        public static void RemoveSongFromPlaylist(string playlist, string songId)
        {
            string path = $"{PlaylistDirectory}/{playlist}/{songId}.song";
            if (File.Exists(path)) File.Delete(path);
        }
        public static Playlist GetDownloadedSongsPlaylist()
        {
            string[] songPaths = Directory.EnumerateDirectories(SongManager.SongDownloadDirectory)
                .OrderBy(s => new DirectoryInfo(s).CreationTimeUtc.Ticks).ToArray();

            for (int i = 0; i < songPaths.Length; i++)
            {
                DirectoryInfo info = new DirectoryInfo(songPaths[i]);
                songPaths[i] = info.Name;
            }

            return new Playlist(downloadedPlaylistName, songPaths);
        }
        public static Playlist GetTemporarySongsPlaylist()
        {
            string[] songPaths = Directory.EnumerateDirectories(SongManager.SongCacheDirectory)
                .OrderBy(s => new DirectoryInfo(s).CreationTimeUtc.Ticks).ToArray();

            for (int i = 0; i < songPaths.Length; i++)
            {
                DirectoryInfo info = new DirectoryInfo(songPaths[i]);
                songPaths[i] = info.Name;
            }

            return new Playlist(tempPlaylistName, songPaths);
        }
        public static bool IsSongInPlaylist(string playlist, string songId)
        {
            return File.Exists($"{PlaylistDirectory}/{playlist}/{songId}.song");
        }
        public static Playlist GetPlaylistByTitle(string title)
        {
            return new Playlist(title, GetSongIDsInPlaylist(title));
        }
    }
}