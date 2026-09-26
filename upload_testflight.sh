#!/bin/zsh
# Archive Super Ottie and upload it to App Store Connect for TestFlight.
#   ./upload_testflight.sh
# The build number is the git commit count, so every upload from a new commit is accepted.
# Requires an Xcode account signed in for team WWDLQL8W8W and an app record for com.dind.superottie.
set -euo pipefail
cd "${0:A:h}"
UNITY=/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity
export OTTIE_BUILD_NUMBER=${OTTIE_BUILD_NUMBER:-$(git rev-list --count HEAD)}
ARCHIVE=Builds/SuperOttie.xcarchive
mkdir -p Logs

echo "==> Unity export (device Xcode project), build $OTTIE_BUILD_NUMBER"
"$UNITY" -batchmode -nographics -projectPath . -buildTarget iOS \
  -executeMethod SuperOttie.Editor.BuildScript.BuildIOSDevice -logFile Logs/build_device.log
grep -E "\[Build\]" Logs/build_device.log

echo "==> xcodebuild archive"
rm -rf "$ARCHIVE"
xcodebuild -project Builds/iOS-Device/Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Release \
  -destination "generic/platform=iOS" -archivePath "$ARCHIVE" -allowProvisioningUpdates \
  archive > Logs/xcodebuild_archive.log 2>&1 || { grep -E "error:" Logs/xcodebuild_archive.log | sort -u | head -20; exit 1; }

echo "==> upload to App Store Connect"
xcodebuild -exportArchive -archivePath "$ARCHIVE" -exportOptionsPlist ExportOptions.plist \
  -exportPath Builds/Export -allowProvisioningUpdates > Logs/xcodebuild_upload.log 2>&1 \
  || { grep -iE "error" Logs/xcodebuild_upload.log | sort -u | head -20; exit 1; }
grep -iE "Upload succeeded|EXPORT SUCCEEDED|uploaded" Logs/xcodebuild_upload.log | head -5
echo "Build $OTTIE_BUILD_NUMBER uploaded. It appears in TestFlight after App Store Connect finishes processing."
