#!/bin/zsh
# Waits for the Codex usage window to reset, then generates every asset in priority order.
# Retries an asset (after waiting) when the usage limit is hit again.
cd "${0:A:h}"
if [[ -z "${NOW:-}" ]]; then  # NOW=1 skips waiting for the usage window
  target=$(date -j -f "%H:%M" "21:53" +%s); now=$(date +%s); (( target > now )) && sleep $(( target - now ))
fi
REF=ottie_reference.jpg
typeset -A refs=(player_sheet $REF title_art $REF app_icon $REF)
for a in player_sheet items_sheet tiles_sheet enemies_sheet bg_day goal_sheet title_art logo app_icon bg_sunset bg_twilight decor_sheet; do
  [[ -f raw/$a.png ]] && { echo "skip $a"; continue; }
  for attempt in {1..24}; do
    echo "$(date +%T) gen $a (attempt $attempt)"
    if ./gen.sh $a ${refs[$a]:-} ; then break; fi
    if grep -q "usage limit" logs/$a.log; then echo "usage limit; waiting 15m"; sleep 900; elif grep -q "401 Unauthorized" logs/$a.log; then echo "auth error; waiting 5m"; sleep 300; else sleep 20; fi
  done
done
echo "$(date +%T) done"; ls raw
