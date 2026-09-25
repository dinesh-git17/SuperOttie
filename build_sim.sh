#!/bin/zsh
# Build Super Ottie for the iOS Simulator and install it on the iPhone 17 Pro (iOS 27) simulator.
#   ./build_sim.sh            export + compile + install + launch
#   SIM=<udid> ./build_sim.sh to target another simulator
set -euo pipefail
cd "${0:A:h}"
UNITY=/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity
SIM=${SIM:-$(xcrun simctl list devices available | grep -E "iPhone 17 Pro \(" | head -1 | grep -oE "[0-9A-F-]{36}")}
mkdir -p Logs

echo "==> Unity export (Xcode project)"
"$UNITY" -batchmode -nographics -projectPath . -buildTarget iOS \
  -executeMethod SuperOttie.Editor.BuildScript.BuildIOSSimulator -logFile Logs/build_ios.log
grep -E "\[Build\]" Logs/build_ios.log

echo "==> xcodebuild (iphonesimulator)"
xcodebuild -project Builds/iOS-Simulator/Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Release \
  -sdk iphonesimulator -destination "id=$SIM" -derivedDataPath Builds/DerivedData-Sim \
  build CODE_SIGNING_ALLOWED=NO > Logs/xcodebuild_sim.log 2>&1 || { grep -E "error:" Logs/xcodebuild_sim.log | head -20; exit 1; }
APP=Builds/DerivedData-Sim/Build/Products/Release-iphonesimulator/SuperOttie.app

echo "==> install + launch on $SIM"
xcrun simctl boot "$SIM" 2>/dev/null || true
xcrun simctl install "$SIM" "$APP"
xcrun simctl launch "$SIM" com.dind.superottie
