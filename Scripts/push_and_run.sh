TARGET_PATH="//sdcard/ModManager/manager"
dotnet build
adb push StArray.ModManager/bin/Debug/net10.0/StArray.ModManager.dll $TARGET_PATH/StArray.ModManager.dll
adb shell am force-stop com.DefaultCompany.Simple3D
adb shell am start com.DefaultCompany.Simple3D/starray.android.launcher.MainActivity