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
        FloatingActionButton fabPlayPause;
        FloatingActionButton fabPrev;
        FloatingActionButton fabNext;
        FloatingActionButton fabLoop;
        FloatingActionButton fabShuffle;

        BottomAppBar babTopAppBar;

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
        }
    }
}