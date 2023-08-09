using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Java.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AudioHub
{
    public class OnEditorActionListener : Java.Lang.Object, TextView.IOnEditorActionListener
    {
        public delegate bool EditorAction(TextView v, ImeAction actionId, KeyEvent e);
        private readonly EditorAction onEditorAction;

        public OnEditorActionListener(EditorAction onEditorAction)
        {
            this.onEditorAction = onEditorAction;
        }
        public bool OnEditorAction(TextView v, ImeAction actionId, KeyEvent e)
        {
            return onEditorAction(v, actionId, e);
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