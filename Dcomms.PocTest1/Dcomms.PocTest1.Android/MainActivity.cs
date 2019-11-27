﻿using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content;

namespace Dcomms.PocTest1.Droid
{
    [Activity(Label = "Dcomms.PocTest1", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            RequestToIgnoreBatteryOptimizations();

            MainService.StartService(this, (poc1)=>
            {
                LoadApplication(new App(poc1));
            });
        }
        void RequestToIgnoreBatteryOptimizations()
        {
            var pm = (PowerManager)Application.Context.GetSystemService(Context.PowerService);

            if (!pm.IsIgnoringBatteryOptimizations(this.PackageName))
            {              
                var myIntent = new Intent();
                myIntent.SetAction(Android.Provider.Settings.ActionIgnoreBatteryOptimizationSettings);
                StartActivity(myIntent);
            }

        }



        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}