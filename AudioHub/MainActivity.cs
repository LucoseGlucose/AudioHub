﻿using Android.App;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using Google.Android.Material.BottomNavigation;
using Xamarin.Essentials;
using System.Collections.Generic;
using Android.Content.PM;
using AndroidX.Fragment.App;
using Android.Media.Session;
using Android.Content;
using Android.Media;
using Google.Android.Material.Navigation;
using Android.Views;
using AndroidX.Core.App;
using Android;
using AndroidX.Activity;
using Android.Bluetooth;

namespace AudioHub
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : FragmentActivity
    {
        private readonly Dictionary<int, AndroidX.Fragment.App.Fragment> fragments = new Dictionary<int, AndroidX.Fragment.App.Fragment>
        {
            { Resource.Id.navigation_listen, new ListenFragment() },
            { Resource.Id.navigation_songs, new SongsFragment() },
            { Resource.Id.navigation_manage, new ManageFragment() },
        };

        private int currentPage;
        private BottomNavigationView bNavView;

        public static MainActivity activity;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            activity = this;

            base.OnCreate(savedInstanceState);
            Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            bNavView = FindViewById<BottomNavigationView>(Resource.Id.bnvNavigation);
            bNavView.ItemSelected += BNavView_ItemSelected;

            SwitchPage(Resource.Id.navigation_listen);

            await Permissions.RequestAsync<Permissions.StorageRead>();
            await Permissions.RequestAsync<Permissions.StorageWrite>();
        }
        private void BNavView_ItemSelected(object sender, NavigationBarView.ItemSelectedEventArgs e)
        {
            SwitchPage(e.Item.ItemId);
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
        public void SwitchPage(int newPage)
        {
            if (currentPage == newPage || string.IsNullOrEmpty(Resources.GetResourceName(newPage))) return;

            AndroidX.Fragment.App.FragmentTransaction ft = SupportFragmentManager.BeginTransaction();
            ft.Replace(Resource.Id.flFragment, fragments[newPage]);
            ft.SetTransition(AndroidX.Fragment.App.FragmentTransaction.TransitFragmentFade);
            ft.Commit();

            currentPage = newPage;
        }
    }
}

