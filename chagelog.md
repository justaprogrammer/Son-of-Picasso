
<a name="v0.0.3"></a>
## [v0.0.3] - 2019-03-04
### Chores
- Moving and adding tests for ImageLoadingService
- Functionality to generate some exif data with genreated images
- Adding tests for the ImageLocationService
- Exclude designer files from codecov
- Adding codecov reach graph
- Excluding xaml and xaml.cs from codecov
- Adding codecov coverage flags
- Tweaking how code coverage is computed

### Features
- Created ImageManagementService to handle data operations

### Pull Requests
- Merge pull request [#16](https://github.com/justaprogrammer/Son-of-Picasso/issues/16) from justaprogrammer/image-management
- Merge pull request [#17](https://github.com/justaprogrammer/Son-of-Picasso/issues/17) from justaprogrammer/more-tests
- Merge pull request [#18](https://github.com/justaprogrammer/Son-of-Picasso/issues/18) from justaprogrammer/exif-data
- Merge pull request [#15](https://github.com/justaprogrammer/Son-of-Picasso/issues/15) from justaprogrammer/codecov-tweak
- Merge pull request [#14](https://github.com/justaprogrammer/Son-of-Picasso/issues/14) from justaprogrammer/adding-tests


<a name="v0.0.2"></a>
## [v0.0.2] - 2019-02-20
### Bug Fixes
- Addressing memory issues by using virtualization correctly
- Correcting ReactiveUI binding

### Chores
- Command line functionality to create test images and clear cache
- Remove nuget artifact from clean task in build script
- Fixing repository url in changelog template
- Change changelog template to not show unreleased commits by default

### Pull Requests
- Merge pull request [#13](https://github.com/justaprogrammer/Son-of-Picasso/issues/13) from justaprogrammer/image-converter
- Merge pull request [#12](https://github.com/justaprogrammer/Son-of-Picasso/issues/12) from justaprogrammer/binding-nitpick
- Merge pull request [#10](https://github.com/justaprogrammer/Son-of-Picasso/issues/10) from justaprogrammer/command-line-dev-tools
- Merge pull request [#11](https://github.com/justaprogrammer/Son-of-Picasso/issues/11) from justaprogrammer/build-fix
- Merge pull request [#9](https://github.com/justaprogrammer/Son-of-Picasso/issues/9) from justaprogrammer/changelog-template
- Merge pull request [#8](https://github.com/justaprogrammer/Son-of-Picasso/issues/8) from justaprogrammer/changelog-template


<a name="v0.0.1"></a>
## v0.0.1 - 2019-01-21
### Bug Fixes
- Fixing project nuget references
- Add IImageFolderViewModel to DI for testing ApplicationViewModel
- Tests need Serilog.Sinks.XUnit

### Chores
- Being explicit about what gets reported in code coverage
- Using parallel execution in fake.build
- Functionality to deploy to github releases on tag
- Functionality to package results in build
- Adding readme.md
- Adding chore as a changelog mention

### Code Refactoring
- Update Nuget packages
- Moving source code to /src

### Features
- Adding Fake build, Codecov and AppVeyor support

### Pull Requests
- Merge pull request [#7](https://github.com/justaprogrammer/Son-of-Picasso/issues/7) from justaprogrammer/chores
- Merge pull request [#4](https://github.com/justaprogrammer/Son-of-Picasso/issues/4) from justaprogrammer/fake-build
- Merge pull request [#5](https://github.com/justaprogrammer/Son-of-Picasso/issues/5) from justaprogrammer/move-source
- Merge pull request [#6](https://github.com/justaprogrammer/Son-of-Picasso/issues/6) from justaprogrammer/fix-test


[v0.0.3]: https://github.com/justaprogrammer/Son-of-Picasso/compare/v0.0.2...v0.0.3
[v0.0.2]: https://github.com/justaprogrammer/Son-of-Picasso/compare/v0.0.1...v0.0.2
