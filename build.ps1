dotnet publish -c Release .\SonOfPicasso.sln -v quiet -fl -flp:logfile=reports\output.log -flp:verbosity=detailed
exit $LastExitCode