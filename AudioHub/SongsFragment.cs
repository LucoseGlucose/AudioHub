﻿using AndroidX.Fragment.App;
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
using Google.Android.Material.FloatingActionButton;
using Android.Graphics.Drawables;
using System.Threading.Tasks;
using AndroidX.ConstraintLayout.Widget;
using YoutubeReExplode.Videos;
using Android.Views.InputMethods;

namespace AudioHub
{
    public class SongsFragment : Fragment
    {
        private ProgressBar progressBar;
        private Progress<double> downloadProgress;

        private Song currentSong;
        private readonly Dictionary<Playlist, bool> playlistSelections = new Dictionary<Playlist, bool>();
        private ViewAdapter<Playlist> selectPlaylistVA;

        private List<Song> songs = new List<Song>();
        private string link;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.fragment_songs, container, false);
        }
        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            RecyclerView rv = view.FindViewById<RecyclerView>(Resource.Id.rvList);
            ViewAdapter<Song> viewAdapter = new ViewAdapter<Song>(songs, Resource.Layout.item_song, BindSongViewAdapter);

            rv.SetAdapter(viewAdapter);
            rv.SetLayoutManager(new LinearLayoutManager(view.Context));

            EditText etSearchBar = view.FindViewById<EditText>(Resource.Id.etSearchBar);
            view.FindViewById<Button>(Resource.Id.btnGo).Click += (s, e) => SearchQuery(etSearchBar.Text, view, viewAdapter);
            etSearchBar.Text = SongManager.lastSearchQuery;

            etSearchBar.SetOnEditorActionListener(new OnEditorActionListener((tv, aID, e) =>
            {
                if (aID != ImeAction.Go) return false;

                SearchQuery(tv.Text, view, viewAdapter);
                return true;
            }));

            selectPlaylistVA = new ViewAdapter<Playlist>(PlaylistManager.GetPlaylists(),
                Resource.Layout.item_playlist_select, BindSelectPlaylistViewAdapter);

            progressBar = view.FindViewById<ProgressBar>(Resource.Id.lpiProgress);
            downloadProgress = new Progress<double>(progress =>
                progressBar.SetProgress((int)Math.Round(progress * 100), true));

            SearchLink();
        }
        public void SetLink(string link)
        {
            this.link = link;
            if (View != null) SearchLink();
        }
        private void SearchLink()
        {
            if (link == null) return;

            View.FindViewById<EditText>(Resource.Id.etSearchBar).Text = link;
            View.FindViewById<Button>(Resource.Id.btnGo).PerformClick();

            link = null;
        }
        private async void SearchQuery(string query, View view, ViewAdapter<Song> viewAdapter)
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            InputMethodManager imm = (InputMethodManager)view.Context.GetSystemService(Context.InputMethodService);
            imm.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.None);

            if (VideoId.TryParse(query).HasValue)
            {
                progressBar.Indeterminate = true;

                viewAdapter.items = new List<Song>() { await SongManager.GetSongFromVideo(VideoId.Parse(query)) };
                songs = viewAdapter.items as List<Song>;
                await SongManager.CacheThumbnail(songs[0]);

                progressBar.Indeterminate = false;
                progressBar.SetProgress(100, true);

                viewAdapter.NotifyDataSetChanged();
            }
            else
            {
                progressBar.Indeterminate = true;
                viewAdapter.items = await SongManager.SearchForSongs(query, default);
                songs = viewAdapter.items as List<Song>;

                progressBar.Indeterminate = false;
                progressBar.SetProgress(100, true);

                viewAdapter.NotifyDataSetChanged();
            }

            view.FindViewById<RecyclerView>(Resource.Id.rvList).ScrollToPosition(0);
        }
        private async void BindSongViewAdapter(RecyclerView.ViewHolder holder, Song song)
        {
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvTitle).Text = song.fullTitle;
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvArtist).Text = song.fullArtist;
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvDuration).Text = song.GetDurationString();

            Drawable thumbnail = await Drawable.CreateFromPathAsync($"{SongManager.ThumbnailCacheDirectory}/{song.id}.jpg");

            holder.ItemView.FindViewById<ImageView>(Resource.Id.imgThumbnail)
                .SetImageDrawable(thumbnail);

            holder.ItemView.FindViewById<FloatingActionButton>(Resource.Id.fabActions).SetOnClickListener(
                new OnClickListener(v => ShowSongDialog(song, thumbnail)));
        }
        private async Task DownloadSong(Android.App.Dialog dialog, Song song)
        {
            dialog.Dismiss();

            progressBar.Indeterminate = false;
            progressBar.SetProgress(0, true);

            await SongManager.DownloadSong(song.id, downloadProgress, default);
        }
        private void ShowSongDialog(Song song, Drawable thumbnail)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_songs_song, null, (dialog, view) =>
            {
                view.FindViewById<ImageView>(Resource.Id.imgThumbnail).SetImageDrawable(thumbnail);
                view.FindViewById<TextView>(Resource.Id.tvTitle).Text = song.title;
                view.FindViewById<TextView>(Resource.Id.tvArtist).Text = song.artist;
                view.FindViewById<TextView>(Resource.Id.tvDuration).Text = song.GetDurationString();

                Button btnDownload = view.FindViewById<Button>(Resource.Id.btnDownload);
                Button btnDownloadAndPlay = view.FindViewById<Button>(Resource.Id.btnDownloadAndPlay);
                Button btnDownloadAndAddToQueue = view.FindViewById<Button>(Resource.Id.btnDownloadAndAddToQueue);

                if (!SongManager.IsSongDownloaded(song.id))
                {
                    btnDownload.Click += async (s, e) => await DownloadSong(dialog, song);

                    btnDownloadAndPlay.Click += async (s, e) =>
                    {
                        await DownloadSong(dialog, song);

                        MainActivity.activity.SwitchPage(Resource.Id.navigation_listen);
                        SongPlayer.Play(song, PlaylistManager.GetDownloadedSongsPlaylist());
                    };

                    btnDownloadAndAddToQueue.Click += async (s, e) =>
                    {
                        await DownloadSong(dialog, song);
                        QueueManager.songs.AddLast(song);
                    };
                }
                else
                {
                    btnDownload.Visibility = ViewStates.Gone;
                    btnDownloadAndPlay.Visibility = ViewStates.Gone;
                    btnDownloadAndAddToQueue.Visibility = ViewStates.Gone;
                }

                view.FindViewById<Button>(Resource.Id.btnPlay).Click += async (s, e) =>
                {
                    dialog.Dismiss();
                    dialog.Dispose();

                    if (!SongManager.IsSongDownloaded(song.id)) await SongManager.CacheSong(song.id, downloadProgress, default);

                    MainActivity.activity.SwitchPage(Resource.Id.navigation_listen);

                    SongPlayer.Play(song, SongManager.IsSongDownloaded(song.id) ?
                        PlaylistManager.GetDownloadedSongsPlaylist() : PlaylistManager.GetTemporarySongsPlaylist());
                };

                view.FindViewById<Button>(Resource.Id.btnAddToQueue).Click += async (s, e) =>
                {
                    dialog.Dismiss();
                    dialog.Dispose();

                    if (!SongManager.IsSongDownloaded(song.id)) await SongManager.CacheSong(song.id, downloadProgress, default);
                    QueueManager.songs.AddLast(song);
                };

                view.FindViewById<Button>(Resource.Id.btnSelectPlaylists).Click += (s, e) =>
                    ShowSelectPlaylistsDialog(song, thumbnail, dialog);

                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => dialog.Dismiss();

                view.FindViewById<Button>(Resource.Id.btnExport).Click += async (s, e) =>
                {
                    dialog.Dismiss();
                    await SongManager.ExportSong(song.id, downloadProgress, default);
                };
            });
        }
        private void ShowSelectPlaylistsDialog(Song song, Drawable thumbnail, Android.App.Dialog prevDialog)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_selectPlaylists, null, (dialog, view) =>
            {
                view.FindViewById<ImageView>(Resource.Id.imgThumbnail).SetImageDrawable(thumbnail);
                view.FindViewById<TextView>(Resource.Id.tvTitle).Text = song.title;
                view.FindViewById<TextView>(Resource.Id.tvArtist).Text = song.artist;
                view.FindViewById<TextView>(Resource.Id.tvDuration).Text = song.GetDurationString();

                RecyclerView rv = view.FindViewById<RecyclerView>(Resource.Id.rvPlaylistSelection);
                rv.SetAdapter(selectPlaylistVA);
                rv.SetLayoutManager(new LinearLayoutManager(view.Context));

                view.FindViewById<Button>(Resource.Id.btnConfirm).Click += async (s, e) =>
                {
                    foreach (KeyValuePair<Playlist, bool> playslistSelection in playlistSelections)
                    {
                        if (SongManager.IsSongDownloaded(song.id))
                        {
                            bool inPlaylist = PlaylistManager.IsSongInPlaylist(playslistSelection.Key.title, song.id);

                            if (inPlaylist != playslistSelection.Value)
                            {
                                if (playslistSelection.Value) PlaylistManager.AddSongToPlaylist(playslistSelection.Key.title, song.id);
                                else PlaylistManager.RemoveSongFromPlaylist(playslistSelection.Key.title, song.id);
                            }

                            dialog.Dismiss();
                        }
                        else if (playslistSelection.Value)
                        {
                            prevDialog.Dismiss();
                            dialog.Dismiss();

                            progressBar.Indeterminate = false;
                            progressBar.SetProgress(0, true);

                            await SongManager.DownloadSong(song.id, downloadProgress, default);
                            PlaylistManager.AddSongToPlaylist(playslistSelection.Key.title, song.id);
                        }
                    }
                };
                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => dialog.Dismiss();

                currentSong = song;
                playlistSelections.Clear();
            });
        }
        private void BindSelectPlaylistViewAdapter(RecyclerView.ViewHolder holder, Playlist playlist)
        {
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvTitle).Text = playlist.title;
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvCount).Text
                = ManageFragment.GetSongCountText(PlaylistManager.GetSongIDsInPlaylist(playlist.title)?.Length);

            bool inPlaylist = PlaylistManager.IsSongInPlaylist(playlist.title, currentSong.id);
            playlistSelections.Add(playlist, inPlaylist);

            CheckBox checkBox = holder.ItemView.FindViewById<CheckBox>(Resource.Id.cbSelect);
            checkBox.Checked = inPlaylist;

            checkBox.CheckedChange += (s, e) => playlistSelections[playlist] = e.IsChecked;
        }
    }
}