import subprocess
from subprocess import check_output
import zipfile

import os
import os.path
from os.path import join
import sys
import shutil
import argparse

parser = argparse.ArgumentParser(description="Release packaging script")
parser.add_argument("--release", dest="release", action="store_const",
        const=True, default=False,
        help="package from release directory")
args = parser.parse_args()

release_mode = args.release

slndir = ".."
projdir = join(slndir, "Mappy")
if (release_mode):
    bindir = join(projdir, "bin/Release")
else:
    bindir = join(projdir, "bin/Debug")

dist_files = [
        join(bindir, "Bugsnag.dll"),
        join(bindir, "Geometry.dll"),
        join(bindir, "Geometry.pdb"),
        join(bindir, "HPIUtil.dll"),
        join(bindir, "ICSharpCode.SharpZipLib.dll"),
        join(bindir, "Mappy.exe"),
        join(bindir, "Mappy.exe.config"),
        join(bindir, "Mappy.pdb"),
        join(bindir, "Ookii.Dialogs.dll"),
        join(bindir, "Pngcs.dll"),
        join(bindir, "System.Reactive.Core.dll"),
        join(bindir, "System.Reactive.Interfaces.dll"),
        join(bindir, "System.Reactive.Linq.dll"),
        join(bindir, "System.Reactive.PlatformServices.dll"),
        join(bindir, "System.Reactive.Windows.Forms.dll"),
        join(bindir, "System.ValueTuple.dll"),
        join(bindir, "TAUtil.dll"),
        join(bindir, "TAUtil.Gdi.dll"),
        join(bindir, "TAUtil.Gdi.pdb"),
        join(bindir, "TAUtil.HpiUtil.dll"),
        join(bindir, "TAUtil.HpiUtil.pdb"),
        join(bindir, "TAUtil.pdb"),
        join(slndir, "license.rx.txt"),
        join(slndir, "LICENSE.txt"),
        join(slndir, "Ookii.Dialogs/LICENSE.Ookii.Dialogs.txt"),
        join(slndir, "Pngcs/LICENSE.Pngcs.txt"),
        join(slndir, "README.md"),
    ]

project_name = "mappy"

tag = check_output(["git", "describe", "--dirty=-d"], universal_newlines=True).strip()
version = tag
if not release_mode:
    version += "-DEBUG"

dist_name = project_name + "-" + version

zip_name = dist_name + ".zip"

dist_dir = dist_name

# build dist dir
if (os.path.exists(dist_dir)):
    shutil.rmtree(dist_dir)
os.mkdir(dist_dir)

for path in dist_files:
    shutil.copy(path, dist_dir)

# rename the readme
os.rename(join(dist_dir, "README.md"), join(dist_dir, "README.txt"))

# zip it up
zip_file = zipfile.ZipFile(zip_name, "w", zipfile.ZIP_DEFLATED)
for (dirpath, dirnames, filenames) in os.walk(dist_dir):
    for f in filenames:
        zip_file.write(join(dirpath, f))
zip_file.close()

# create the installer
with open("install.iss.tmpl") as iss_tmpl:
    with open("install.iss", "w") as iss_file:
        for line in iss_tmpl:
            iss_file.write((line.replace("(PROJECT_VERSION)", version)))

iscc = "C:/Program Files (x86)/Inno Setup 6/ISCC.exe"
subprocess.run([iscc, "install.iss"], check=True)
