import os
import tempfile
import unittest
import zipfile
from xml.dom import minidom
from xml.etree import ElementTree

from merge_nuget_packages import EXPECTED_TARGETS, merge_packages


class MergePackagesTests(unittest.TestCase):
    def test_preserves_default_nuspec_namespace_and_all_targets(self):
        namespace = "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"
        with tempfile.TemporaryDirectory() as directory:
            package_paths = []
            for framework_set in ("net10", "net11"):
                package_path = os.path.join(directory, f"{framework_set}.nupkg")
                package_paths.append(package_path)
                targets = sorted(
                    target for target in EXPECTED_TARGETS
                    if target.startswith(framework_set)
                )
                groups = "".join(
                    f'<group targetFramework="{target}" />' for target in targets
                )
                nuspec = (
                    f'<package xmlns="{namespace}"><metadata>'
                    '<id>DuoExtensions.Maui</id><version>0.2.0</version>'
                    f'<dependencies>{groups}</dependencies>'
                    '</metadata></package>'
                )
                with zipfile.ZipFile(package_path, "w") as package:
                    package.writestr("DuoExtensions.Maui.nuspec", nuspec)
                    for target in targets:
                        package.writestr(f"lib/{target}/DuoExtensions.Maui.dll", target)

            output_path = os.path.join(directory, "merged.nupkg")
            merge_packages(*package_paths, output_path)

            with zipfile.ZipFile(output_path) as package:
                nuspec = package.read("DuoExtensions.Maui.nuspec")
                document = minidom.parseString(nuspec)
                self.assertIsNone(document.documentElement.prefix)
                self.assertEqual(namespace, document.documentElement.namespaceURI)
                root = ElementTree.fromstring(nuspec)
                self.assertEqual(
                    "DuoExtensions.Maui",
                    root.findtext(f"{{{namespace}}}metadata/{{{namespace}}}id"),
                )
                groups = root.findall(
                    f"{{{namespace}}}metadata/{{{namespace}}}dependencies/{{{namespace}}}group"
                )
                self.assertEqual(
                    EXPECTED_TARGETS,
                    {group.get("targetFramework") for group in groups},
                )
                for target in EXPECTED_TARGETS:
                    self.assertEqual(
                        target.encode(),
                        package.read(f"lib/{target}/DuoExtensions.Maui.dll"),
                    )


if __name__ == "__main__":
    unittest.main()