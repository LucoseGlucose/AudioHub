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
using AndroidX.ConstraintLayout.Widget;
using System.IO;
using static Android.Provider.MediaStore.Audio;

namespace AudioHub
{
    public class ManageFragment : Fragment
    {
        private ViewAdapter<Playlist> playlistVA;
        private ViewAdapter<Song> songVA;
        private Playlist currentPlaylist;

        private ProgressBar progressBar;
        private Progress<double> downloadProgress;

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

            Playlist[] playlists = PlaylistManager.GetPlaylists();
            playlistVA = new ViewAdapter<Playlist>(playlists, Resource.Layout.item_playlist, BindPlaylistViewAdapter);

            RecyclerView rv = view.FindViewById<RecyclerView>(Resource.Id.rvList);
            rv.SetAdapter(playlistVA);
            rv.SetLayoutManager(new LinearLayoutManager(view.Context));

            view.FindViewById<Button>(Resource.Id.btnNewPlaylist).Click += (s, e) => ShowNewPlaylistDialog();

            Playlist downloadedSongs = PlaylistManager.GetDownloadedSongsPlaylist();
            View downloadedPlaylist = view.FindViewById<View>(Resource.Id.iDownloadedPlaylist);

            downloadedPlaylist.FindViewById<TextView>(Resource.Id.tvTitle).Text = downloadedSongs.title;
            downloadedPlaylist.FindViewById<TextView>(Resource.Id.tvCount).Text =
                $"{downloadedSongs.songs.Length} song{(downloadedSongs.songs.Length == 1 ? "" : "s")}";

            FloatingActionButton fabDownloadedActions = downloadedPlaylist.FindViewById<FloatingActionButton>(Resource.Id.fabActions);
            fabDownloadedActions.SetImageDrawable(Context.GetDrawable(Resource.Drawable.round_visibility_24));
            fabDownloadedActions.Click += (s, e) => ShowViewPlaylistDialog(downloadedSongs);

            View queuePlaylist = view.FindViewById<View>(Resource.Id.iQueuePlaylist);
            queuePlaylist.FindViewById<TextView>(Resource.Id.tvTitle).Text = "Queue";

            FloatingActionButton fabQueueActions = queuePlaylist.FindViewById<FloatingActionButton>(Resource.Id.fabActions);
            fabQueueActions.SetImageDrawable(Context.GetDrawable(Resource.Drawable.round_visibility_24));
            //fabQueueActions.Click += (s, e) => ShowViewPlaylistDialog(PlaylistManager.GetDownloadedSongsPlaylist());

            progressBar = view.FindViewById<ProgressBar>(Resource.Id.lpiProgress);
            progressBar.Min = 0;
            progressBar.Max = 100;

            progressBar.Indeterminate = false;
            progressBar.SetProgress(0, true);

            downloadProgress = new Progress<double>(progress =>
                progressBar.SetProgress((int)Math.Round(progress * 100), true));

            songVA = new ViewAdapter<Song>(Array.Empty<Song>(), Resource.Layout.item_song, BindSongViewAdapter);
        }
        private void BindPlaylistViewAdapter(RecyclerView.ViewHolder holder, Playlist playlist)
        {
            string songCount = playlist.songs.Length == 1 ? "1 Song" : $"{playlist.songs.Length} Songs";

            holder.ItemView.FindViewById<TextView>(Resource.Id.tvTitle).Text = playlist.title;
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvCount).Text = songCount;

