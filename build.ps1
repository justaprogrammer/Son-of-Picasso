dotnet publish -c Release .\SonOfPicasso.sln -v quiet -fl -flp:logfile=build.log -flp:verbosity=detailed
exit $LastExitCode