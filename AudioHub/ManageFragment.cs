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
using Android.Views.InputMethods;
using Google.Android.Material.Search;
using YoutubeReExplode.Videos;
using System.Threading.Tasks;

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

            playlistVA = new ViewAdapter<Playlist>(Array.Empty<Playlist>(), Resource.Layout.item_playlist, BindPlaylistViewAdapter);

            RecyclerView rv = view.FindViewById<RecyclerView>(Resource.Id.rvList);
            rv.SetAdapter(playlistVA);
            rv.SetLayoutManager(new LinearLayoutManager(view.Context));

            view.FindViewById<Button>(Resource.Id.btnNewPlaylist).Click += (s, e) => ShowNewPlaylistDialog();

            progressBar = view.FindViewById<ProgressBar>(Resource.Id.lpiProgress);
            downloadProgress = new Progress<double>(progress =>
                progressBar.SetProgress((int)Math.Round(progress * 100), true));

            songVA = new ViewAdapter<Song>(Array.Empty<Song>(), Resource.Layout.item_song, BindSongViewAdapter);
            selectPlaylistVA = new ViewAdapter<Playlist>(Array.Empty<Playlist>(),
                Resource.Layout.item_playlist_select, BindSelectPlaylistViewAdapter);

            UpdatePlaylistList();

            EditText searchBar = view.FindViewById<EditText>(Resource.Id.etSearchBar);
            view.FindViewById<Button>(Resource.Id.btnGo).Click += (s, e) => PlaylistSearchQuery(searchBar.Text, view);

            searchBar.AfterTextChanged += (s, e) =>
            {
                if (!e.Editable.Any()) UpdatePlaylistList();
            };

            searchBar.SetOnEditorActionListener(new OnEditorActionListener((tv, aID, e) =>
            {
                if (aID != ImeAction.Go) return false;

                PlaylistSearchQuery(tv.Text, view);
                return true;
            }));

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
        private void PlaylistSearchQuery(string query, View view)
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            InputMethodManager imm = (InputMethodManager)view.Context.GetSystemService(Context.InputMethodService);
            imm.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.None);

            List<Playlist> playlistList = GetAllPllaylists();
            List<Playlist> matchingPlaylists = new List<Playlist>();

            for (int i = 0; i < playlistList.Count; i++)
            {
                if (playlistList[i].title.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                {
                    matchingPlaylists.Add(playlistList[i]);
                }
            }

            playlistVA.items = matchingPlaylists;
            playlistVA.NotifyDataSetChanged();
        }
        private void SongSearchQuery(string query, View view)
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            InputMethodManager imm = (InputMethodManager)view.Context.GetSystemService(Context.InputMethodService);
            imm.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.None);

            Song[] songList = PlaylistManager.GetSongsInPlaylist(currentPlaylist.title);
            List<Song> matchingSongs = new List<Song>();

            for (int i = 0; i < songList.Length; i++)
            {
                if (songList[i].title.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                    || songList[i].artist.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                {
                    matchingSongs.Add(songList[i]);
                }
            }

            songVA.items = matchingSongs;
            songVA.NotifyDataSetChanged();
        }
        private void BindPlaylistViewAdapter(RecyclerView.ViewHolder holder, Playlist playlist)
        {
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvTitle).Text = playlist.title;

            songCountTexts.Add(holder.ItemView.FindViewById<TextView>(Resource.Id.tvCount));
            songCountTexts.Last().Text = GetSongCountText(PlaylistManager.GetSongIDsInPlaylist(playlist.title)?.Length);

            FloatingActionButton fab = holder.ItemView.FindViewById<FloatingActionButton>(Resource.Id.fabActions);

            if (playlist.title == PlaylistManager.downloadedPlaylistName)
            {
                fab.SetImageDrawable(Context.GetDrawable(Resource.Drawable.round_download_24));
                fab.Click += (s, e) => ShowViewPlaylistDialog(PlaylistManager.GetDownloadedSongsPlaylist());
            }
            else if (playlist.title == PlaylistManager.queuePlaylistName)
            {
                fab.SetImageDrawable(Context.GetDrawable(Resource.Drawable.round_queue_24));
                fab.Click += (s, e) => ShowViewPlaylistDialog(QueueManager.GetQueuePlaylist());
            }
            else if (playlist.title == PlaylistManager.tempPlaylistName)
            {
                fab.SetImageDrawable(Context.GetDrawable(Resource.Drawable.round_delete_24));
                fab.Click += (s, e) => ShowViewPlaylistDialog(PlaylistManager.GetTemporarySongsPlaylist());
            }
            else fab.SetOnClickListener(new OnClickListener(v => ShowPlaylistDialog(playlist)));
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

                EditText searchBar = view.FindViewById<EditText>(Resource.Id.etSearchBar);
                view.FindViewById<Button>(Resource.Id.btnGo).Click += (s, e) => SongSearchQuery(searchBar.Text, view);

                searchBar.AfterTextChanged += (s, e) =>
                {
                    if (!e.Editable.Any()) UpdateSongList();
                };

                searchBar.SetOnEditorActionListener(new OnEditorActionListener((tv, aID, e) =>
                {
                    if (aID != ImeAction.Go) return false;

                    SongSearchQuery(tv.Text, view);
                    return true;
                }));

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

            string dir = SongManager.IsSongDownloaded(song.id) ? SongManager.SongDownloadDirectory : SongManager.SongCacheDirectory;
            Drawable thumbnail = await Drawable.CreateFromPathAsync($"{(dir)}/{song.id}/Thumbnail.jpg");

            holder.ItemView.FindViewById<ImageView>(Resource.Id.imgThumbnail)
                .SetImageDrawable(thumbnail);

            holder.ItemView.FindViewById<FloatingActionButton>(Resource.Id.fabActions)
                .SetOnClickListener(new OnClickListener(v => ShowSongDialog(song, thumbnail)));
        }
        private async Task DownloadSong(Song song)
        {
            progressBar.Indeterminate = false;
            progressBar.SetProgress(0, true);

            await SongManager.DownloadSong(song.id, downloadProgress, default);
            progressBar.SetProgress(100, true);

            UpdateSongList();
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
                Button btnDownloadAndPlay = view.FindViewById<Button>(Resource.Id.btnDownloadAndPlay);

                if (!SongManager.IsSongDownloaded(song.id))
                {
                    btnDownload.Click += async (s, e) =>
                    {
                        DismissDialog();
                        await DownloadSong(song);
                    };

                    btnDownloadAndPlay.Click += async (s, e) =>
                    {
                        DismissDialog();
                        await DownloadSong(song);

                        SongPlayer.Play(song, PlaylistManager.GetDownloadedSongsPlaylist());

                        DismissDialog();
                        DismissDialog();
                        DismissDialog();

                        MainActivity.activity.SwitchPage(Resource.Id.navigation_listen);
                    };
                }
                else
                {
                    btnDownload.Visibility = ViewStates.Gone;
                    btnDownloadAndPlay.Visibility = ViewStates.Gone;
                }

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
        private List<Playlist> GetAllPllaylists()
        {
            List<Playlist> playlists = new List<Playlist>() { PlaylistManager.GetDownloadedSongsPlaylist(),
                QueueManager.GetQueuePlaylist(), PlaylistManager.GetTemporarySongsPlaylist() };
            playlists.AddRange(PlaylistManager.GetPlaylists());

            return playlists;
        }
        private void UpdatePlaylistList()
        {
            playlistVA.items = GetAllPllaylists();
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