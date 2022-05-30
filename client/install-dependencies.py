import glob
import os
from pathlib import Path
import subprocess
import shutil

class Dependency:
    def __init__(self, input_folder, output_folder, files_to_copy):
        self.input_folder = Path(input_folder)
        self.output_folder = Path(output_folder)
        self.files_to_copy = files_to_copy

def clone_submodules():
    result = subprocess.run(["git", "submodule", "init"])

    if result.returncode != 0:
        print(f"[ERR] Failed to initialize git submodules (return code {result.returncode})")

    result = subprocess.run(["git", "submodule", "update"])

    if result.returncode != 0:
        print(f"[ERR] Failed to update git submodules (return code {result.returncode})")

def install_dependencies(dependencies):
    for dependency in dependencies:
        print(f"[INF] Installing '{dependency.input_folder}'")
        os.makedirs(dependency.output_folder, 0o777, True)

        for file_glob in dependency.files_to_copy:
            path_with_glob = Path(dependency.input_folder) / file_glob

            for file in glob.glob(str(path_with_glob)):
                # Take out the dependencies/folder from the front of the path
                strippedPath = Path(*Path(file).parts[2:])
                shutil.copy(file, dependency.output_folder / strippedPath)

if __name__ == "__main__":
    clone_submodules()

    os.chdir("client")

    dependencies = [
        Dependency("dependencies/json.lua", "src/lib/json", ["json.lua"]),
        Dependency("dependencies/batteries", "src/lib/batteries", ["*.lua"]),
        Dependency("dependencies/concord/concord", "src/lib", ["*.lua"])
    ]

    install_dependencies(dependencies)
