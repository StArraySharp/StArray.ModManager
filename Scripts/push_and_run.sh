TARGET_PATH="//sdcard/ADOFAI/ModManager/manager"
PATH=$PATH:/d/AndroidSDK/platform-tools
dotnet build --no-restore
adb push StArray.ModManager/bin/Debug/net10.0/StArray.ModManager.dll $TARGET_PATH/StArray.ModManager.dll
adb push StArray.ModManager/bin/Debug/net10.0/StArray.ModManager.pdb $TARGET_PATH/StArray.ModManager.pdb
adb push StArray.ModManager.Android/bin/Debug/net10.0/StArray.ModManager.Android.dll $TARGET_PATH/StArray.ModManager.Android.dll
adb push StArray.ModManager.Android/bin/Debug/net10.0/StArray.ModManager.Android.pdb $TARGET_PATH/StArray.ModManager.Android.pdb
adb shell am force-stop starray.adofai.v3
adb shell am start starray.adofai.v3/starray.adofai.v3.MainActivity
adb logcat -c
adb logcat --pid=$(adb shell pidof starray.adofai.v3)