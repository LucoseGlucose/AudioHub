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
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.BottomAppBar;
using Google.Android.Material.Slider;
using Android.Graphics.Drawables;
using YoutubeReExplode.Playlists;
using AndroidX.RecyclerView.Widget;
using System.Threading;
using YoutubeReExplode.Videos.Streams;
using YoutubeReExplode.Videos;
using System.IO;
using Android.Graphics;

namespace AudioHub
{
    public class ListenFragment : Fragment
    {
        private FloatingActionButton fabPlayPause;
        private FloatingActionButton fabPrev;
        private FloatingActionButton fabNext;
        private FloatingActionButton fabLoop;
        private FloatingActionButton fabShuffle;

        private SeekBar elapsedSlider;
        private TextView tvElapsedDuration;
        private TextView tvFullDuration;

        private TextView tvSongPlaylist;
        private ImageView imgSongThumbnail;
        private TextView tvSongTitle;
        private TextView tvSongArtist;

        private BottomAppBar babTopAppBar;

        private ProgressBar progressBar;
        private Progress<double> downloadProgress;

        private Handler songTimer;
        private float lastSliderVal;

        private Song currentSong;
        private readonly Dictionary<Playlist, bool> playlistSelections = new Dictionary<Playlist, bool>();
        private ViewAdapter<Playlist> selectPlaylistVA;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.fragment_listen, container, false);
        }
        public override async void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            fabPlayPause = view.FindViewById<FloatingActionButton>(Resource.Id.fabPlayPause);
            fabPrev = view.FindViewById<FloatingActionButton>(Resource.Id.fabPrev);
            fabNext = view.FindViewById<FloatingActionButton>(Resource.Id.fabNext);
            fabLoop = view.FindViewById<FloatingActionButton>(Resource.Id.fabLoop);
            fabShuffle = view.FindViewById<FloatingActionButton>(Resource.Id.fabShuffle);

            elapsedSlider = view.FindViewById<SeekBar>(Resource.Id.sSongProgress);
            elapsedSlider.Min = 0;
            elapsedSlider.Max = SongPlayer.currentSong.durationSecs;

            elapsedSlider.ProgressChanged += (s, e) =>
            {
                if (e.FromUser) SongPlayer.Seek(e.Progress);
                tvElapsedDuration.Text = Song.GetDurationString(e.Progress);
            };

            tvElapsedDuration = view.FindViewById<TextView>(Resource.Id.tvElapsedDuration);
            tvFullDuration = view.FindViewById<TextView>(Resource.Id.tvFullDuration);

            tvElapsedDuration.Text = Song.GetDurationString((int)Math.Floor((float)SongPlayer.mediaPlayer.CurrentPosition / 1000));
            tvFullDuration.Text = Song.GetDurationString(SongPlayer.currentSong.durationSecs);

            selectPlaylistVA = new ViewAdapter<Playlist>(PlaylistManager.GetPlaylists(),
                Resource.Layout.item_playlist_select, BindSelectPlaylistViewAdapter);

            babTopAppBar = view.FindViewById<BottomAppBar>(Resource.Id.babTopAppBar);
            babTopAppBar.SetNavigationOnClickListener(new OnClickListener(v => MainActivity.activity.FinishAndRemoveTask()));

            babTopAppBar.Menu.FindItem(Resource.Id.topappbar_readMessages).Icon.SetTint(Context.GetColor(MessageListener.readMessages ?
                        Resource.Color.m3_sys_color_dark_primary : Resource.Color.m3_sys_color_dark_on_surface));

            babTopAppBar.MenuItemClick += async (s, e) =>
            {
                if (e.Item.ItemId == Resource.Id.topappbar_regenTitles)
                {
                    SongPlayer.Pause(false);

                    foreach (string songId in PlaylistManager.GetDownloadedSongsPlaylist().songs)
                    {
                        Song song = SongManager.GetSongById(songId);
                        SongManager.WriteSongData(SongManager.GetSongDirectory(songId), SongManager.GetUpdatedSong(song));
                    }

                    SongPlayer.Resume();
                }
                if (e.Item.ItemId == Resource.Id.topappbar_readMessages)
                {
                    MessageListener.readMessages = !MessageListener.readMessages;
                    e.Item.Icon.SetTint(Context.GetColor(MessageListener.readMessages ?
                        Resource.Color.m3_sys_color_dark_primary : Resource.Color.m3_sys_color_dark_on_surface));
                }

                if (string.IsNullOrEmpty(SongPlayer.currentSong.id)) return;

                if (e.Item.ItemId == Resource.Id.topappbar_selectPlaylists)
                {
                    ShowSelectPlaylistsDialog(SongPlayer.currentSong, imgSongThumbnail.Drawable);
                }
                if (e.Item.ItemId == Resource.Id.topappbar_addToQueue)
                {
                    QueueManager.songs.AddLast(SongPlayer.currentSong);
                }
                if (e.Item.ItemId == Resource.Id.topappbar_download)
                {
                    await SongManager.DownloadCachedSong(SongPlayer.currentSong, downloadProgress, default);
                }
            };

            progressBar = view.FindViewById<ProgressBar>(Resource.Id.lpiProgress);
            downloadProgress = new Progress<double>(progress =>
                progressBar.SetProgress((int)Math.Round(progress * 100), true));

            songTimer = new Handler(Looper.MyLooper());

            tvSongPlaylist = view.FindViewById<TextView>(Resource.Id.tvPlaylist);
            imgSongThumbnail = view.FindViewById<ImageView>(Resource.Id.imgThumbnail);
            tvSongTitle = view.FindViewById<TextView>(Resource.Id.tvTitle);
            tvSongArtist = view.FindViewById<TextView>(Resource.Id.tvArtist);

            imgSongThumbnail.SetImageDrawable(await Drawable.CreateFromPathAsync(
                SongManager.IsSongDownloaded(SongPlayer.currentSong.id) ?
                $"{SongManager.GetSongDirectory(SongPlayer.currentSong.id)}/Thumbnail.jpg"
                : $"{SongManager.SongCacheDirectory}/{SongPlayer.currentSong.id}/Thumbnail.jpg"));

            tvSongPlaylist.Text = SongPlayer.currentPlaylist.title;
            tvSongTitle.Text = SongPlayer.currentSong.title;
            tvSongArtist.Text = SongPlayer.currentSong.artist;

            fabPlayPause.SetImageDrawable(MainActivity.activity.GetDrawable(
                SongPlayer.mediaPlayer.IsPlaying ? Resource.Drawable.round_pause_24 : Resource.Drawable.round_play_arrow_24));

            fabLoop.SetImageDrawable(MainActivity.activity.GetDrawable(
                SongPlayer.loop ? Resource.Drawable.round_replay_circle_filled_24 : Resource.Drawable.round_replay_24));

            fabShuffle.SetImageDrawable(MainActivity.activity.GetDrawable(
                SongPlayer.shuffle ? Resource.Drawable.round_shuffle_on_24 : Resource.Drawable.round_shuffle_24));

            SongPlayer.onPlay += async (song, playlist) =>
            {
                fabPlayPause.SetImageDrawable(MainActivity.activity.GetDrawable(Resource.Drawable.round_pause_24));

                tvElapsedDuration.Text = Song.GetDurationString(0);
                tvFullDuration.Text = Song.GetDurationString(song.durationSecs);
                elapsedSlider.Max = song.durationSecs;

                imgSongThumbnail.SetImageDrawable(await Drawable.CreateFromPathAsync(
                    SongManager.IsSongDownloaded(song.id) ? $"{SongManager.GetSongDirectory(song.id)}/Thumbnail.jpg"
                    : $"{SongManager.SongCacheDirectory}/{song.id}/Thumbnail.jpg"));

                tvSongPlaylist.Text = playlist.title;
                tvSongTitle.Text = song.title;
                tvSongArtist.Text = song.artist;

                songTimer.Post(TimerUpdate);
            };

            SongPlayer.onPause += () =>
            {
                fabPlayPause.SetImageDrawable(MainActivity.activity.GetDrawable(Resource.Drawable.round_play_arrow_24));
                songTimer.RemoveCallbacks(TimerUpdate);
            };

            SongPlayer.onResume += () =>
            {
                fabPlayPause.SetImageDrawable(MainActivity.activity.GetDrawable(Resource.Drawable.round_pause_24));
                songTimer.PostDelayed(TimerUpdate, 1000);
            };

            SongPlayer.onToggleLoop += (loop) =>
            {
                fabLoop.SetImageDrawable(MainActivity.activity.GetDrawable(
                    loop ? Resource.Drawable.round_replay_circle_filled_24 : Resource.Drawable.round_replay_24));
            };

            SongPlayer.onToggleShuffle += (shuffle) =>
            {
                fabShuffle.SetImageDrawable(MainActivity.activity.GetDrawable(
                    shuffle ? Resource.Drawable.round_shuffle_on_24 : Resource.Drawable.round_shuffle_24));
            };

            fabPlayPause.Click += (s, e) =>
            {
                if (SongPlayer.mediaPlayer.IsPlaying) SongPlayer.Pause(true);
                else SongPlayer.Resume();
            };

            fabNext.Click += (s, e) => SongPlayer.PlayNextSong(true);
            fabPrev.Click += (s, e) => SongPlayer.PlayPreviousSong();

            fabLoop.Click += (s, e) => SongPlayer.ToggleLoop();
            fabShuffle.Click += (s, e) => SongPlayer.ToggleShuffle();
        }
        private void TimerUpdate()
        {
            if (SongPlayer.mediaPlayer == null || !SongPlayer.mediaPlayer.IsPlaying) return;

            int secs = (int)Math.Floor(SongPlayer.mediaPlayer.CurrentPosition / 1000f);
            elapsedSlider.Progress = secs;

            songTimer.PostDelayed(TimerUpdate, 1000);
        }
        private void ShowSelectPlaylistsDialog(Song song, Drawable thumbnail)
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
                    if (playlistSelections.Count < 1)
                    {
                        dialog.Dismiss();
                        return;
                    }

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