            holder.ItemView.FindViewById<FloatingActionButton>(Resource.Id.fabActions).Click += (s, e) => ShowPlaylistDialog(playlist);
        }
        private void ShowPlaylistDialog(Playlist playlist)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_playlists, (dialog, view) =>
            {
                view.FindViewById<TextView>(Resource.Id.tvTitle).Text = playlist.title;
                view.FindViewById<TextView>(Resource.Id.tvCount).Text
                    = $"{playlist.songs.Length} song{(playlist.songs.Length == 1 ? "" : "s")}";

                view.FindViewById<Button>(Resource.Id.btnView).Click += (s, e) => ShowViewPlaylistDialog(playlist);
                view.FindViewById<Button>(Resource.Id.btnDelete).Click += (s, e) => ShowDeletePlaylistDialog(playlist, dialog);
                view.FindViewById<Button>(Resource.Id.btnRename).Click += (s, e) => ShowRenamePlaylistDialog(playlist);
                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => dialog.Dismiss();
            });
        }
        private void ShowViewPlaylistDialog(Playlist playlist)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_viewPlaylist, (dialog, view) =>
            {
                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => dialog.Dismiss();

                RecyclerView rv = view.FindViewById<RecyclerView>(Resource.Id.rvPlaylistList);
                rv.SetAdapter(songVA);
                rv.SetLayoutManager(new LinearLayoutManager(view.Context));

                songVA.items = PlaylistManager.GetSongsInPlaylist(playlist);
                currentPlaylist = playlist;
            });
        }
        private async void BindSongViewAdapter(RecyclerView.ViewHolder holder, Song song)
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
        private void ShowSongDialog(Song song, Drawable thumbnail)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_manage_song, (dialog, view) =>
            {
                view.FindViewById<ImageView>(Resource.Id.imgThumbnail).SetImageDrawable(thumbnail);
                view.FindViewById<TextView>(Resource.Id.tvTitle).Text = song.title;
                view.FindViewById<TextView>(Resource.Id.tvArtist).Text = song.artist;
                view.FindViewById<TextView>(Resource.Id.tvDuration).Text = song.GetDurationString();

                Button btnDownload = view.FindViewById<Button>(Resource.Id.btnDownload);
                if (!SongManager.IsSongDownloaded(song.id))
                {
                    btnDownload.Click += async (s, e) =>
                    {
                        dialog.Dismiss();

                        progressBar.Indeterminate = false;
                        progressBar.SetProgress(0, true);

                        await SongManager.DownloadSong(song.id, downloadProgress, default);
                        progressBar.SetProgress(0, true);

                        UpdateSongList();
                    };
                }
                else btnDownload.Visibility = ViewStates.Gone;

                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => dialog.Dismiss();
                view.FindViewById<Button>(Resource.Id.btnDelete).Click += (s, e) => ShowDeleteSongDialog(song, thumbnail, dialog);
            });
        }
        private void ShowDeleteSongDialog(Song song, Drawable thumbnail, Android.App.Dialog prevDialog)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_deleteSong, (dialog, view) =>
            {
                view.FindViewById<ImageView>(Resource.Id.imgThumbnail).SetImageDrawable(thumbnail);
                view.FindViewById<TextView>(Resource.Id.tvTitle).Text = song.title;
                view.FindViewById<TextView>(Resource.Id.tvArtist).Text = song.artist;
                view.FindViewById<TextView>(Resource.Id.tvDuration).Text = song.GetDurationString();

                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => dialog.Dismiss();
                view.FindViewById<Button>(Resource.Id.btnDelete).Click += (s, e) =>
                {
                    SongManager.DeleteSong(song.id);
                    UpdateSongList();

                    prevDialog.Dismiss();
                    dialog.Dismiss();
                };
            });
        }
        private void ShowDeletePlaylistDialog(Playlist playlist, Android.App.Dialog prevDialog)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_deletePlaylist, (dialog, view) =>
            {
                view.FindViewById<TextView>(Resource.Id.tvTitle).Text = playlist.title;
                view.FindViewById<TextView>(Resource.Id.tvCount).Text
                    = $"{playlist.songs.Length} song{(playlist.songs.Length == 1 ? "" : "s")}";

                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => dialog.Dismiss();
                view.FindViewById<Button>(Resource.Id.btnDelete).Click += (s, e) =>
                {
                    PlaylistManager.DeletePlaylist(playlist.title);
                    UpdatePlaylistList();

                    prevDialog.Dismiss();
                    dialog.Dismiss();
                };
            });
        }
        private void ShowNewPlaylistDialog()
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_newPlaylist, (dialog, view) =>
            {
                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => dialog.Dismiss();
                view.FindViewById<Button>(Resource.Id.btnCreate).Click += (s, e) =>
                {
                    string title = view.FindViewById<EditText>(Resource.Id.etPlaylistName).Text;

                    if (!string.IsNullOrEmpty(title) && !Directory.Exists($"{PlaylistManager.PlaylistDirectory}/{title}"))
                    {
                        PlaylistManager.CreatePlaylist(title);
                        UpdatePlaylistList();
                    }

                    dialog.Dismiss();
                };
            });
        }
        private void ShowRenamePlaylistDialog(Playlist playlist)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_renamePlaylist, (dialog, view) =>
            {
                view.FindViewById<TextView>(Resource.Id.tvTitle).Text = playlist.title;
                view.FindViewById<TextView>(Resource.Id.tvCount).Text
                    = $"{playlist.songs.Length} song{(playlist.songs.Length == 1 ? "" : "s")}";

                view.FindViewById<Button>(Resource.Id.btnRename).Click += (s, e) => dialog.Dismiss();
                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => dialog.Dismiss();
            });
        }
        private void UpdatePlaylistList()
        {
            playlistVA.items = PlaylistManager.GetPlaylists();
            playlistVA.NotifyDataSetChanged();
        }
        private void UpdateSongList()
        {
            songVA.items = PlaylistManager.GetSongsInPlaylist(currentPlaylist);
            songVA.NotifyDataSetChanged();
        }
    }
}