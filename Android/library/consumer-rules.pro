# Consumer ProGuard rules for StArray.ModLoader AAR library.
# These rules are included when the AAR is consumed by an application.
# See https://developer.android.com/studio/projects/android-library#considerations

# Keep ModLoader public API
-keep class starray.android.modloader.ModLoader {
    public *;
}
-keep class starray.android.modloader.ModLoaderNative {
    native <methods>;
}
