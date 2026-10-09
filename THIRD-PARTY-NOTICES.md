# Third-party notices

Plumb is licensed under the MIT license (see `LICENSE`). It uses the following components under their own licenses.

## IfcOpenShell IfcConvert

- Version: 0.9.0, unmodified release binaries
- License: GNU Lesser General Public License v3.0, https://www.gnu.org/licenses/lgpl-3.0.html
- Source: https://github.com/IfcOpenShell/IfcOpenShell

Plumb runs IfcConvert as a separate program to convert IFC geometry to glTF. It is not linked into Plumb, and the bundled copy in `tools/ifcconvert/` can be replaced with any compatible build.

## Unity glTFast

- Version: 6.20.0, used by the viewer in `viewer/`
- License: Apache License 2.0, https://www.apache.org/licenses/LICENSE-2.0
- Source: https://github.com/Unity-Technologies/com.unity.cloud.gltfast

The viewer is built with Unity 6 under the Unity Software Terms.

## xBIM Essentials

- License: CDDL 1.0, https://github.com/xBimTeam/XbimEssentials/blob/master/LICENCE.md
- Source: https://github.com/xBimTeam/XbimEssentials

## Avalonia, CommunityToolkit.Mvvm, Microsoft.Data.Sqlite, Microsoft.Extensions.Logging

- License: MIT
