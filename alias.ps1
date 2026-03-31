if ($IsMacOS) {
	set-alias -name sample -Value $env:InstallDirectory\Sample.CommandLine\Sample.CommandLine
	set-alias -name test -Value $env:InstallDirectory\LoggingTest\LoggingTest
}else {
	set-alias -name sample -Value $env:InstallDirectory\Sample.CommandLine\Sample.CommandLine.exe
	set-alias -name test -Value $env:InstallDirectory\LoggingTest\LoggingTest.exe
}


