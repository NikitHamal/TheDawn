using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Microsoft.Xna.Framework;

namespace TheDawn.Platforms.Android;

[Activity(
    Label = "The Dawn",
    MainLauncher = true,
    Icon = "@drawable/icon",
    Theme = "@style/AppTheme",
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.Landscape,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
public sealed class MainActivity : AndroidGameActivity
{
    private DawnGame? _game;

    protected override void OnCreate(Bundle? bundle)
    {
        base.OnCreate(bundle);
        _game = new DawnGame();
        SetContentView((View)_game.Services.GetService(typeof(View))!);
        _game.Run();
    }
}
