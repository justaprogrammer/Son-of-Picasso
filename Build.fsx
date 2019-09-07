#r "paket: groupref FakeBuild //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.IO
open Fake.BuildServer
open Fake.IO.Globbing.Operators
open Fake.DotNet
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

  
  let configuration: (DotNet.Options -> DotNet.Options) = (fun t -> {t with
                                                                        Verbosity = Some DotNet.Verbosity.Minimal})

  DotNet.exec configuration "clean" "SonOfPicasso.sln"
  |> ignore
)

Target.create "Build" (fun _ ->
  CreateProcess.fromRawCommandLine "gitversion" "/updateassemblyinfo src\common\SharedAssemblyInfo.cs /ensureassemblyinfo"
  |> Proc.run
  |> ignore

  let configuration: (DotNet.BuildOptions -> DotNet.BuildOptions)
        = (fun t -> {t with
                        Configuration = DotNet.BuildConfiguration.Release})
  
  DotNet.build configuration "SonOfPicasso.sln"
)

let test proj framework flag =
    let projectPath = sprintf "src\\%s\\%s.csproj" proj proj
    let reportFile = sprintf "%s-%s.results.trx" proj framework

    let configuration: (DotNet.TestOptions -> DotNet.TestOptions)
        = (fun t -> {t with
                        Configuration = DotNet.BuildConfiguration.Release
                        NoBuild = true
                        Framework = Some framework
                        Logger = Some (sprintf "trx;LogFileName=%s" reportFile)
                        ResultsDirectory = Some "../../reports"
                        })

    DotNet.test configuration projectPath

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
        
    Trace.publish ImportData.BuildArtifact (sprintf "reports/%s" reportFile)

Target.create "Test" (fun _ -> 
    ()
)

Target.create "TestCore" (fun _ -> 
    test "SonOfPicasso.Core.Tests" "netcoreapp3.0" "coretest"
)

Target.create "TestUI" (fun _ -> 
    test "SonOfPicasso.UI.Tests" "netcoreapp3.0" "uitest"
)

Target.create "TestData" (fun _ -> 
    test "SonOfPicasso.Data.Tests" "netcoreapp3.0" "datatest"
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

"Build" ==> "TestCore" ==> "Test"
"Build" ==> "TestUI" ==> "Test"
"Build" ==> "TestData" ==> "Test"

"Build" ==> "Test" ==> "Default"
"Build" ==> "Package" ==> "Default"

// start build
Target.runOrDefault "Default"
