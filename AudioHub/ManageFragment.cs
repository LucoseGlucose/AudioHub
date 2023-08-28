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
using Google.Android.Material.FloatingActionButton;
using Android.Graphics.Drawables;
using AndroidX.ConstraintLayout.Widget;
using System.IO;

namespace AudioHub
{
    public class ManageFragment : Fragment
    {
        private ViewAdapter<Playlist> playlistVA;
        private ViewAdapter<Song> songVA;
        private ViewAdapter<Playlist> selectPlaylistVA;

        private Playlist currentPlaylist;
        private readonly List<TextView> songCountTexts = new List<TextView>();

        private ProgressBar progressBar;
        private Progress<double> downloadProgress;

        private Song currentSong;
        private readonly Dictionary<Playlist, bool> playlistSelections = new Dictionary<Playlist, bool>();

        private readonly Stack<Android.App.Dialog> dialogStack = new Stack<Android.App.Dialog>();

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

            songCountTexts.Add(downloadedPlaylist.FindViewById<TextView>(Resource.Id.tvCount));
            songCountTexts.Last().Text = GetSongCountText(PlaylistManager.GetSongIDsInPlaylist(downloadedSongs.title)?.Length);

            FloatingActionButton fabDownloadedActions = downloadedPlaylist.FindViewById<FloatingActionButton>(Resource.Id.fabActions);
            fabDownloadedActions.SetImageDrawable(Context.GetDrawable(Resource.Drawable.round_download_24));
            fabDownloadedActions.Click += (s, e) => ShowViewPlaylistDialog(downloadedSongs);

            View queuePlaylist = view.FindViewById<View>(Resource.Id.iQueuePlaylist);
            queuePlaylist.FindViewById<TextView>(Resource.Id.tvTitle).Text = PlaylistManager.queuePlaylistName;

            songCountTexts.Add(queuePlaylist.FindViewById<TextView>(Resource.Id.tvCount));
            songCountTexts.Last().Text = GetSongCountText(PlaylistManager.GetSongIDsInPlaylist(PlaylistManager.queuePlaylistName)?.Length);

            FloatingActionButton fabQueueActions = queuePlaylist.FindViewById<FloatingActionButton>(Resource.Id.fabActions);
            fabQueueActions.SetImageDrawable(Context.GetDrawable(Resource.Drawable.round_queue_24));
            fabQueueActions.Click += (s, e) => ShowViewPlaylistDialog(QueueManager.GetQueuePlaylist());

            progressBar = view.FindViewById<ProgressBar>(Resource.Id.lpiProgress);
            downloadProgress = new Progress<double>(progress =>
                progressBar.SetProgress((int)Math.Round(progress * 100), true));

            songVA = new ViewAdapter<Song>(Array.Empty<Song>(), Resource.Layout.item_song, BindSongViewAdapter);
            selectPlaylistVA = new ViewAdapter<Playlist>(playlists, Resource.Layout.item_playlist_select, BindSelectPlaylistViewAdapter);

            EditText searchBar = view.FindViewById<EditText>(Resource.Id.etSearchBar);
            searchBar.AfterTextChanged += (s, e) =>
            {
                List<char> chars = new List<char>();
                foreach (char c in e.Editable)
                {
                    chars.Add(c);
                }

                string str = new string(chars.ToArray());
                Playlist[] playlistList = PlaylistManager.GetPlaylists();

                for (int i = 0; i < playlistList.Length; i++)
                {
                    if (playlistList[i].title.Contains(str, StringComparison.CurrentCultureIgnoreCase))
                    {
                        rv.ScrollToPosition(i);
                        break;
                    }
                }
            };
        }
        public static string GetSongCountText(int? count)
        {
            return count == 1 ? "1 Song" : $"{count} Songs";
        }
        public void DismissDialog()
        {
            if (!dialogStack.TryPeek(out Android.App.Dialog dialog)) return;
            dialogStack.Pop();

            dialog.Dismiss();
            dialog.Dispose();
        }
        private void BindPlaylistViewAdapter(RecyclerView.ViewHolder holder, Playlist playlist)
        {
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvTitle).Text = playlist.title;

            songCountTexts.Add(holder.ItemView.FindViewById<TextView>(Resource.Id.tvCount));
            songCountTexts.Last().Text = GetSongCountText(PlaylistManager.GetSongIDsInPlaylist(playlist.title)?.Length);

