dotnet publish -c Release .\SonOfPicasso.sln -v quiet -fl -flp:logfile=reports\output.log;verbosity=detailed
exit $LastExitCode