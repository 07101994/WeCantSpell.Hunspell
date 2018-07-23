# WeCantSpell.Hunspell Changelog

All notable changes to this project will be documented in this file.

## 2.0.1 - 2018-07-22
### Fixed
- Applied upstream fixes for dotted `I` and Turkish
- Applied upstream fixes for forbidden words
- Applied upstream changes for Hungarian

## 2.0.0 - 2017-06-30
### Changed
- Replaced .NET Framework 4.6.1 and 4.5.1 with a single 4.5 build (net45).
- Reduced nuget package size.
- Improved performance.

### Removed
- Removed build for PCL Profile 259 (portable-net45+win8+wpa81+wp8).
- Removed build for .NET Standard 1.1 (netstandard1.1).
- Removed or made inaccessible members and types, including `WordEntrySet` and `WordEntry`.

## 1.1.0 - 2017-06-19
### Added
- New `WordEntryDetail` type to simplify storage.

### Changed
- Reduced memory usage.
- Improved performance.
- Included fixes from source up to commit 77492a4.
- Project beautification 🐝.

### Fixed
- Able to read affix files with a flag mode of NUMBER.

## 1.0.0 - 2017-05-30
- Initial release of the project

The format is based on [Keep a Changelog](http://keepachangelog.com/) and this project tries its best to adhere to [Semantic Versioning](http://semver.org/).
