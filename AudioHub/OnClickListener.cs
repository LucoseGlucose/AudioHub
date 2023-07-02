using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioHub
{
    public class OnClickListener : Java.Lang.Object, View.IOnClickListener
    {
        private readonly Action<View> onClick;

        public OnClickListener(Action<View> onClick)
        {
            this.onClick = onClick;
        }
        public void OnClick(View v)
        {
            onClick(v);
        }

        public JniManagedPeerStates JniManagedPeerState => JniManagedPeerStates.None;

        public void Disposed()
        {

        }
        public void DisposeUnlessReferenced()
        {
            
        }
        public void Finalized()
        {

        }
        public void SetJniIdentityHashCode(int value)
        {

        }
        public void SetJniManagedPeerState(JniManagedPeerStates value)
        {

        }
        public void SetPeerReference(JniObjectReference reference)
        {

        }
    }
}