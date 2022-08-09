#!/bin/bash
UNITY_BIN="/Applications/Unity/Hub/Editor/2021.3.*/Unity.app/Contents/MacOS/Unity"
IOS_VERSION="$(cat Assets/Plugins/iOS/BugfenderSDK.h | perl -ne '/BFLibraryVersionNumber_([\d_]+)/s and $v=$1; END {$v=~s/_/./g; print $v}')"
ANDROID_VERSION="$(ls Assets/Plugins/Android/android-*.aar | perl -ne 'print $1 if /.*-([\d.]+)\.aar/s')"
PACKAGE_NAME="Bugfender-android${ANDROID_VERSION}-iOS${IOS_VERSION}.unitypackage"

echo "Detected SDK versions: android: ${ANDROID_VERSION} - iOS: ${IOS_VERSION}"
$UNITY_BIN -gvh_disable -projectPath . -quit -batchmode -exportPackage Assets/Bugfender Assets/Plugins "$PACKAGE_NAME"
