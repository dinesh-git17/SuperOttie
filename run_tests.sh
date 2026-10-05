#!/bin/zsh
# Usage: ./run_tests.sh [EditMode|PlayMode]   (runs headless; prints a summary, exits non-zero on failure)
cd "${0:A:h}"
mode=${1:-EditMode}
rm -f Logs/$mode.xml  # never report a stale result if Unity aborts (e.g. compile errors)
UNITY=/Applications/Unity/Hub/Editor/6000.6.3f1/Unity.app/Contents/MacOS/Unity
"$UNITY" -batchmode -projectPath . -runTests -testPlatform $mode -testResults Logs/$mode.xml -logFile Logs/$mode.log
code=$?
python3 - "$mode" <<'PY'
import os, sys, xml.etree.ElementTree as ET
if not os.path.exists(f"Logs/{sys.argv[1]}.xml"):
    sys.exit(f"{sys.argv[1]}: no results (Unity aborted; see Logs/{sys.argv[1]}.log)")
r = ET.parse(f"Logs/{sys.argv[1]}.xml").getroot()
print(sys.argv[1], r.attrib.get("result"), "total", r.attrib.get("total"), "passed", r.attrib.get("passed"), "failed", r.attrib.get("failed"))
for tc in r.iter("test-case"):
    if tc.attrib.get("result") not in ("Passed", "Skipped"):
        m = tc.find(".//message")
        print("FAIL", tc.attrib["fullname"], "\n   ", ((m.text or "") if m is not None else "")[:600])
PY
exit $code
