TARGET_PATH="//sdcard/ModLoader/loader"
dotnet build
adb push StArray.ModLoader/bin/Debug/net10.0/ModLoader.dll $TARGET_PATH/ModLoader.dll
adb shell am force-stop com.DefaultCompany.Simple3D
adb shell am start com.DefaultCompany.Simple3D/starray.android.launcher.MainActivity