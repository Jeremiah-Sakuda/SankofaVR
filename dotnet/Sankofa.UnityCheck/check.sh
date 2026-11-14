#!/bin/sh
# Type-checks the Unity runtime scripts (as built for the headset and as built for the editor), the PlayMode tests
# and the story table's EditMode tests, without Unity. It compiles; it runs nothing. See docs/development.md.
cd "$(dirname "$0")" && export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
fail=0
check() {
  name=$1; shift
  out=$(dotnet build "$@" 2>&1)
  n=$(printf '%s\n' "$out" | grep -c ' error ')
  echo "$name: $n errors"
  if [ "$n" != "0" ]; then fail=1; printf '%s\n' "$out" | grep ' error ' | sed 's/ \[.*//' | sort -u | cut -c1-260 | head -25; fi
}
check "runtime, headset" Runtime/Sankofa.UnityCheck.Runtime.csproj
check "runtime, editor" Runtime/Sankofa.UnityCheck.Runtime.csproj -p:ExtraDefines=UNITY_EDITOR
check "PlayMode tests" PlayModeTests/Sankofa.UnityCheck.PlayModeTests.csproj
check "EditMode tests (story table)" EditorTests/Sankofa.UnityCheck.EditorTests.csproj
exit $fail
