:<<"::CMDLITERAL"
@PUSHD "%~dp0..\..\rider-fsharp"
@CALL "gradlew.bat" "%*"
@POPD  
@GOTO :EOF
::CMDLITERAL
pushd "../../rider-fsharp"
exec "gradlew" "$@"
popd
