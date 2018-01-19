using Android.App;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.Locations;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

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
        private TextView distanceText;
        private TextView timeText;
        private TextView tempoText;

        private Button posButton;
        private Button startButton;

        private GoogleMap _map;

        public MapFragment _mapFragment { get; private set; }

        private Circle blueDot;
        private PolylineOptions polylineOptions = new PolylineOptions().InvokeColor(Color.CadetBlue).InvokeWidth(10);

        private bool clickCondition = false;
        private bool startCondition = false;

        private Location lastLocation;
        private int distance;

        private int timerCount = 0;
        private Timer timer;

        #region OnCreate, OnResume, OnPause
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.Map);

            InitButtons();

            PositionButtonClick();
            StartButtonClick();

            InitializeLocationManager();
            InitMapFragment();

            InitTimer();
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

            if (blueDot != null)
                blueDot.Remove();

            if (startCondition)
            {
                distance += CalcStep();

                ZoomToCurrentLoc();
                DrawRoute();
            }

            ShowParameters();
            DrawBlueDot();

            if (startCondition)
                lastLocation = currentLocation;
            else
                lastLocation = null;
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
                    .InvokeCompassEnabled(true)
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
            if (currentLocation != null)
            {
                locationText.Text = string.Format("Pozycja: {0:f5}; {1:f5}", currentLocation.Latitude, currentLocation.Longitude);
                bearingText.Text = string.Format("Kierunek: {0} deg", currentLocation.Bearing);
                distanceText.Text = string.Format("Dystans: {0} m", distance);


                if (averageTempo() > 10)
                {
                    int km = 1000;
                    int hour = 3600;

                    tempoText.Text = string.Format("Średnie tempo: {0:f1} km/h", (averageTempo() * hour / km));
                }
                else
                    tempoText.Text = string.Format("Średnie tempo: {0:f1} m/s", averageTempo());

                if (distance > 999)
                {
                    int km = distance / 1000;
                    int m = distance % 1000;

                    distanceText.Text = string.Format("Dystans: {0} km {1} m", km, m);
                }
                else
                    distanceText.Text = string.Format("Dystans: {0} m", distance);

                if (currentLocation.Speed > 10)
                {
                    int km = 1000;
                    int hour = 3600;

                    velocityText.Text = string.Format("Prędkość: {0:f1} km/h", (currentLocation.Speed * hour / km));
                }
                else
                    velocityText.Text = string.Format("Prędkość: {0:f1} m/s", currentLocation.Speed);

                if (timerCount > 59)
                {
                    int min = timerCount / 60;
                    int sec = timerCount % 60;
                    timeText.Text = string.Format("Czas: {0} min {1} s", min, sec);
                }
                else
                    timeText.Text = string.Format("Czas: {0} s", timerCount);
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
        }

        private void DrawBlueDot()
        {
            CircleOptions circleOptions = new CircleOptions();
            circleOptions.InvokeCenter(new LatLng(currentLocation.Latitude, currentLocation.Longitude));
            circleOptions.InvokeRadius(5);
            circleOptions.InvokeFillColor(Color.CadetBlue);
            circleOptions.InvokeStrokeColor(Color.White);
            circleOptions.InvokeStrokeWidth(2);
            circleOptions.InvokeZIndex(1);

            blueDot = _map.AddCircle(circleOptions);
        }

        private void StartButtonClick()
        {
            startButton = FindViewById<Button>(Resource.Id.StartButton);
            startButton.Click += (sender, e) =>
            {
                if (currentLocation != null)
                {
                    if (!clickCondition)
                    {
                        startCondition = true;
                        clickCondition = true;
                        startButton.Text = "Stop";
                        timer.Start();
                    }
                    else
                    {
                        startCondition = false;
                        clickCondition = false;
                        startButton.Text = "Start";
                        timer.Stop();
                    }
                }
            };
        }

        private void PositionButtonClick()
        {
            posButton = FindViewById<Button>(Resource.Id.PositionButton);
            posButton.Click += (sender, e) =>
            {
                if (currentLocation != null)
                {
                    ZoomToCurrentLoc();
                }
            };
        }

        public double ConvertToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        private int CalcStep()
        {
            if (lastLocation != null)
            {
                var lat1 = lastLocation.Latitude;
                var lon1 = lastLocation.Longitude;

                var lat2 = currentLocation.Latitude;
                var lon2 = currentLocation.Longitude;

                var R = 6371e3; // metres
                var phi1 = ConvertToRadians(lat1);
                var phi2 = ConvertToRadians(lat2);
                var deltaPhi = ConvertToRadians(lat2 - lat1);
                var deltaLambda = ConvertToRadians(lon2 - lon1);

                var a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                        Math.Cos(phi1) * Math.Cos(phi2) *
                        Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
                var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

                var d = R * c;

                return ((int)d);
            }
            else
                return 0;
        }

        private void InitTimer()
        {
            timer = new Timer();
            timer.Interval = 1000;
            timer.Elapsed += Timer_Elapsed;
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timerCount++;
        }

        //meters per second
        private float averageTempo()
        {
            if (timerCount != 0)
                return distance / timerCount;
            else
                return 0;
        }

        private void InitButtons()
        {
            locationText = FindViewById<TextView>(Resource.Id.LocTextView);
            velocityText = FindViewById<TextView>(Resource.Id.VelTextView);
            bearingText = FindViewById<TextView>(Resource.Id.BeaTextView);
            distanceText = FindViewById<TextView>(Resource.Id.DisTextView);
            timeText = FindViewById<TextView>(Resource.Id.TimeTextView);
            tempoText = FindViewById<TextView>(Resource.Id.AvTempoTextView);
        }
        #endregion
    }
}