            holder.ItemView.FindViewById<FloatingActionButton>(Resource.Id.fabActions)
                .SetOnClickListener(new OnClickListener(v => ShowPlaylistDialog(playlist)));
        }
        private void ShowPlaylistDialog(Playlist playlist)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_playlists, dialogStack, (dialog, view) =>
            {
                view.FindViewById<TextView>(Resource.Id.tvTitle).Text = playlist.title;

                songCountTexts.Add(view.FindViewById<TextView>(Resource.Id.tvCount));
                songCountTexts.Last().Text = GetSongCountText(PlaylistManager.GetSongIDsInPlaylist(playlist.title)?.Length);

                view.FindViewById<Button>(Resource.Id.btnView).Click += (s, e) =>
                {
                    DismissDialog();
                    ShowViewPlaylistDialog(playlist);
                };

                view.FindViewById<Button>(Resource.Id.btnDelete).Click += (s, e) => ShowDeletePlaylistDialog(playlist);
                view.FindViewById<Button>(Resource.Id.btnRename).Click += (s, e) => ShowRenamePlaylistDialog(playlist);
                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => DismissDialog();
            });
        }
        private void ShowViewPlaylistDialog(Playlist playlist)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_viewPlaylist, dialogStack, (dialog, view) =>
            {
                view.FindViewById<TextView>(Resource.Id.tvTitle).Text = playlist.title;
                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => DismissDialog();

                songCountTexts.Add(view.FindViewById<TextView>(Resource.Id.tvCount));
                songCountTexts.Last().Text = GetSongCountText(PlaylistManager.GetSongIDsInPlaylist(playlist.title)?.Length);

                RecyclerView rv = view.FindViewById<RecyclerView>(Resource.Id.rvPlaylistList);
                rv.SetAdapter(songVA);
                rv.SetLayoutManager(new LinearLayoutManager(view.Context));

                songVA.items = PlaylistManager.GetSongsInPlaylist(playlist.title);
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
                .SetOnClickListener(new OnClickListener(v => ShowSongDialog(song, thumbnail)));
        }
        private void ShowSongDialog(Song song, Drawable thumbnail)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_manage_song, dialogStack, (dialog, view) =>
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
                        DismissDialog();

                        progressBar.Indeterminate = false;
                        progressBar.SetProgress(0, true);

                        await SongManager.DownloadSong(song.id, downloadProgress, default);
                        progressBar.SetProgress(100, true);

                        UpdateSongList();
                    };
                }
                else btnDownload.Visibility = ViewStates.Gone;

                Button btnRemoveFromQueue = view.FindViewById<Button>(Resource.Id.btnRemoveFromQueue);

                if (QueueManager.songs.Contains(song))
                {
                    btnRemoveFromQueue.Click += (s, e) =>
                    {
                        QueueManager.songs.Remove(song);
                        UpdateSongList();
                        DismissDialog();
                    };
                }
                else btnRemoveFromQueue.Visibility = ViewStates.Gone;

                view.FindViewById<Button>(Resource.Id.btnAddToQueue).Click += (s, e) =>
                {
                    QueueManager.songs.AddLast(song);
                    UpdateSongList();
                    DismissDialog();
                };

                view.FindViewById<Button>(Resource.Id.btnSelectPlaylists).Click += (s, e) => ShowSelectPlaylistsDialog(song, thumbnail);
                view.FindViewById<Button>(Resource.Id.btnPlay).Click += (s, e) =>
                {
                    SongPlayer.Play(song, currentPlaylist);

                    DismissDialog();
                    DismissDialog();
                    DismissDialog();

                    MainActivity.activity.SwitchPage(Resource.Id.navigation_listen);
                };

                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => DismissDialog();
                view.FindViewById<Button>(Resource.Id.btnDelete).Click += (s, e) => ShowDeleteSongDialog(song, thumbnail);
            });
        }
        private void ShowDeleteSongDialog(Song song, Drawable thumbnail)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_deleteSong, dialogStack, (dialog, view) =>
            {
                view.FindViewById<ImageView>(Resource.Id.imgThumbnail).SetImageDrawable(thumbnail);
                view.FindViewById<TextView>(Resource.Id.tvTitle).Text = song.title;
                view.FindViewById<TextView>(Resource.Id.tvArtist).Text = song.artist;
                view.FindViewById<TextView>(Resource.Id.tvDuration).Text = song.GetDurationString();

                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => DismissDialog();
                view.FindViewById<Button>(Resource.Id.btnDelete).Click += (s, e) =>
                {
                    SongManager.DeleteSong(song.id);
                    UpdateSongList();

                    DismissDialog();
                    DismissDialog();
                };
            });
        }
        private void ShowDeletePlaylistDialog(Playlist playlist)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_deletePlaylist, dialogStack, (dialog, view) =>
            {
                view.FindViewById<TextView>(Resource.Id.tvTitle).Text = playlist.title;
                view.FindViewById<TextView>(Resource.Id.tvCount).Text
                    = $"{playlist.songs.Length} song{(playlist.songs.Length == 1 ? "" : "s")}";

                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => DismissDialog();
                view.FindViewById<Button>(Resource.Id.btnDelete).Click += (s, e) =>
                {
                    PlaylistManager.DeletePlaylist(playlist.title);
                    songCountTexts.Clear();

                    UpdateSongList();
                    UpdatePlaylistList();

                    DismissDialog();
                    DismissDialog();
                };
            });
        }
        private void ShowNewPlaylistDialog()
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_newPlaylist, dialogStack, (dialog, view) =>
            {
                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => DismissDialog();
                view.FindViewById<Button>(Resource.Id.btnCreate).Click += (s, e) =>
                {
                    string title = view.FindViewById<EditText>(Resource.Id.etPlaylistName).Text;

                    if (!string.IsNullOrEmpty(title) && !Directory.Exists($"{PlaylistManager.PlaylistDirectory}/{title}"))
                    {
                        PlaylistManager.CreatePlaylist(title);
                        UpdatePlaylistList();
                    }

                    DismissDialog();
                };
            });
        }
        private void ShowRenamePlaylistDialog(Playlist playlist)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_renamePlaylist, dialogStack, (dialog, view) =>
            {
                view.FindViewById<TextView>(Resource.Id.tvTitle).Text = playlist.title;
                view.FindViewById<TextView>(Resource.Id.tvCount).Text
                    = $"{playlist.songs.Length} song{(playlist.songs.Length == 1 ? "" : "s")}";

                view.FindViewById<Button>(Resource.Id.btnRename).Click += (s, e) =>
                {
                    DismissDialog();
                    DismissDialog();
                };
                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => DismissDialog();
            });
        }
        private void UpdatePlaylistList()
        {
            playlistVA.items = PlaylistManager.GetPlaylists();
            playlistVA.NotifyDataSetChanged();

            selectPlaylistVA.items = PlaylistManager.GetPlaylists();
            selectPlaylistVA.NotifyDataSetChanged();
        }
        private void UpdateSongList()
        {
            songVA.items = PlaylistManager.GetSongsInPlaylist(currentPlaylist.title);
            songVA.NotifyDataSetChanged();

            for (int i = 0; i < songCountTexts.Count; i++)
            {
                songCountTexts[i].Text = GetSongCountText(songVA.items.Count());
            }
        }
        private void ShowSelectPlaylistsDialog(Song song, Drawable thumbnail)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_selectPlaylists, dialogStack, (dialog, view) =>
            {
                view.FindViewById<ImageView>(Resource.Id.imgThumbnail).SetImageDrawable(thumbnail);
                view.FindViewById<TextView>(Resource.Id.tvTitle).Text = song.title;
                view.FindViewById<TextView>(Resource.Id.tvArtist).Text = song.artist;
                view.FindViewById<TextView>(Resource.Id.tvDuration).Text = song.GetDurationString();

                RecyclerView rv = view.FindViewById<RecyclerView>(Resource.Id.rvPlaylistSelection);
                rv.SetAdapter(selectPlaylistVA);
                rv.SetLayoutManager(new LinearLayoutManager(view.Context));

                view.FindViewById<Button>(Resource.Id.btnConfirm).Click += (s, e) =>
                {
                    foreach (KeyValuePair<Playlist, bool> playslistSelection in playlistSelections)
                    {
                        bool inPlaylist = PlaylistManager.IsSongInPlaylist(playslistSelection.Key.title, song.id);

                        if (inPlaylist != playslistSelection.Value)
                        {
                            if (playslistSelection.Value) PlaylistManager.AddSongToPlaylist(playslistSelection.Key.title, song.id);
                            else PlaylistManager.RemoveSongFromPlaylist(playslistSelection.Key.title, song.id);

                            UpdateSongList();
                            UpdatePlaylistList();
                        }
                    }

                    DismissDialog();
                };
                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => DismissDialog();

                currentSong = song;
                playlistSelections.Clear();
            });
        }
        private void BindSelectPlaylistViewAdapter(RecyclerView.ViewHolder holder, Playlist playlist)
        {
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvTitle).Text = playlist.title;

            songCountTexts.Add(holder.ItemView.FindViewById<TextView>(Resource.Id.tvCount));
            songCountTexts.Last().Text = GetSongCountText(PlaylistManager.GetSongIDsInPlaylist(playlist.title)?.Length);

            bool inPlaylist = PlaylistManager.IsSongInPlaylist(playlist.title, currentSong.id);
            playlistSelections.Add(playlist, inPlaylist);

            CheckBox checkBox = holder.ItemView.FindViewById<CheckBox>(Resource.Id.cbSelect);
            checkBox.Checked = inPlaylist;

            checkBox.CheckedChange += (s, e) => playlistSelections[playlist] = e.IsChecked;
        }
    }
}