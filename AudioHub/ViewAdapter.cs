using Android;
using AndroidX.AppCompat.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
using AndroidX.Core.App;
using AndroidX.RecyclerView.Widget;
using Java.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Google.Android.Material.FloatingActionButton;
using Android.App;

namespace AudioHub
{
    public class ViewAdapter<T> : RecyclerView.Adapter
    {
        public class ViewHolder : RecyclerView.ViewHolder
        {
            public ViewHolder(View itemView) : base(itemView)
            {

            }
        }

        public IEnumerable<T> items;
        private readonly Action<RecyclerView.ViewHolder, T> bindAction;
        private readonly int layoutResID;

        public override int ItemCount => items.Count();

        public ViewAdapter(IEnumerable<T> items, int layoutResID, Action<RecyclerView.ViewHolder, T> bindAction)
        {
            this.items = items;
            this.layoutResID = layoutResID;
            this.bindAction = bindAction;
        }
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View view = LayoutInflater.From(parent.Context).Inflate(layoutResID, parent, false);

            return new ViewHolder(view);
        }
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            bindAction(holder, items.ElementAt(position));
        }
    }
}