## Download
- [Windows x64 ZIP]({{DOWNLOAD_URL}})
- [SHA-256 checksum]({{CHECKSUM_URL}})

## Getting started
Extract the ZIP file, then run `SysmacVariableBackupViewer.exe`.

Requirements: Windows 10 or later (x64). The .NET runtime is included.

## Changes
### Added
- Reproducible dummy backup and project metadata samples, with an updated screenshot and loading instructions.

### Changed
- Refresh the viewer with softer grid lines and selection, a blue primary button, and distinct colours for variable names, data types and BOOL values.
- Migrate the application and build tooling to .NET 10. The Windows distribution includes the runtime.
- Publish the executable, fixed-name distribution ZIP and SHA-256 checksum under `artifacts/release/` in local builds and CI.

### Fixed
- Keep BOOL values readable on selected cells and primary button text readable while pressed.
- Handle VirusTotal upload conflicts and delayed analysis lookup.
- Reject incomplete VirusTotal responses without a matching API-provided executable SHA-256.

<!-- virustotal:start -->
{{VIRUSTOTAL_CONTENT}}
<!-- virustotal:end -->
