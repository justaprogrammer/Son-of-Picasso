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
  ["reports" ; "src/common"]
  |> Seq.iter Directory.delete

  !! "nuget/*"
  -- "nuget/*.txt"
  -- "nuget/*.nuspec"
  |> File.deleteAll

  let configuration = 
    (fun p -> { p with 
                  Properties = ["Configuration", "Release"]
                  Verbosity = Some MSBuildVerbosity.Minimal })

  !! "SonOfPicasso.sln"
  |> MSBuild.run configuration null "Clean" list.Empty
  |> Trace.logItems "Clean-Output: "
)

Target.create "Build" (fun _ ->
  CreateProcess.fromRawCommandLine "gitversion" "/updateassemblyinfo src\common\SharedAssemblyInfo.cs /ensureassemblyinfo"
  |> Proc.run
  |> ignore

  let configuration = (fun p -> { p with 
                                    DoRestore = true
                                    Verbosity = Some MSBuildVerbosity.Minimal })

  !! "SonOfPicasso.sln"
  |> MSBuild.runRelease configuration null "Build"
  |> Trace.logItems "Build-Output: "
)

Target.create "Test" (fun _ ->
    [
        ("SonOfPicasso.Core.Tests", "netcoreapp2.1");
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

Target.create "Core Coverage" (fun _ ->
    [
        ("SonOfPicasso.Core.Tests", "netcoreapp2.1");
    ]
    |> Seq.iter (fun (proj, framework) -> 
            let dllPath = sprintf "src\\%s\\bin\\Release\\%s\\%s.dll" proj framework proj
            let projectPath = sprintf "src\\%s\\%s.csproj" proj proj
            let reportPath = sprintf "reports/%s-%s.coverage.xml" proj framework

            sprintf "%s --target \"dotnet\" --targetargs \"test -c Release -f %s %s --no-build\" --format opencover --output \"./%s\""
                dllPath framework projectPath reportPath
            |> CreateProcess.fromRawCommandLine "coverlet"
            |> Proc.run
            |> ignore

            Trace.publish ImportData.BuildArtifact reportPath

            if isAppveyor then
                CreateProcess.fromRawCommandLine "codecov" (sprintf "-f \"%s\"" reportPath)
                |> Proc.run
                |> ignore
        )
)

Target.create "Coverage" (fun _ ->
    [
        ("SonOfPicasso.UI.Tests", "net472");
    ]
    |> Seq.iter (fun (proj, framework) -> 
            let dllPath = sprintf "src\\%s\\bin\\Release\\%s\\%s.dll" proj framework proj
            let reportPath = sprintf "reports/%s-%s.coverage.xml" proj framework

            Directory.ensure "reports"

            OpenCover.getVersion (Some (fun p -> { p with ExePath = "./packages/fakebuildresources/OpenCover/tools/OpenCover.Console.exe" }))

            OpenCover.run (fun p ->
                { p with
                        ExePath = "./packages/fakebuildresources/OpenCover/tools/OpenCover.Console.exe"
                        TestRunnerExePath = "./packages/fakebuildresources/xunit.runner.console/tools/net472/xunit.console.exe";
                        Output = reportPath;
                        Register = OpenCover.RegisterUser;
                        Filter = "+[SonOfPicasso.*]*";
                })
                (sprintf "%s -noshadow" dllPath)

            Trace.publish ImportData.BuildArtifact reportPath

            if isAppveyor then
                CreateProcess.fromRawCommandLine "codecov" (sprintf "-f \"%s\"" reportPath)
                |> Proc.run
                |> ignore
        )
)

Target.create "Default" (fun _ -> 
    ()
)

open Fake.Core.TargetOperators
"Clean" ==> "Build"

"Build" ==> "Test" ==> "Default"
"Build" ==> "Core Coverage" ==> "Default"
"Build" ==> "Coverage" ==> "Default"

// start build
Target.runOrDefault "Default"
