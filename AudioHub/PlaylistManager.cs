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
        public static string[] GetSongsInPlaylist(string title)
        {
            return Directory.GetFiles($"{PlaylistDirectory}/{title}");
        }
        public static Song[] GetSongsInPlaylist(Playlist playlist)
        {
            Song[] songs = new Song[playlist.songs.Length];
            for (int i = 0; i < songs.Length; i++)
            {
                songs[i] = SongManager.GetSongById(playlist.songs[i]);
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
    }
}