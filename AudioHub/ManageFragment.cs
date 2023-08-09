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
using Java.Lang;

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

            List<Playlist> playlists = new List<Playlist>() {  };

            RecyclerView rv = view.FindViewById<RecyclerView>(Resource.Id.rvList);
            rv.SetAdapter(new ViewAdapter<Playlist>(playlists, Resource.Layout.item_playlist, BindPlaylistViewAdapter));
            rv.SetLayoutManager(new LinearLayoutManager(view.Context));

            view.FindViewById<Button>(Resource.Id.btnNewPlaylist).Click += (s, e) => ShowNewPlaylistDialog();
        }
        private static void BindPlaylistViewAdapter(RecyclerView.ViewHolder holder, Playlist playlist)
        {
            string songCount = playlist.songs.Length == 1 ? "1 Song" : $"{playlist.songs.Length} Songs";

            holder.ItemView.FindViewById<TextView>(Resource.Id.tvTitle).Text = playlist.title;
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvCount).Text = songCount;

            holder.ItemView.FindViewById<FloatingActionButton>(Resource.Id.fabActions).Click += (s, e) => ShowPlaylistDialog(playlist);
        }
        private static void ShowPlaylistDialog(Playlist playlist)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_playlists, (dialog, view) =>
            {
                view.FindViewById<TextView>(Resource.Id.tvTitle).Text = playlist.title;
                view.FindViewById<TextView>(Resource.Id.tvCount).Text
                    = $"{playlist.songs.Length} song{(playlist.songs.Length == 1 ? "" : "s")}";

                view.FindViewById<Button>(Resource.Id.btnView).Click += (s, e) => ShowViewPlaylistDialog(playlist);
                view.FindViewById<Button>(Resource.Id.btnDelete).Click += (s, e) => { ShowDeletePlaylistDialog(playlist, dialog); };
                view.FindViewById<Button>(Resource.Id.btnRename).Click += (s, e) => { ShowRenamePlaylistDialog(playlist); };
                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => { dialog.Dismiss(); };
            });
        }
        private static void ShowViewPlaylistDialog(Playlist playlist)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_viewPlaylist, (dialog, view) =>
            {
                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => { dialog.Dismiss(); };

                RecyclerView rv = view.FindViewById<RecyclerView>(Resource.Id.rvPlaylistList);
                rv.SetAdapter(new ViewAdapter<Song>(PlaylistManager.GetSongsInPlaylist(playlist),
                    Resource.Layout.item_song, BindSongViewAdapter));

                rv.SetLayoutManager(new LinearLayoutManager(view.Context));
            });
        }
        private static async void BindSongViewAdapter(RecyclerView.ViewHolder holder, Song song)
        {
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvTitle).Text = song.title;
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvArtist).Text = song.artist;
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvDuration).Text = song.GetDurationString();

            Drawable thumbnail = await Drawable.CreateFromPathAsync($"{SongManager.SongDownloadDirectory}/{song.id}/Thumbnail.jpg");

            holder.ItemView.FindViewById<ImageView>(Resource.Id.imgThumbnail)
                .SetImageDrawable(thumbnail);

            holder.ItemView.FindViewById<FloatingActionButton>(Resource.Id.fabActions)
                .Click += (s, e) => ShowSongDialog(song, thumbnail);
        }
        private static void ShowSongDialog(Song song, Drawable thumbnail)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_manage_song, (dialog, view) =>
            {
                view.FindViewById<ImageView>(Resource.Id.imgThumbnail).SetImageDrawable(thumbnail);
                view.FindViewById<TextView>(Resource.Id.tvTitle).Text = song.title;
                view.FindViewById<TextView>(Resource.Id.tvArtist).Text = song.artist;
                view.FindViewById<TextView>(Resource.Id.tvDuration).Text = song.GetDurationString();

                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => { dialog.Dismiss(); };
                view.FindViewById<Button>(Resource.Id.btnDelete).Click += (s, e) => { ShowDeleteSongDialog(song, thumbnail, dialog); };
            });
        }
        private static void ShowDeleteSongDialog(Song song, Drawable thumbnail, Android.App.Dialog prevDialog)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_deleteSong, (dialog, view) =>
            {
                view.FindViewById<ImageView>(Resource.Id.imgThumbnail).SetImageDrawable(thumbnail);
                view.FindViewById<TextView>(Resource.Id.tvTitle).Text = song.title;
                view.FindViewById<TextView>(Resource.Id.tvArtist).Text = song.artist;
                view.FindViewById<TextView>(Resource.Id.tvDuration).Text = song.GetDurationString();

                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => { dialog.Dismiss(); };
                view.FindViewById<Button>(Resource.Id.btnDelete).Click += (s, e) =>
                {
                    prevDialog.Dismiss();
                    dialog.Dismiss();
                    MainActivity.ShowToast($"Song {song.title} successfully deleted");
                };
            });
        }
        private static void ShowDeletePlaylistDialog(Playlist playlist, Android.App.Dialog prevDialog)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_deletePlaylist, (dialog, view) =>
            {
                view.FindViewById<TextView>(Resource.Id.tvTitle).Text = playlist.title;
                view.FindViewById<TextView>(Resource.Id.tvCount).Text
                    = $"{playlist.songs.Length} song{(playlist.songs.Length == 1 ? "" : "s")}";

                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => { dialog.Dismiss(); };
                view.FindViewById<Button>(Resource.Id.btnDelete).Click += (s, e) =>
                {
                    prevDialog.Dismiss();
                    dialog.Dismiss();
                    MainActivity.ShowToast($"Playlist {playlist.title} successfully deleted");
                };
            });
        }
        private static void ShowNewPlaylistDialog()
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_newPlaylist, (dialog, view) =>
            {
                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => { dialog.Dismiss(); };
                view.FindViewById<Button>(Resource.Id.btnCreate).Click += (s, e) =>
                {
                    string title = view.FindViewById<EditText>(Resource.Id.etPlaylistName).Text;

                    if (string.IsNullOrEmpty(title)) MainActivity.ShowToast("Could not create playlist please enter a name");
                    //TODO make sure playlist doesn't exist yet
                    else
                    {
                        MainActivity.ShowToast($"Playlist {title} created successfully");
                    }

                    dialog.Dismiss();
                };
            });
        }
        private static void ShowRenamePlaylistDialog(Playlist playlist)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_renamePlaylist, (dialog, view) =>
            {
                view.FindViewById<TextView>(Resource.Id.tvTitle).Text = playlist.title;
                view.FindViewById<TextView>(Resource.Id.tvCount).Text
                    = $"{playlist.songs.Length} song{(playlist.songs.Length == 1 ? "" : "s")}";

                view.FindViewById<Button>(Resource.Id.btnRename).Click += (s, e) => { dialog.Dismiss(); };
                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => { dialog.Dismiss(); };
            });
        }
    }
}