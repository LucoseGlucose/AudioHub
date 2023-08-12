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

namespace AudioHub
{
    public class ListenFragment : Fragment
    {
        private FloatingActionButton fabPlayPause;
        private FloatingActionButton fabPrev;
        private FloatingActionButton fabNext;
        private FloatingActionButton fabLoop;
        private FloatingActionButton fabShuffle;

        private BottomAppBar babTopAppBar;

        private ProgressBar progressBar;
        private Progress<double> downloadProgress;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.fragment_listen, container, false);
        }
        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            fabPlayPause = view.FindViewById<FloatingActionButton>(Resource.Id.fabPlayPause);
            fabPrev = view.FindViewById<FloatingActionButton>(Resource.Id.fabPrev);
            fabNext = view.FindViewById<FloatingActionButton>(Resource.Id.fabNext);
            fabLoop = view.FindViewById<FloatingActionButton>(Resource.Id.fabLoop);
            fabShuffle = view.FindViewById<FloatingActionButton>(Resource.Id.fabShuffle);

            babTopAppBar = view.FindViewById<BottomAppBar>(Resource.Id.babTopAppBar);
            babTopAppBar.SetNavigationOnClickListener(new OnClickListener(v =>
            {
                MainActivity.activity.FinishAndRemoveTask();
            }));

            progressBar = view.FindViewById<ProgressBar>(Resource.Id.lpiProgress);
            downloadProgress = new Progress<double>(progress =>
                progressBar.SetProgress((int)Math.Round(progress * 100), true));
        }
    }
}