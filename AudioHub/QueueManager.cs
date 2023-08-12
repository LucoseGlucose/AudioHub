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
    public static class QueueManager
    {
        public static readonly LinkedList<Song> songs = new LinkedList<Song>();

        public static bool IsEmpty() => songs.Count == 0;
        public static Song GetNextSong()
        {
            Song song = songs.First.Value;
            songs.RemoveFirst();
            return song;
        }
        public static Playlist GetQueuePlaylist()
        {
            return new Playlist(PlaylistManager.queuePlaylistName, PlaylistManager.GetSongIDsInPlaylist(PlaylistManager.queuePlaylistName));
        }
    }
}