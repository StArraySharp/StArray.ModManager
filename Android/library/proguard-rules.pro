# Add project specific ProGuard rules here.
# You can control the set of applied configuration files using the
# proguardFiles setting in build.gradle.

# Keep JNI native methods
-keepclasseswithmembernames class * {
    native <methods>;
}

# Keep ModLoader API
-keep class starray.android.modloader.** { *; }
