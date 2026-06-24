# Add project specific ProGuard rules here.
# You can control the set of applied configuration files using the
# proguardFiles setting in build.gradle.

# Keep JNI native methods
-keepclasseswithmembernames class * {
    native <methods>;
}

# Keep ModManager API
-keep class starray.android.modmanager.** { *; }
