#r "paket: groupref FakeBuild //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.IO
open Fake.BuildServer
open Fake.IO.Globbing.Operators
open Fake.DotNet
open Fake.DotNet.Testing
open Fake.Core
open Fake.Tools

BuildServer.install [
    AppVeyor.Installer
]

let isAppveyor = AppVeyor.detect()
let gitVersion = GitVersion.generateProperties id

Target.create "Clean" (fun _ ->
  ["reports" ; "build" ; "src/common"]
  |> Seq.iter Directory.delete

  let configuration = 
    (fun p -> { p with 
                  Properties = ["Configuration", "Release"]
                  Verbosity = Some MSBuildVerbosity.Minimal })

  !! "SonOfPicasso.sln"
  |> MSBuild.run configuration null "Clean" list.Empty
  |> Trace.logItems "Clean-Output: "
)

Target.create "Build" (fun _ ->
  NuGet.Restore.RestoreMSSolutionPackages (fun p -> {p with ToolPath = "packages\\fakebuildresources\\NuGet.CommandLine\\tools\\nuget.exe"}) "SonOfPicasso.sln"

  CreateProcess.fromRawCommandLine "gitversion" "/updateassemblyinfo src\common\SharedAssemblyInfo.cs /ensureassemblyinfo"
  |> Proc.run
  |> ignore

  let logger : MSBuildDistributedLoggerConfig = { 
        ClassName = None ; 
        AssemblyPath = "packages\\fakebuildresources\\BCC-MSBuildLog\\tools\\net472\\BCCMSBuildLog.dll" ;
        Parameters = None
    }

  let configuration = (fun p -> { p with 
                                    DoRestore = true
                                    Verbosity = Some MSBuildVerbosity.Minimal
                                    Loggers = Some([logger])})

  !! "SonOfPicasso.sln"
  |> MSBuild.runRelease configuration null "Build"
  |> Trace.logItems "Build-Output: "
)

Target.create "Test" (fun _ ->
    [
        ("SonOfPicasso.Core.Tests", "net472");
        ("SonOfPicasso.UI.Tests", "net472");
    ]
    |> Seq.iter (fun (proj, framework) ->
        (
            let projectPath = sprintf "src\\%s\\%s.csproj" proj proj
            let reportFile = sprintf "%s-%s.results.trx" proj framework

            let configuration: (DotNet.TestOptions -> DotNet.TestOptions)
                = (fun t -> {t with
                                Configuration = DotNet.BuildConfiguration.Release
                                NoBuild = true
                                Framework = Some framework
                                Logger = Some (sprintf "trx;LogFileName=%s" reportFile)
                                ResultsDirectory = Some "../../reports"})

            DotNet.test configuration projectPath
            
            Trace.publish ImportData.BuildArtifact (sprintf "reports/%s" reportFile)
        ))
)

Target.create "Coverage" (fun _ ->
    [
        ("SonOfPicasso.Core.Tests", "net472", "coretest");
        ("SonOfPicasso.UI.Tests", "net472", "uitest");
    ]
    |> Seq.iter (fun (proj, framework, flag) -> 
            let dllPath = sprintf "src\\%s\\bin\\Release\\%s\\%s.dll" proj framework proj
            let projectPath = sprintf "src\\%s\\%s.csproj" proj proj
            let reportPath = sprintf "reports/%s-%s.coverage.xml" proj framework

            Directory.ensure "reports"
          
            sprintf "%s --target \"dotnet\" --targetargs \"test -c Release -f %s %s --no-build\" --include \"[SonOfPicasso.*]*\" --exclude \"[SonOfPicasso.*.Tests]*\" --exclude \"[SonOfPicasso.Testing.Common]*\" --format opencover --output \"./%s\""
                dllPath framework projectPath reportPath
            |> CreateProcess.fromRawCommandLine "coverlet"
            |> Proc.run
            |> ignore

            Trace.publish ImportData.BuildArtifact reportPath

            if isAppveyor then
                CreateProcess.fromRawCommandLine "codecov" (sprintf "-f \"%s\" --flag %s" reportPath flag)
                |> Proc.run
                |> ignore
        )
)


Target.create "Package" (fun _ -> 
    let packagePath = (sprintf "build/son-of-picasso-%s.zip" gitVersion.FullSemVer)

    Directory.ensure "build"
  
    !! "src/SonOfPicasso.UI/bin/Release/**/*"
    |> Zip.filesAsSpecs "src\\SonOfPicasso.UI\\bin\\Release"
    |> Zip.zipSpec packagePath

    Trace.publish ImportData.BuildArtifact packagePath
)

Target.create "Default" (fun _ -> 
    ()
)

open Fake.Core.TargetOperators
"Clean" ==> "Build"

"Build" ==> "Test" ==> "Default"
"Build" ==> "Coverage" ==> "Default"
"Build" ==> "Package" ==> "Default"

// start build
Target.runOrDefault "Default"
