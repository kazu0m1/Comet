# Notice

Comet is an independent Windows comic viewer implementation.

Its reading workflow and keyboard behavior are intentionally familiar to users of
[MComix](https://sourceforge.net/projects/mcomix/), which is a separate project.
No MComix source code, artwork, binaries, or branding are included in Comet.
Comet is not affiliated with or endorsed by the MComix project.

This repository is licensed under the MIT License. MComix has its own license and
copyright holders; refer to the MComix project for those terms.


## Third-party software

Comet uses SkiaSharp for WebP decoding fallback on Windows systems where the WIC WebP
extension is not available. SkiaSharp is distributed under the MIT License.


Comet uses SharpCompress for RAR/CBR and 7z/CB7 reading. SharpCompress is distributed
under the MIT License.


The test fixture `tests/Comet.SmokeTests/Fixtures/SharpCompress_Rar.cbr` is a verbatim
copy of SharpCompress `tests/TestArchives/Archives/Rar.rar` (Git blob
`050509c347d6911db98f13db8aa33c8f914ba9de`) and is used only for automated
RAR/CBR compatibility testing. It is covered by the SharpCompress MIT License.


Additional solid-archive fixtures are also verbatim copies from the SharpCompress
MIT-licensed test suite:
- `SharpCompress_Rar_Solid.cbr` from `Rar.solid.rar` (Git blob
  `ef56f892bf4ed17e1c54bd5f0fce65e3c1a5ec0b`).
- `SharpCompress_7Zip_Solid.cb7` from `7Zip.solid.7z` (Git blob
  `5ac695e59aeff2385e9065cb8841118f1bde06e1`).
