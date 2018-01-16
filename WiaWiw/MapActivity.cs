using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Locations;
using Android.Util;
using System.Timers;
using System.Threading.Tasks;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;

namespace WiaWiw
{
    [Activity(Label = "Map Activity")]
    public class MapActivity : Activity, ILocationListener, IOnMapReadyCallback
    {
        private string TAG = "X:" + typeof(MapActivity).Name;
        private string locationProvider;
        private Location currentLocation;
        private LocationManager locationManager;

        private CameraPosition.Builder builder = CameraPosition.InvokeBuilder();

        private TextView locationText;
        private TextView velocityText;
        private TextView bearingText;

        private GoogleMap _map;

        private PolylineOptions polylineOptions = new PolylineOptions()
            .InvokeColor(Color.Blue)
            .InvokeWidth(20);

        //private List<LatLng> arrayPoints = null;
        //PolylineOptions polylineOptions;
        //List<LatLng> points = null;
        
        //public DateTime DeviceDateTime { get; set; }
        public MapFragment _mapFragment { get; private set; }

        #region OnCreate, OnResume, OnPause
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Map);

            locationText = FindViewById<TextView>(Resource.Id.LocTextView);
            velocityText = FindViewById<TextView>(Resource.Id.VelTextView);
            bearingText = FindViewById<TextView>(Resource.Id.BeaTextView);

            //arrayPoints = new List<LatLng>();

            InitializeLocationManager();
            InitMapFragment();
        }

        protected override void OnResume()
        {
            base.OnResume();
            locationManager.RequestLocationUpdates(locationProvider, 0, 0, this);
        }

        protected override void OnPause()
        {
            base.OnPause();
            locationManager.RemoveUpdates(this);
        }
        #endregion

        #region ILocationListener
        public void OnLocationChanged(Location location)
        {
            currentLocation = location;

            ZoomToCurrentLoc();
            ShowParameters();
            DrawRoute();

        }        

        public void OnProviderDisabled(string provider)
        {
            //throw new NotImplementedException();
        }

        public void OnProviderEnabled(string provider)
        {
            //throw new NotImplementedException();
        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {
            //throw new NotImplementedException();
        }
        #endregion

        #region IOnMapReadyCallback
        public void OnMapReady(GoogleMap map)
        {
            _map = map;
        }
        #endregion

        #region MyMethods
        private void InitializeLocationManager()
        {
            locationManager = (LocationManager)GetSystemService(LocationService);
            Criteria criteriaForLocationService = new Criteria
            {
                Accuracy = Accuracy.Fine
            };
            IList<string> acceptableLocationProviders = locationManager.GetProviders(criteriaForLocationService, true);

            if (acceptableLocationProviders.Any())
            {
                locationProvider = acceptableLocationProviders.First();
            }
            else
            {
                locationProvider = string.Empty;
            }
            Log.Debug(TAG, "Using " + locationProvider + ".");
        }

        private void InitMapFragment()
        {
            _mapFragment = FragmentManager.FindFragmentByTag("map") as MapFragment;
            if (_mapFragment == null)
            {
                GoogleMapOptions mapOptions = new GoogleMapOptions()
                    .InvokeMapType(GoogleMap.MapTypeTerrain)
                    .InvokeZoomControlsEnabled(false)
                    .InvokeCompassEnabled(true)
                    .InvokeMapToolbarEnabled(true)
                   ;

                FragmentTransaction fragTx = FragmentManager.BeginTransaction();
                _mapFragment = MapFragment.NewInstance(mapOptions);
                fragTx.Add(Resource.Id.map, _mapFragment, "map");
                fragTx.Commit();
            }
            _mapFragment.GetMapAsync(this);
        }

        private void ShowParameters()
        {
            if (currentLocation == null)
            {
                locationText.Text = "Unable to determine your location. Try again in a short while.";
            }
            else
            {
                locationText.Text = string.Format("Pozycja: {0:f5}; {1:f5}", currentLocation.Latitude, currentLocation.Longitude);
                velocityText.Text = string.Format("Prêdkoœæ: {0:f1} m/s", currentLocation.Speed);
                bearingText.Text = string.Format("Kierunek: {0:f0} deg", currentLocation.Bearing);
            }
        }

        private void ZoomToCurrentLoc()
        {
            LatLng zoomToLoc = new LatLng(currentLocation.Latitude, currentLocation.Longitude);
            builder.Target(zoomToLoc);
            builder.Zoom(17);
            CameraPosition cameraPosition = builder.Build();
            CameraUpdate cameraUpdate = CameraUpdateFactory.NewCameraPosition(cameraPosition);

            if (_map != null)
            {
                _map.MoveCamera(cameraUpdate);
            }
        }

        private void DrawRoute()
        {
            polylineOptions.Add(new LatLng(currentLocation.Latitude, currentLocation.Longitude));
            _map.AddPolyline(polylineOptions);

            //LatLng position = new LatLng(currentLocation.Latitude, currentLocation.Longitude);
            //points.Add(position);

            //polylineOptions.AddAll(points);
            //polylineOptions.Color(Color.Red);
            //polylineOptions.Width(20);

            //_map.AddPolyline(polylineOptions);
        }
        #endregion
    }
}