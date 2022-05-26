import os
import tempfile
from pathlib import Path
import subprocess
import glob
import shutil

flatc_binary = "C:\\Users\\joshp\\Downloads\\Windows.flatc.binary\\flatc"

class Language:
    def __init__(self, extension, flatc_argument, output_directory):
        self.extension = extension
        self.flatc_argument = flatc_argument
        self.output_directory = output_directory

    def __str__(self):
        return self.extension

def compile_schemas_for_language(language, fbs_files):
    with tempfile.TemporaryDirectory() as temp_directory:
        for file in fbs_files:
            print(f"Generating {language} for... '{file}'")
            result = subprocess.run([flatc_binary, language.flatc_argument, "-o", temp_directory, file])

        final_temp_path = Path(temp_directory)/"Zophos"/"Data"

        os.makedirs(language.output_directory, 0o777, True)
        for file in glob.glob(str(final_temp_path/f"*.{language.extension}")):
            shutil.copy(file, language.output_directory)

def verify_flatc():
    try:
        result = subprocess.call([flatc_binary, "--help"],
                    stdout=subprocess.DEVNULL,
                    stderr=subprocess.STDOUT)
    except FileNotFoundError:
        print(f"[ERR] Can't find flatc (checked '{flatc_binary}')!")
        exit(-1)

if __name__ == "__main__":
    verify_flatc()

    schema_directory = Path("schema")

    languages = [
        Language("cs", "--csharp", "server/src/Zophos.Server/Zophos.Data/Schemas"),
        Language("lua", "--lua", "client/src/schemas")
    ]

    fbs_files = glob.glob(str(schema_directory/"*.fbs"))

    for language in languages:
        compile_schemas_for_language(language, fbs_files)
