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
            ViewAdapter<Song> viewAdapter = new ViewAdapter<Song>(new List<Song>(), Resource.Layout.item_song, BindSongViewAdapter);

            rv.SetAdapter(viewAdapter);
            rv.SetLayoutManager(new LinearLayoutManager(view.Context));

            view.FindViewById<EditText>(Resource.Id.etSearchBar).SetOnEditorActionListener(new OnEditorActionListener((tv, aID, e) =>
            {
                if (aID != ImeAction.Go) return false;

                SearchQuery(tv.Text, view, viewAdapter);
                return true;
            }));

            view.FindViewById<Button>(Resource.Id.btnGo).Click += (s, e) => 
                SearchQuery(view.FindViewById<EditText>(Resource.Id.etSearchBar).Text, view, viewAdapter);

            progressBar = view.FindViewById<ProgressBar>(Resource.Id.lpiProgress);
            progressBar.Min = 0;
            progressBar.Max = 100;

            progressBar.Indeterminate = false;
            progressBar.SetProgress(0, true);

            downloadProgress = new Progress<double>(progress =>
                progressBar.SetProgress((int)Math.Round(progress * 100), true));
        }
        private async void SearchQuery(string query, View view, ViewAdapter<Song> viewAdapter)
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            InputMethodManager imm = (InputMethodManager)view.Context.GetSystemService(Context.InputMethodService);
            imm.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.None);

            if (VideoId.TryParse(query).HasValue)
            {
                await SongManager.DownloadSong(VideoId.Parse(query), downloadProgress, default);
                progressBar.SetProgress(0, true);
            }
            else
            {
                progressBar.Indeterminate = true;
                viewAdapter.items = await SongManager.SearchForSongs(query, default);

                progressBar.Indeterminate = false;
                progressBar.SetProgress(0, true);

                viewAdapter.NotifyDataSetChanged();
            }
        }
        private async void BindSongViewAdapter(RecyclerView.ViewHolder holder, Song song)
        {
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvTitle).Text = song.title;
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvArtist).Text = song.artist;
            holder.ItemView.FindViewById<TextView>(Resource.Id.tvDuration).Text = song.GetDurationString();

            Drawable thumbnail = await Drawable.CreateFromPathAsync($"{SongManager.ThumbnailCacheDirectory}/{song.id}.jpg");

            holder.ItemView.FindViewById<ImageView>(Resource.Id.imgThumbnail)
                .SetImageDrawable(thumbnail);

            holder.ItemView.FindViewById<FloatingActionButton>(Resource.Id.fabActions).SetOnClickListener(
                new OnClickListener(v => ShowSongDialog(song, thumbnail)));
        }
        private void ShowSongDialog(Song song, Drawable thumbnail)
        {
            MainActivity.ShowDialog(Resource.Layout.dialog_songs_song, (dialog, view) =>
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
                    };
                }
                else btnDownload.Visibility = ViewStates.Gone;

                view.FindViewById<Button>(Resource.Id.btnAddToPlaylist).Click += (s, e) =>
                {

                };

                view.FindViewById<Button>(Resource.Id.btnCancel).Click += (s, e) => dialog.Dismiss();
            });
        }
    }
}