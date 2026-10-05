#!/bin/zsh
# Usage: gen.sh <prompt-name> [reference images...]
# Reads prompts/<name>.txt, asks Codex (ChatGPT Images) to render it into raw/<name>.png.
# CODEX_MODEL picks the Codex model (default gpt-6-sol).
set -euo pipefail
cd "${0:A:h}"
name="$1"; shift
prompt="Use your built-in image generation tool (ChatGPT Images) to create ONE image, then save the resulting PNG as ./raw/${name}.png (overwrite if it exists). Do not draw it with code; it must come from the image generator. Do not modify any other files. Din has already approved this generation, so go ahead without asking for confirmation.

$(cat prompts/${name}.txt)"
imgs=()
for r in "$@"; do imgs+=(-i "$r"); done
print -r -- "$prompt" | codex exec --skip-git-repo-check -m "${CODEX_MODEL:-gpt-6-sol}" -c model_reasoning_effort="low" -s workspace-write -C "$PWD" "${imgs[@]}" > "logs/${name}.log" 2>&1
[[ -f raw/${name}.png ]] && echo "OK ${name}" || { echo "FAIL ${name}"; tail -5 logs/${name}.log; exit 1; }
