import argparse
import copy
import os
import tempfile
import zipfile
from xml.etree import ElementTree


EXPECTED_TARGETS = {
    "net10.0",
    "net10.0-ios27.1",
    "net11.0",
    "net11.0-ios27.2",
}


def local_name(tag):
    return tag.rsplit("}", 1)[-1]


def find_child(element, name):
    return next((child for child in element if local_name(child.tag) == name), None)


def package_metadata(package, package_path):
    nuspec_paths = [path for path in package.namelist() if path.endswith(".nuspec")]
    if len(nuspec_paths) != 1:
        raise ValueError(f"{package_path} must contain exactly one .nuspec file")

    nuspec_path = nuspec_paths[0]
    root = ElementTree.fromstring(package.read(nuspec_path))
    metadata = find_child(root, "metadata")
    if metadata is None:
        raise ValueError(f"{package_path} has no package metadata")
    return nuspec_path, root, metadata


def framework_groups(package, package_path):
    _, _, metadata = package_metadata(package, package_path)
    dependencies = find_child(metadata, "dependencies")
    if dependencies is None:
        return [], []

    groups = []
    direct_dependencies = []
    for child in dependencies:
        if local_name(child.tag) == "group":
            groups.append(copy.deepcopy(child))
        else:
            direct_dependencies.append(copy.deepcopy(child))
    return groups, direct_dependencies


def merge_packages(net10_path, net11_path, output_path):
    with zipfile.ZipFile(net10_path) as net10, zipfile.ZipFile(net11_path) as net11:
        net10_nuspec_path, _, net10_metadata = package_metadata(net10, net10_path)
        net11_nuspec_path, net11_root, net11_metadata = package_metadata(net11, net11_path)
        if net10_nuspec_path != net11_nuspec_path:
            raise ValueError("The package nuspec paths do not match")

        for key in ("id", "version"):
            net10_value = find_child(net10_metadata, key)
            net11_value = find_child(net11_metadata, key)
            if net10_value is None or net11_value is None or net10_value.text != net11_value.text:
                raise ValueError(f"The .NET 10 and .NET 11 packages must have the same {key}")

        entries = {}
        for package in (net11, net10):
            for path in package.namelist():
                if path == net10_nuspec_path:
                    continue
                if package is net10 and path in {"[Content_Types].xml", "_rels/.rels"}:
                    continue
                if package is net10 and path.startswith(
                    "package/services/metadata/core-properties/"
                ):
                    continue
                contents = package.read(path)
                if path in entries and entries[path] != contents:
                    raise ValueError(f"Package entries conflict at {path}")
                entries[path] = contents

        dependency_groups = {}
        direct_dependencies = {}
        for package, package_path in ((net10, net10_path), (net11, net11_path)):
            groups, direct = framework_groups(package, package_path)
            for group in groups:
                framework = group.get("targetFramework", "").lower()
                if framework in dependency_groups:
                    raise ValueError(f"Duplicate dependency group for {framework}")
                dependency_groups[framework] = group
            for dependency in direct:
                direct_dependencies[ElementTree.tostring(dependency)] = dependency

        missing_targets = {
            target
            for target in EXPECTED_TARGETS
            if not any(path.startswith(f"lib/{target}/") for path in entries)
        }
        if missing_targets:
            raise ValueError(f"Missing package assets for: {', '.join(sorted(missing_targets))}")

        dependencies = find_child(net11_metadata, "dependencies")
        if dependencies is None:
            namespace = (
                net11_metadata.tag[:-len("metadata")]
                if net11_metadata.tag.endswith("metadata")
                else ""
            )
            dependencies = ElementTree.SubElement(net11_metadata, f"{namespace}dependencies")
        for child in list(dependencies):
            dependencies.remove(child)
        for dependency in direct_dependencies.values():
            dependencies.append(dependency)
        for framework in sorted(dependency_groups):
            dependencies.append(dependency_groups[framework])

        entries[net10_nuspec_path] = ElementTree.tostring(
            net11_root, encoding="utf-8", xml_declaration=True
        )

    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)
    with tempfile.NamedTemporaryFile(dir=os.path.dirname(os.path.abspath(output_path)), delete=False) as temporary:
        temporary_path = temporary.name
    try:
        with zipfile.ZipFile(temporary_path, "w", compression=zipfile.ZIP_DEFLATED) as output:
            for path, contents in entries.items():
                output.writestr(path, contents)
        os.replace(temporary_path, output_path)
    finally:
        if os.path.exists(temporary_path):
            os.remove(temporary_path)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("net10_package")
    parser.add_argument("net11_package")
    parser.add_argument("output_package")
    arguments = parser.parse_args()
    merge_packages(arguments.net10_package, arguments.net11_package, arguments.output_package)


if __name__ == "__main__":
    main()
