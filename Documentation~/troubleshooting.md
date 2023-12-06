# Troubleshooting

## Native Library on Windows

If you run into an error message similar to this one:

```log
Failed to load 'draco_unity.dll' because one or more of its dependencies could not be loaded
```

The issue might get resolved by installing [Microsoft Visual C++ Redistributable packages for Visual Studio 2015, 2017, 2019, and 2022](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170#visual-studio-2015-2017-2019-and-2022).

## Code signing issues

The binary libraries used in this package are currently not code-signed. macOS in particular will not let you load the `draco_unity.bundle` for that reason.

Here's the steps to make it work on macOS

1. When you first open a project with *Draco for Unity* (or add the package), you get prompted to remove the "broken" draco_unity.bundle. Don't do it and click "cancel" instead.
2. Open the macOS "System Preferences" and go to "Security & Privacy". At the bottom of the "General" tab you should see a warning about draco_unity.bundle. Click the "Allow anyways" button besides it.
3. Restart Unity
4. Now you get another, similar prompt (see step 1) with the third option "Open". Click it
5. Now it should work (at least for development on your machine)

If the problem persists please report the issue and consider replacing the native libraries in the `Runtime/Plugins` folder with ones you built and signed on your own. Sources can be found in [draco repository](https://github.com/atteneder/draco).
