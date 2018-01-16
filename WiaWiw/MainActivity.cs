using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content;

namespace WiaWiw
{

    [Activity(Label = "WiaWiw", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.Main);

            Button mapButton = FindViewById<Button>(Resource.Id.MapButton);
            mapButton.Click += (sender, e) =>
            {
                var intent = new Intent(this, typeof(MapActivity));
                StartActivity(intent);
            };
        }
    }
}

