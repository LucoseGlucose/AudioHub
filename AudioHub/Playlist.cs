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

namespace AudioHub
{
    public class Playlist
    {
        public string title;
        public LinkedList<Song> songs = new LinkedList<Song>();

        public Playlist()
        {

        }
        public Playlist(string title, LinkedList<Song> songs)
        {
            this.title = title;
            this.songs = songs;
        }
    }
}