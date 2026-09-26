"""Tiny App Store Connect API client for TestFlight chores.

    ~/.appstoreconnect/.venv/bin/python Tools/asc.py GET /v1/apps
    ~/.appstoreconnect/.venv/bin/python Tools/asc.py GET '/v1/betaGroups?filter[app]=<id>'
    ~/.appstoreconnect/.venv/bin/python Tools/asc.py POST /v1/betaGroups '{"data": {...}}'

Reads ~/.appstoreconnect/config.json ({"key_id", "issuer_id"}) and the matching
~/.appstoreconnect/private_keys/AuthKey_<key_id>.p8. No credentials live in this repo.
"""

import json
import sys
import time
from pathlib import Path

import jwt
import requests

HOME = Path.home() / ".appstoreconnect"
BASE = "https://api.appstoreconnect.apple.com"


def token() -> str:
    cfg = json.loads((HOME / "config.json").read_text())
    key = (HOME / "private_keys" / f"AuthKey_{cfg['key_id']}.p8").read_text()
    now = int(time.time())
    claims = {"iss": cfg["issuer_id"], "iat": now, "exp": now + 15 * 60, "aud": "appstoreconnect-v1"}
    return jwt.encode(claims, key, algorithm="ES256", headers={"kid": cfg["key_id"], "typ": "JWT"})


def call(method: str, path: str, body: dict | None = None) -> dict:
    url = path if path.startswith("http") else BASE + path
    r = requests.request(method, url, json=body, headers={"Authorization": f"Bearer {token()}"}, timeout=60)
    if r.status_code >= 400:
        raise SystemExit(f"{method} {path} -> {r.status_code}\n{r.text}")
    return r.json() if r.content else {}


if __name__ == "__main__":
    method, path = sys.argv[1].upper(), sys.argv[2]
    body = json.loads(sys.argv[3]) if len(sys.argv) > 3 else None
    print(json.dumps(call(method, path, body), indent=2))
