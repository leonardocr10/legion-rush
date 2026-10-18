#!/usr/bin/env bash
# Compila todos os scripts do jogo com o compilador e as DLLs do próprio Unity 6 (sem abrir o editor, sem licença).
# URP/Core RP são substituídos por stubs (ci/stubs). Falha com os erros do compilador.
set -euo pipefail

UNITY_ROOT="${UNITY_ROOT:-/opt/unity}"
DATA="$UNITY_ROOT/Editor/Data"
OUT="${RUNNER_TEMP:-/tmp}/lr-compile"
mkdir -p "$OUT"

pick() { for p in "$@"; do [ -e "$p" ] && { echo "$p"; return 0; }; done; return 1; }

DOTNET=$(pick "$DATA/NetCoreRuntime/dotnet") || { echo "dotnet do Unity não encontrado"; find "$DATA" -maxdepth 2 -iname '*dotnet*' | head; exit 2; }
CSC=$(pick "$DATA/DotNetSdkRoslyn/csc.dll") || { echo "csc.dll não encontrado"; find "$DATA" -name csc.dll | head; exit 2; }
API=$(pick "$DATA/MonoBleedingEdge/lib/mono/4.7.1-api" "$DATA/MonoBleedingEdge/lib/mono/4.8-api") || { echo "perfil .NET do Unity não encontrado"; ls "$DATA/MonoBleedingEdge/lib/mono" || true; exit 2; }
MANAGED="$DATA/Managed/UnityEngine"
[ -d "$MANAGED" ] || { echo "Managed/UnityEngine não encontrado"; ls "$DATA/Managed"; exit 2; }
echo "Unity: $(cat "$UNITY_ROOT/Editor/Data/../../version" 2>/dev/null || basename "$UNITY_ROOT")"
echo "csc: $CSC | perfil: $API"

BASE_REFS=(-r:"$API/mscorlib.dll" -r:"$API/System.dll" -r:"$API/System.Core.dll" -r:"$API/System.Xml.dll")
for f in "$API"/Facades/*.dll; do BASE_REFS+=(-r:"$f"); done
ENGINE_REFS=(); EDITOR_REFS=()
for f in "$MANAGED"/UnityEngine*.dll; do ENGINE_REFS+=(-r:"$f"); done
for f in "$MANAGED"/UnityEditor*.dll; do EDITOR_REFS+=(-r:"$f"); done
[ -f "$DATA/Managed/UnityEditor.Graphs.dll" ] && EDITOR_REFS+=(-r:"$DATA/Managed/UnityEditor.Graphs.dll")

# Erros viram anotações do GitHub (visíveis na aba Actions e pela API de check-runs).
csc() {
  set +e
  "$DOTNET" "$CSC" -nologo -nostdlib -noconfig -langversion:9.0 -target:library -nowarn:1701,1702 -warnaserror- "$@" > "$OUT/csc.log" 2>&1
  local code=$?
  set -e
  cat "$OUT/csc.log"
  grep -E ': error ' "$OUT/csc.log" | head -40 | sed -E 's#^(.*)\(([0-9]+),([0-9]+)\): error (CS[0-9]+): (.*)$#::error file=\1,line=\2,col=\3::\4 \5#' || true
  [ $code -eq 0 ] || { echo "::error::Falha de compilação (código $code)"; exit $code; }
}

echo "== Stubs URP =="
csc "${BASE_REFS[@]}" "${ENGINE_REFS[@]}" -out:"$OUT/LR.Stubs.dll" ci/stubs/*.cs

mapfile -t RUNTIME < <(find Assets -name '*.cs' -not -path '*/Editor/*' | sort)
mapfile -t EDITOR < <(find Assets -path '*/Editor/*' -name '*.cs' | sort)
echo "Runtime: ${#RUNTIME[@]} arquivos | Editor: ${#EDITOR[@]} arquivos"

echo "== Runtime (player Android) =="
csc "${BASE_REFS[@]}" "${ENGINE_REFS[@]}" -r:"$OUT/LR.Stubs.dll" \
  -define:"UNITY_ANDROID;UNITY_6000_0_OR_NEWER;ENABLE_LEGACY_INPUT_MANAGER" -out:"$OUT/LR.Player.dll" "${RUNTIME[@]}"

echo "== Runtime (editor / play mode) =="
csc "${BASE_REFS[@]}" "${ENGINE_REFS[@]}" "${EDITOR_REFS[@]}" -r:"$OUT/LR.Stubs.dll" \
  -define:"UNITY_EDITOR;UNITY_6000_0_OR_NEWER;ENABLE_LEGACY_INPUT_MANAGER;DEVELOPMENT_BUILD" -out:"$OUT/LR.Runtime.dll" "${RUNTIME[@]}"

echo "== Editor =="
csc "${BASE_REFS[@]}" "${ENGINE_REFS[@]}" "${EDITOR_REFS[@]}" -r:"$OUT/LR.Stubs.dll" -r:"$OUT/LR.Runtime.dll" \
  -define:"UNITY_EDITOR;UNITY_6000_0_OR_NEWER" -out:"$OUT/LR.Editor.dll" "${EDITOR[@]}"

echo "Compilação OK: runtime (player + editor) e scripts de editor."
