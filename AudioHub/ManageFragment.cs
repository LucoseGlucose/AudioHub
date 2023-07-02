using AndroidX.Fragment.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AndroidX.RecyclerView.Widget;
using YoutubeReExplode.Common;
using Google.Android.Material.FloatingActionButton;
using Android.Graphics.Drawables;
using YoutubeReExplode.Playlists;
using static Android.Provider.MediaStore.Audio;
using AndroidX.ConstraintLayout.Widget;

namespace AudioHub
{
    public class ManageFragment : Fragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.fragment_manage, container, false);
        }
        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            List<Playlist> playlists = new List<Playlist>() { new Playlist() { title = "Bangers",
                songs = new LinkedList<Song>(new Song[] { new Song() { title = "Flex", artist = "Polo G ft. Juice Wrld",
                id = 0, durationSecs = 164f, thumbnailPath = "storage/emulated/0/DCIM/Camera/IMG_20230618_200446_345.jpg" } }) } };

            RecyclerView rv = view.FindViewById<RecyclerView>(Resource.Id.rvList);
            rv.SetAdapter(new ViewAdapter<Playlist>(playlists, Resource.Layout.item_playlist, BindPlaylistViewAdapter));
            rv.SetLayoutManager(new LinearLayoutManager(view.Context));
        }
        private static void BindPlaylistViewAdapter(RecyclerView.ViewHolder holder, Playlist playlist)
        {
            string songCount = playlist.songs.Count == 1 ? "1 Song" : $"{playlist.songs.Count} Songs";

            holder.ItemView.FindViewById<TextView>(Resource.Id.tvTitle).Text = playlist.title;
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvCount).Text = songCount;

            holder.ItemView.FindViewById<FloatingActionButton>(Resource.Id.fabActions).Click += (s, e) => ShowPlaylistDialog(playlist);
        }
        private static void ShowPlaylistDialog(Playlist playlist)
        {
            Android.App.Dialog dialog = new Android.App.Dialog(MainActivity.activity, Resource.Style.AppTheme);
            dialog.SetCancelable(true);
            dialog.SetCanceledOnTouchOutside(true);

            View view = LayoutInflater.From(MainActivity.activity).Inflate(Resource.Layout.dialog_playlists, null);

            view.FindViewById<TextView>(Resource.Id.tvTitle).Text = playlist.title;
            view.FindViewById<TextView>(Resource.Id.tvCount).Text
                = $"{playlist.songs.Count} song{(playlist.songs.Count == 1 ? "" : "s")}";

            view.FindViewById<Button>(Resource.Id.btnView).Click += (s, e) => ShowViewPlaylistDialog(playlist);
            view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => { dialog.Dismiss(); };

            dialog.Window.Attributes.WindowAnimations = Resource.Style.Base_Animation_AppCompat_Dialog;
            dialog.SetContentView(view);
            dialog.Create();
            dialog.Show();
        }
        private static void ShowViewPlaylistDialog(Playlist playlist)
        {
            Android.App.Dialog dialog = new Android.App.Dialog(MainActivity.activity, Resource.Style.AppTheme);
            dialog.SetCancelable(true);
            dialog.SetCanceledOnTouchOutside(true);

            View view = LayoutInflater.From(MainActivity.activity).Inflate(Resource.Layout.dialog_viewPlaylist, null);
            view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => { dialog.Dismiss(); };

            RecyclerView rv = view.FindViewById<RecyclerView>(Resource.Id.rvPlaylistList);
            rv.SetAdapter(new ViewAdapter<Song>(playlist.songs, Resource.Layout.item_song, BindSongViewAdapter));
            rv.SetLayoutManager(new LinearLayoutManager(view.Context));

            dialog.Window.Attributes.WindowAnimations = Resource.Style.Base_Animation_AppCompat_Dialog;
            dialog.SetContentView(view);
            dialog.Create();
            dialog.Show();
        }
        private static async void BindSongViewAdapter(RecyclerView.ViewHolder holder, Song song)
        {
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvTitle).Text = song.title;
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvArtist).Text = song.artist;
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvDuration).Text = song.GetDurationString();

            Drawable thumbnail = await Drawable.CreateFromPathAsync(song.thumbnailPath);

            holder.ItemView.FindViewById<ImageView>(Resource.Id.imgThumbnail)
                .SetImageDrawable(thumbnail);

            holder.ItemView.FindViewById<FloatingActionButton>(Resource.Id.fabActions)
                .Click += (s, e) => ShowSongDialog(song, thumbnail);
        }
        private static void ShowSongDialog(Song song, Drawable thumbnail)
        {
            Android.App.Dialog dialog = new Android.App.Dialog(MainActivity.activity, Resource.Style.AppTheme);
            dialog.SetCancelable(true);
            dialog.SetCanceledOnTouchOutside(true);

            View view = LayoutInflater.From(MainActivity.activity).Inflate(Resource.Layout.dialog_manage_song, null);

            view.FindViewById<ImageView>(Resource.Id.imgThumbnail).SetImageDrawable(thumbnail);
            view.FindViewById<TextView>(Resource.Id.tvTitle).Text = song.title;
            view.FindViewById<TextView>(Resource.Id.tvArtist).Text = song.artist;
            view.FindViewById<TextView>(Resource.Id.tvDuration).Text = song.GetDurationString();

            view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => { dialog.Dismiss(); };

            dialog.Window.Attributes.WindowAnimations = Resource.Style.Base_Animation_AppCompat_Dialog;
            dialog.SetContentView(view);
            dialog.Create();
            dialog.Show();
        }
    }
}