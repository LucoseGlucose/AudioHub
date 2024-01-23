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

namespace AudioHub
{
    public class ListenFragment : Fragment
    {
        private FloatingActionButton fabPlayPause;
        private FloatingActionButton fabPrev;
        private FloatingActionButton fabNext;
        private FloatingActionButton fabLoop;
        private FloatingActionButton fabShuffle;

        private Slider elapsedSlider;
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

            elapsedSlider = view.FindViewById<Slider>(Resource.Id.sSongProgress);
            elapsedSlider.SetLabelFormatter(new SongDurationFormatter());
            lastSliderVal = elapsedSlider.Value;

            tvElapsedDuration = view.FindViewById<TextView>(Resource.Id.tvElapsedDuration);
            tvFullDuration = view.FindViewById<TextView>(Resource.Id.tvFullDuration);

            tvElapsedDuration.Text = Song.GetDurationString((int)Math.Floor((float)SongPlayer.mediaPlayer.CurrentPosition / 1000));
            tvFullDuration.Text = Song.GetDurationString(SongPlayer.currentSong.durationSecs);

            babTopAppBar = view.FindViewById<BottomAppBar>(Resource.Id.babTopAppBar);
            babTopAppBar.SetNavigationOnClickListener(new OnClickListener(v => MainActivity.activity.FinishAndRemoveTask()));

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

            fabPlayPause.Click += (s, e) =>
            {
                if (SongPlayer.mediaPlayer.IsPlaying) SongPlayer.Pause();
                else SongPlayer.Resume();
            };

            fabNext.Click += (s, e) => SongPlayer.PlayNextSong();
            fabPrev.Click += (s, e) => SongPlayer.PlayPreviousSong();

            fabLoop.Click += (s, e) =>
            {
                SongPlayer.loop = !SongPlayer.loop;
                fabLoop.SetImageDrawable(MainActivity.activity.GetDrawable(
                    SongPlayer.loop ? Resource.Drawable.round_replay_circle_filled_24 : Resource.Drawable.round_replay_24));
            };

            fabShuffle.Click += (s, e) =>
            {
                SongPlayer.ToggleShuffle();
                fabShuffle.SetImageDrawable(MainActivity.activity.GetDrawable(
                    SongPlayer.shuffle ? Resource.Drawable.round_shuffle_on_24 : Resource.Drawable.round_shuffle_24));
            };
        }
        private void TimerUpdate()
        {
            if (SongPlayer.mediaPlayer == null) return;

            int secs = (int)Math.Floor((float)SongPlayer.mediaPlayer.CurrentPosition / 1000);
            tvElapsedDuration.Text = Song.GetDurationString(secs);

            if (elapsedSlider.Value != lastSliderVal && lastSliderVal != 0f)
            {
                lastSliderVal = elapsedSlider.Value;
                SongPlayer.Seek((int)Math.Floor(elapsedSlider.Value * SongPlayer.currentSong.durationSecs));
            }
            else
            {
                elapsedSlider.Value = (float)secs / SongPlayer.currentSong.durationSecs;
                lastSliderVal = elapsedSlider.Value;
            }

            songTimer.PostDelayed(TimerUpdate, 1000);
        }
    }
}